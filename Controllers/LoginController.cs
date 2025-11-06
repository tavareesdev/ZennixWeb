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
    *
    * Dependências:
    * - ApplicationDbContext para acesso aos dados de usuários e sessões.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PIM.Models;
using System.Linq;
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
                .FirstOrDefault(u => u.Email == model.Email && u.Senha == senhaHash);


            if (usuario != null)
            {
                // Inativa sessões anteriores ativas
                var sessoesAtivas = _context.Sessoes
                    .Where(s => s.UsuarioId == usuario.Id && s.Ativa)
                    .ToList();

                foreach (var sessao in sessoesAtivas)
                {
                    sessao.Ativa = false;
                    sessao.DataFim = DateTime.Now; // REGISTRA O FIM
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

                HttpContext.Session.SetObjectAsJson("usuario", usuario);

                TempData["LoginStatus"] = "sucesso";
                return RedirectToAction("Index", "Home");
            }

            TempData["LoginStatus"] = "erro";
            return View("Index");
        }        

        /**
            * GerarMD5
            *
            * Gera o hash MD5 de uma string de entrada.
            *
            * Tipo de retorno: string
            * - Retorna a representação hexadecimal do hash MD5 da string fornecida.
            *
            * Funcionamento detalhado:
            * 1. Converte a string de entrada para bytes ASCII.
            * 2. Calcula o hash MD5 dos bytes.
            * 3. Converte os bytes do hash para string hexadecimal.
            *
            * Parâmetros:
            * - string input: texto a ser convertido em hash MD5.
        */

        private string GerarMD5(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
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
            *
            * Funcionamento detalhado:
            * 1. Obtém usuário logado a partir da sessão HTTP.
            * 2. Verifica se existe sessão ativa no banco de dados.
            * 3. Se existir, marca sessão como inativa e registra DataFim.
            * 4. Limpa todas as informações da sessão HTTP.
            * 5. Redireciona para Index.
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
                    sessaoAtiva.DataFim = DateTime.Now; // REGISTRA O FIM
                    _context.SaveChanges();
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
