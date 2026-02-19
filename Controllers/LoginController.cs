/**
    * LoginController
    *
    * Controlador responsável pela autenticação de usuários no sistema,
    * gerenciamento de sessões ativas e encerramento de sessões.
    *
    * Funcionalidades:
    * - Exibir tela de login.
    * - Autenticar usuário e iniciar sessão.
    * - Encerrar sessão do usuário.
    * - Criar usuário de teste com acesso único.
    *
    * Dependências:
    * - ApplicationDbContext para acesso aos dados de usuários e sessões.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PIM.Models;
using PIM.Models.ViewModels;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using PIM.Helpers;

namespace PIM.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        /**
            * Construtor LoginController
            *
            * Inicializa o controlador com dependência do contexto do banco de dados.
            *
            * Tipo de retorno: N/A
            *
            * Funcionamento detalhado:
            * - Recebe instância de ApplicationDbContext e atribui à propriedade privada do controlador.
            *
            * Parâmetros:
            * - ApplicationDbContext context: contexto do banco de dados.
        */

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        /**
            * Index
            *
            * Exibe a página de login para o usuário.
            *
            * Tipo de retorno: IActionResult
            * - Retorna a View de login.
            *
            * Funcionamento detalhado:
            * - Não recebe parâmetros.
            * - Apenas retorna a view padrão de login.
        */

        public IActionResult Index()
        {
            return View();
        }

        /**
            * Entrar
            *
            * Autentica o usuário com base em email e senha e inicia uma sessão ativa.
            *
            * Tipo de retorno: IActionResult
            * - Redireciona para a página inicial do sistema em caso de sucesso.
            * - Retorna a View de login com mensagens de erro em caso de falha.
            *
            * Funcionamento detalhado:
            * 1. Verifica se campos de email e senha foram preenchidos.
            * 2. Gera hash MD5 da senha fornecida.
            * 3. Busca usuário no banco de dados com email e senha hash correspondentes.
            * 4. Se usuário encontrado:
            *    - Inativa quaisquer sessões anteriores ainda ativas.
            *    - Cria uma nova sessão ativa com data de início atual.
            *    - Armazena informações do usuário na sessão HTTP.
            *    - Define TempData["LoginStatus"] como "sucesso" e redireciona para Home/Index.
            * 5. Se usuário não encontrado, define TempData["LoginStatus"] como "erro" e retorna a view de login.
            *
            * Parâmetros:
            * - UsuarioLogin model: objeto contendo Email e Senha do usuário.
        */

        [HttpPost]
        public IActionResult Entrar(UsuarioLogin model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Senha))
            {
                TempData["LoginStatus"] = "campos-vazios";
                return View("Index");
            }

            string senhaHash = GerarMD5(model.Senha);

            var usuario = _context.Usuarios
                .Include(u => u.Setor)
                .Include(u => u.Cargo)
                .FirstOrDefault(u => u.Email == model.Email && u.Senha == senhaHash);

            if (usuario != null)
            {
                // Verifica se é usuário de teste
                if (usuario.TipoUsuario == 1) // É usuário de teste
                {
                    // Verifica se já existe alguma sessão para este usuário
                    var sessaoExistente = _context.Sessoes
                        .Any(s => s.UsuarioId == usuario.Id);

                    if (sessaoExistente)
                    {
                        // Se já existe sessão, significa que já acessou antes
                        TempData["LoginStatus"] = "acesso-ja-utilizado";
                        return View("Index");
                    }
                    
                    // Primeiro acesso - cria sessão normalmente
                    var novaSessao = new Sessao
                    {
                        UsuarioId = usuario.Id,
                        DataInicio = DateTime.Now,
                        Ativa = true
                    };
                    _context.Sessoes.Add(novaSessao);
                    _context.SaveChanges();
                    
                    // Marca na sessão HTTP que é acesso de teste
                    HttpContext.Session.SetString("tipoAcesso", "teste");
                    TempData["LoginStatus"] = "sucesso-teste";
                }
                else
                {
                    // Usuário normal - inativa sessões anteriores
                    var sessoesAtivas = _context.Sessoes
                        .Where(s => s.UsuarioId == usuario.Id && s.Ativa)
                        .ToList();

                    foreach (var sessao in sessoesAtivas)
                    {
                        sessao.Ativa = false;
                        sessao.DataFim = DateTime.Now;
                    }

                    // Cria nova sessão ativa
                    var novaSessao = new Sessao
                    {
                        UsuarioId = usuario.Id,
                        DataInicio = DateTime.Now,
                        Ativa = true
                    };

                    _context.Sessoes.Add(novaSessao);
                    _context.SaveChanges();
                    
                    TempData["LoginStatus"] = "sucesso";
                }

                // Armazena informações do usuário na sessão
                HttpContext.Session.SetObjectAsJson("usuario", usuario);
                
                return RedirectToAction("Index", "Home");
            }

            TempData["LoginStatus"] = "erro";
            return View("Index");
        }

        /**
            * CriarUsuarioTeste
            *
            * Cria um novo usuário de teste com acesso único.
            * Utiliza cargo ID 1 e setor ID 1 como padrão.
            *
            * Tipo de retorno: IActionResult (JSON)
            * - Retorna as credenciais geradas para o usuário de teste.
            *
            * Parâmetros:
            * - [FromBody] UsuarioTesteViewModel model: objeto com nome, email e telefone
        */

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CriarUsuarioTeste([FromBody] UsuarioTesteViewModel model)
        {
            try
            {
                // Validação básica
                if (model == null)
                {
                    return Json(new { sucesso = false, mensagem = "Dados não recebidos." });
                }

                if (string.IsNullOrEmpty(model.Nome) || string.IsNullOrEmpty(model.Email))
                {
                    return Json(new { sucesso = false, mensagem = "Nome e e-mail são obrigatórios." });
                }

                // Validar formato do email
                if (!IsValidEmail(model.Email))
                {
                    return Json(new { sucesso = false, mensagem = "E-mail inválido." });
                }

                // Validar se o email já existe
                var emailExistente = await _context.Usuarios
                    .AnyAsync(u => u.Email == model.Email);

                if (emailExistente)
                {
                    return Json(new { sucesso = false, mensagem = "Este e-mail já está cadastrado no sistema." });
                }

                // Verificar se os IDs 1 existem (opcional, mas recomendado)
                var cargoExiste = await _context.Cargos.AnyAsync(c => c.Id == 1);
                var setorExiste = await _context.Setores.AnyAsync(s => s.Id == 1);

                if (!cargoExiste || !setorExiste)
                {
                    return Json(new { sucesso = false, mensagem = "Cargo ID 1 ou Setor ID 1 não encontrados. Contate o administrador." });
                }

                // Gerar senha aleatória (8 caracteres)
                string senhaGerada = GerarSenhaAleatoria(8);
                
                // Criar usuário de teste com IDs fixos (1)
                var usuarioTeste = new Usuario
                {
                    Nome = model.Nome.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Telefone = model.Telefone?.Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", ""),
                    Senha = GerarMD5(senhaGerada),
                    TipoUsuario = 1, // 1 = Usuário de teste
                    Status = 1,
                    
                    // IDs fixos conforme solicitado
                    ID_Cargo = 1,
                    ID_Setor = 1,
                    DataNasc = DateTime.Now.AddYears(-18),
                    DataAdm = DateTime.Now
                };

                _context.Usuarios.Add(usuarioTeste);
                await _context.SaveChangesAsync();

                // Retornar sucesso com as credenciais
                return Json(new 
                { 
                    sucesso = true, 
                    mensagem = "Usuário de teste criado com sucesso!",
                    senha = senhaGerada,
                    email = usuarioTeste.Email,
                    nome = usuarioTeste.Nome
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { sucesso = false, mensagem = "Erro no banco de dados: " + innerMessage });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao criar usuário de teste: " + ex.Message });
            }
        }

        /**
            * GerarSenhaAleatoria
            *
            * Gera uma senha aleatória com letras e números.
            *
            * Tipo de retorno: string
            * - Retorna a senha gerada.
            *
            * Parâmetros:
            * - int tamanho: tamanho da senha
        */

        private string GerarSenhaAleatoria(int tamanho)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, tamanho)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /**
            * IsValidEmail
            *
            * Valida se o formato do email é válido.
            *
            * Tipo de retorno: bool
            * - true se email válido, false caso contrário.
            *
            * Parâmetros:
            * - string email: email a ser validado
        */

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /**
            * GerarMD5
            *
            * Gera o hash MD5 de uma string de entrada.
            *
            * Tipo de retorno: string
            * - Retorna a representação hexadecimal do hash MD5 da string fornecida.
            *
            * Parâmetros:
            * - string input: texto a ser convertido em hash MD5.
        */

        private string GerarMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }

        /**
            * Sair
            *
            * Encerra a sessão do usuário atualmente logado e limpa a sessão HTTP.
            *
            * Tipo de retorno: IActionResult
            * - Redireciona para a página de login (Index).
        */
        
        public IActionResult Sair()
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");

            if (usuario != null)
            {
                var sessaoAtiva = _context.Sessoes
                    .FirstOrDefault(s => s.UsuarioId == usuario.Id && s.Ativa);

                if (sessaoAtiva != null)
                {
                    sessaoAtiva.Ativa = false;
                    sessaoAtiva.DataFim = DateTime.Now;
                    _context.SaveChanges();
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}