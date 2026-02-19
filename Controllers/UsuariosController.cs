/**
    * UsuariosController
    *
    * Controlador responsável pelo gerenciamento de usuários do sistema,
    * incluindo pesquisa, visualização, edição, criação, upload de fotos
    * e alteração de senha. Também registra histórico de alterações realizadas.
    *
    * Funcionalidades:
    * - Exibir painel de usuários.
    * - Buscar usuários com filtros dinâmicos.
    * - Exibir dados de um usuário específico.
    * - Atualizar dados de um usuário e registrar histórico.
    * - Upload de foto do usuário.
    * - Alteração de senha.
    * - Criar novos usuários.
    *
    * Dependências:
    * - ApplicationDbContext para acesso aos dados de usuários, setores, cargos e histórico.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using PIM.Models;
using PIM.Models.ViewModels;
using PIM.Helpers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace PIM.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        /**
            * Construtor UsuariosController
            *
            * Inicializa o controlador com dependência do contexto do banco de dados.
            *
            * Tipo de retorno: N/A
            *
            * Parâmetros:
            * - ApplicationDbContext context: contexto do banco de dados.
        */

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static readonly Dictionary<string, DateTime> UltimosHistoricos = new();

        /**
            * HistoricoRecentementeRegistrado (private)
            *
            * Verifica se determinada ação já foi registrada recentemente para evitar duplicidade
            * no histórico (intervalo menor que 55 segundos).
            *
            * Tipo de retorno: bool
            * - true se ação já registrada recentemente, false caso contrário.
            *
            * Parâmetros:
            * - int idUsuario: ID do usuário afetado.
            * - int? idModificante: ID de quem realizou a alteração.
            * - string acao: descrição da ação realizada.
        */

        private bool HistoricoRecentementeRegistrado(int idUsuario, int? idModificante, string acao)
        {
            string chave = $"{idUsuario}_{idModificante}_{acao}";
            if (UltimosHistoricos.TryGetValue(chave, out DateTime ultimaExecucao))
            {
                if ((DateTime.Now - ultimaExecucao).TotalSeconds < 55)
                {
                    return true;
                }
            }
            UltimosHistoricos[chave] = DateTime.Now;
            return false;
        }

        /**
            * PainelUsuarios
            *
            * Exibe o painel principal de gerenciamento de usuários.
            *
            * Tipo de retorno: IActionResult
            * - Retorna a View do painel de usuários.
        */

        public IActionResult PainelUsuarios()
        {
            ViewBag.TipoPainel = "PainelUsuarios";
            return View();
        }

        /**
            * Buscar
            *
            * Pesquisa usuários aplicando filtros dinâmicos de setor, nome, cargo e status.
            * Respeita regras de acesso conforme o cargo do usuário logado.
            *
            * Tipo de retorno: IActionResult (JSON)
            * - Retorna a lista de usuários filtrados.
            *
            * Parâmetros:
            * - string setor: filtro de setor.
            * - string nome: filtro de nome.
            * - string cargo: filtro de cargo.
            * - string status: filtro de status (Ativo/Inativo).
        */
        
        [HttpPost]
        public async Task<IActionResult> Buscar([FromForm] string setor, [FromForm] string nome, [FromForm] string cargo, [FromForm] string status)
        {
            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");

            if (usuarioLogado == null)
                return Unauthorized();

            var query = _context.Usuarios
                .Include(u => u.Setor)
                .Include(u => u.Cargo)
                .Where(u => u.Nome != "ChatGPT")
                .AsQueryable();

            // IDs fixos dos cargos
            int idDiretor = 10;
            int idCoordenador = 9;
            int idSupervisor = 8;

            // Regras de acesso
            if (usuarioLogado.ID_Cargo == idCoordenador || usuarioLogado.ID_Cargo == idSupervisor)
            {
                // Coordenador ou Supervisor vê usuários do mesmo setor
                query = query.Where(u => u.ID_Setor == usuarioLogado.ID_Setor);
            }
            else if (usuarioLogado.ID_Cargo != idDiretor)
            {
                // Demais usuários veem apenas a si mesmos
                query = query.Where(u => u.Id == usuarioLogado.Id);
            }

            // Filtros dinâmicos
            if (!string.IsNullOrWhiteSpace(setor) && int.TryParse(setor, out int setorId))
                query = query.Where(u => u.ID_Setor == setorId);

            if (!string.IsNullOrWhiteSpace(nome))
                query = query.Where(u => u.Nome.Contains(nome));

            if (!string.IsNullOrWhiteSpace(cargo))
                query = query.Where(u => u.Cargo.Descricao.Contains(cargo));

            if (!string.IsNullOrWhiteSpace(status) && int.TryParse(status, out int statusInt))
                query = query.Where(u => u.Status == statusInt);

            var usuarios = await query
                .Select(u => new
                {
                    u.Id,
                    u.Nome,
                    Setor = u.Setor.Descricao,
                    Cargo = u.Cargo.Descricao,
                    Status = u.Status == 1 ? "Ativo" : "Inativo"
                })
                .ToListAsync();

            return Json(usuarios);
        }

        /**
            * DadosUsuario (GET)
            *
            * Exibe detalhes de um usuário específico, incluindo setores, cargos e histórico.
            *
            * Tipo de retorno: IActionResult
            * - Retorna a View com dados completos do usuário.
            *
            * Parâmetros:
            * - int id: ID do usuário a ser exibido.
        */

        public async Task<IActionResult> DadosUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Setor)
                .Include(u => u.Cargo)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return NotFound();

            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            if (usuarioLogado == null)
                return Unauthorized();

            ViewBag.UsuarioLogado = usuarioLogado;

            // Pega todos os setores para popular o select
            ViewBag.Setores = await _context.Setores.OrderBy(s => s.Descricao).ToListAsync();

            // Pega todos os cargos para popular o select
            ViewBag.Cargos = await _context.Cargos.OrderBy(c => c.Descricao).ToListAsync();

            // Pega o histórico do usuário para exibir
            ViewBag.Historicos = await _context.HistoricoUsuario
                .Where(h => h.ID_Usuario == id)
                .OrderByDescending(h => h.Data)
                .ToListAsync();

            return View(usuario);
        }

        /**
            * DadosUsuario (POST)
            *
            * Atualiza os dados de um usuário e registra alterações no histórico.
            *
            * Tipo de retorno: IActionResult
            * - Redireciona para a mesma página com os dados atualizados.
            *
            * Parâmetros:
            * - Usuario model: objeto com os dados atualizados do usuário.
            *
            * Observações:
            * - Histórico é registrado apenas se não houver alteração recente similar.
        */

        [HttpPost]
        public async Task<IActionResult> DadosUsuario(Usuario model)
        {
            var usuarioDb = await _context.Usuarios.FindAsync(model.Id);
            if (usuarioDb == null) return NotFound();

            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            int? idModificante = usuarioLogado?.Id;
            string nomeModificante = usuarioLogado?.Nome ?? "Sistema";

            var historicosParaInserir = new List<HistoricoUsuario>();

            void AdicionarHistorico(string acao)
            {
                if (!HistoricoRecentementeRegistrado(usuarioDb.Id, idModificante, acao))
                {
                    historicosParaInserir.Add(new HistoricoUsuario
                    {
                        ID_Usuario = usuarioDb.Id,
                        ID_Modificante = idModificante,
                        QuemFez = nomeModificante,
                        AcaoTomada = acao,
                        Data = DateTime.Now
                    });
                }
            }

            if (usuarioDb.Nome != model.Nome)
            {
                AdicionarHistorico($"Alterou o campo Nome de \"{usuarioDb.Nome}\" para \"{model.Nome}\"");
                usuarioDb.Nome = model.Nome;
            }

            if (usuarioDb.Email != model.Email)
            {
                AdicionarHistorico($"Alterou o campo Email de \"{usuarioDb.Email}\" para \"{model.Email}\"");
                usuarioDb.Email = model.Email;
            }

            if (usuarioDb.ID_Setor != model.ID_Setor)
            {
                AdicionarHistorico($"Alterou o campo Setor de \"{usuarioDb.ID_Setor}\" para \"{model.ID_Setor}\"");
                usuarioDb.ID_Setor = model.ID_Setor;
            }

            if (usuarioDb.ID_Cargo != model.ID_Cargo)
            {
                AdicionarHistorico($"Alterou o campo Cargo de \"{usuarioDb.ID_Cargo}\" para \"{model.ID_Cargo}\"");
                usuarioDb.ID_Cargo = model.ID_Cargo;
            }

            if (usuarioDb.DataNasc != model.DataNasc)
            {
                AdicionarHistorico($"Alterou o campo Data de Nascimento de \"{usuarioDb.DataNasc?.ToString("dd/MM/yyyy")}\" para \"{model.DataNasc?.ToString("dd/MM/yyyy")}\"");
                usuarioDb.DataNasc = model.DataNasc;
            }

            if (usuarioDb.DataAdm != model.DataAdm)
            {
                AdicionarHistorico($"Alterou o campo Data de Admissão de \"{usuarioDb.DataAdm?.ToString("dd/MM/yyyy")}\" para \"{model.DataAdm?.ToString("dd/MM/yyyy")}\"");
                usuarioDb.DataAdm = model.DataAdm;
            }

            if (usuarioDb.DataDemi != model.DataDemi)
            {
                AdicionarHistorico($"Alterou o campo Data de Demissão de \"{usuarioDb.DataDemi?.ToString("dd/MM/yyyy")}\" para \"{model.DataDemi?.ToString("dd/MM/yyyy")}\"");
                usuarioDb.DataDemi = model.DataDemi;
            }

            if (usuarioDb.Status != model.Status)
            {
                string statusAntigo = usuarioDb.Status == 1 ? "Ativo" : "Inativo";
                string statusNovo = model.Status == 1 ? "Ativo" : "Inativo";
                AdicionarHistorico($"Alterou o campo Status de \"{statusAntigo}\" para \"{statusNovo}\"");
                usuarioDb.Status = model.Status;
            }

            _context.Usuarios.Update(usuarioDb);

            if (historicosParaInserir.Any())
                _context.HistoricoUsuario.AddRange(historicosParaInserir);

            await _context.SaveChangesAsync();

            return RedirectToAction("DadosUsuario", new { id = usuarioDb.Id });
        }

        /**
            * UploadFoto
            *
            * Faz upload da foto do usuário, valida extensão e registra histórico da ação.
            *
            * Tipo de retorno: IActionResult (JSON)
            * - Retorna OK em caso de sucesso ou BadRequest em caso de erro.
            *
            * Parâmetros:
            * - int id: ID do usuário.
            * - IFormFile foto: arquivo de imagem enviado.
        */

        [HttpPost]
        public async Task<IActionResult> UploadFoto(int id, IFormFile foto)
        {
            if (foto == null || foto.Length == 0)
                return BadRequest("Nenhuma foto foi enviada.");

            var extensao = Path.GetExtension(foto.FileName).ToLower();
            if (extensao != ".jpg" && extensao != ".jpeg")
                return BadRequest("Apenas arquivos JPG ou JPEG são permitidos.");

            var fileName = $"{id}.jpg";
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "usuarios");

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var caminhoCompleto = Path.Combine(savePath, fileName);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            int? idModificante = usuarioLogado?.Id;
            string nomeModificante = usuarioLogado?.Nome ?? "Sistema";

            string acao = "Atualizou a foto do usuário.";
            if (!HistoricoRecentementeRegistrado(id, idModificante, acao))
            {
                _context.HistoricoUsuario.Add(new HistoricoUsuario
                {
                    ID_Usuario = id,
                    ID_Modificante = idModificante,
                    QuemFez = nomeModificante,
                    AcaoTomada = acao,
                    Data = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            return Ok(new { sucesso = true });
        }

        /**
            * SalvarAlteracoes
            *
            * Atualiza campos específicos de um usuário passados via JSON e registra histórico.
            *
            * Tipo de retorno: IActionResult (JSON)
            * - Retorna sucesso ou erro da operação.
            *
            * Parâmetros:
            * - int id: ID do usuário.
            * - Dictionary<string,string> campos: campos a serem atualizados.
        */

        [HttpPost]
        public async Task<IActionResult> SalvarAlteracoes(int id, [FromBody] Dictionary<string, string> campos)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Setor)
                .Include(u => u.Cargo)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return NotFound();

            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            int? idModificante = usuarioLogado?.Id;
            string nomeModificante = usuarioLogado?.Nome ?? "Sistema";

            var historicosParaInserir = new List<HistoricoUsuario>();

            foreach (var campo in campos)
            {
                string acao = null;

                switch (campo.Key)
                {
                    case "Nome":
                        if (usuario.Nome != campo.Value)
                        {
                            acao = $"Alterou o campo Nome de \"{usuario.Nome}\" para \"{campo.Value}\"";
                            usuario.Nome = campo.Value;
                        }
                        break;
                    case "Email":
                        if (usuario.Email != campo.Value)
                        {
                            acao = $"Alterou o campo Email de \"{usuario.Email}\" para \"{campo.Value}\"";
                            usuario.Email = campo.Value;
                        }
                        break;
                    case "Status":
                        if (int.TryParse(campo.Value, out int status) && usuario.Status != status)
                        {
                            string statusAntigo = usuario.Status == 1 ? "Ativo" : "Inativo";
                            string statusNovo = status == 1 ? "Ativo" : "Inativo";
                            acao = $"Alterou o campo Status de \"{statusAntigo}\" para \"{statusNovo}\"";
                            usuario.Status = status;
                        }
                        break;
                    case "DataNasc":
                        if (DateTime.TryParse(campo.Value, out DateTime nasc) && usuario.DataNasc != nasc)
                        {
                            acao = $"Alterou o campo Data de Nascimento de \"{usuario.DataNasc?.ToString("dd/MM/yyyy")}\" para \"{nasc.ToString("dd/MM/yyyy")}\"";
                            usuario.DataNasc = nasc;
                        }
                        break;
                    case "DataAdm":
                        if (DateTime.TryParse(campo.Value, out DateTime adm) && usuario.DataAdm != adm)
                        {
                            acao = $"Alterou o campo Data de Admissão de \"{usuario.DataAdm?.ToString("dd/MM/yyyy")}\" para \"{adm.ToString("dd/MM/yyyy")}\"";
                            usuario.DataAdm = adm;
                        }
                        break;
                    case "DataDemi":
                        if (DateTime.TryParse(campo.Value, out DateTime demi) && usuario.DataDemi != demi)
                        {
                            acao = $"Alterou o campo Data de Demissão de \"{usuario.DataDemi?.ToString("dd/MM/yyyy")}\" para \"{demi.ToString("dd/MM/yyyy")}\"";
                            usuario.DataDemi = demi;
                        }
                        break;
                    case "ID_Setor":
                        if (int.TryParse(campo.Value, out int setorId) && usuario.ID_Setor != setorId)
                        {
                            var setorAntigoNome = usuario.Setor?.Descricao ?? "Sem Setor";
                            var setorNovoNome = await _context.Setores
                                .Where(s => s.Id == setorId)
                                .Select(s => s.Descricao)
                                .FirstOrDefaultAsync() ?? "Sem Setor";

                            acao = $"Alterou o campo Setor de \"{setorAntigoNome}\" para \"{setorNovoNome}\"";
                            usuario.ID_Setor = setorId;
                        }
                        break;

                    case "ID_Cargo":
                        if (int.TryParse(campo.Value, out int cargoId) && usuario.ID_Cargo != cargoId)
                        {
                            // Cargo do usuário logado
                            var cargoLogadoId = usuarioLogado?.ID_Cargo;

                            // Lista de cargos em ordem hierárquica
                            var hierarquia = new List<int> { 8, 9, 10 };

                            // Só valida se o cargo novo está na hierarquia especial
                            if (hierarquia.Contains(cargoId))
                            {
                                if (cargoLogadoId == null 
                                    || !hierarquia.Contains((int)cargoLogadoId) 
                                    || hierarquia.IndexOf(cargoId) > hierarquia.IndexOf((int)cargoLogadoId))
                                {
                                    return BadRequest(new { sucesso = false, mensagem = "Você não tem permissão para atribuir esse cargo." });
                                }
                            }

                            var cargoAntigoNome = usuario.Cargo?.Descricao ?? "Sem Cargo";
                            var cargoNovoNome = await _context.Cargos
                                .Where(c => c.Id == cargoId)
                                .Select(c => c.Descricao)
                                .FirstOrDefaultAsync() ?? "Sem Cargo";

                            acao = $"Alterou o campo Cargo de \"{cargoAntigoNome}\" para \"{cargoNovoNome}\"";
                            usuario.ID_Cargo = cargoId;
                        }
                        break;
                }

                if (acao != null && !HistoricoRecentementeRegistrado(usuario.Id, idModificante, acao))
                {
                    historicosParaInserir.Add(new HistoricoUsuario
                    {
                        ID_Usuario = usuario.Id,
                        ID_Modificante = idModificante,
                        QuemFez = nomeModificante,
                        AcaoTomada = acao,
                        Data = DateTime.Now
                    });
                }
            }

            if (historicosParaInserir.Any())
                _context.HistoricoUsuario.AddRange(historicosParaInserir);

            await _context.SaveChangesAsync();

            return Ok(new { sucesso = true, mensagem = "Dados atualizados com sucesso." });
        }

        /**
            * AlterarSenha
            *
            * Atualiza a senha de um usuário, aplicando hash MD5.
            *
            * Tipo de retorno: IActionResult (JSON)
            * - Retorna mensagem de sucesso ou erro.
            *
            * Parâmetros:
            * - AlterarSenhaViewModel model: objeto contendo ID do usuário e nova senha.
        */

        [HttpPost]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaViewModel model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.NovaSenha))
            {
                return BadRequest(new { mensagem = "Dados inválidos para alteração de senha." });
            }

            var usuario = await _context.Usuarios.FindAsync(model.Id);

            if (usuario == null)
            {
                return NotFound(new { mensagem = "Usuário não encontrado." });
            }

            // Gera hash MD5 da nova senha
            string senhaComHash = GerarHashMD5(model.NovaSenha.Trim());
            usuario.Senha = senhaComHash;

            try
            {
                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                return Ok(new { mensagem = "Senha alterada com sucesso." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro ao alterar a senha.", detalhes = ex.Message });
            }
        }

        /**
            * GerarHashMD5
            *
            * Gera hash MD5 para uma string fornecida.
            *
            * Tipo de retorno: string
            * - Retorna a representação hexadecimal do hash MD5.
            *
            * Parâmetros:
            * - string input: texto a ser convertido em hash MD5.
        */

        private string GerarHashMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // minúsculo
                }
                return sb.ToString();
            }
        }

        /**
            * NovoUsuario
            *
            * Exibe a página para criação de um novo usuário, populando dropdowns
            * de cargos e setores e trazendo informações do usuário logado.
            *
            * Tipo de retorno: IActionResult
            * - Retorna a View para criar um novo usuário.
        */

        public IActionResult NovoUsuario()
        {
            ViewBag.Cargos = _context.Cargos.ToList();
            ViewBag.Setores = _context.Setores.ToList();

            // Pega o usuário logado da sessão
            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");

            if (usuarioLogado != null)
            {
                ViewBag.SetorUsuarioLogado = usuarioLogado.ID_Setor;

                // Busca o setor pelo ID do setor do usuário
                var setorUsuario = _context.Setores.FirstOrDefault(s => s.Id == usuarioLogado.ID_Setor);
                ViewBag.SetorNome = setorUsuario != null ? setorUsuario.Descricao : "Setor não identificado";
            }
            else
            {
                ViewBag.SetorNome = "Setor não identificado";
                ViewBag.SetorUsuarioLogado = 0;
            }

            return View();
        }

        /**
            * CriarUsuario
            *
            * Cria um novo usuário com dados fornecidos, valida datas e telefone,
            * atribui senha padrão com hash MD5 e registra no banco.
            *
            * Tipo de retorno: IActionResult
            * - Redireciona para NovoUsuario com mensagens de sucesso ou erro.
            *
            * Parâmetros:
            * - string Nome, string Email, int ID_Cargo, int ID_Setor, DateTime? dataNascimento,
            *   DateTime? dataAdmissao, string telefone
            *
            * Observações:
            * - Senha padrão inicial: "Zennix@2025" (criptografada com MD5).
            * - Telefone é limpo para conter apenas números.
        */

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CriarUsuario(string Nome, string Email, int ID_Cargo, int ID_Setor, DateTime? dataNascimento, DateTime? dataAdmissao, string telefone)
        {
            try
            {
                if (!dataNascimento.HasValue || !dataAdmissao.HasValue)
                {
                    TempData["MensagemErro"] = "Data de nascimento ou admissão inválida.";
                    return RedirectToAction("NovoUsuario");
                }

                // Remove tudo que não seja número
                telefone = Regex.Replace(telefone ?? "", @"\D", "");

                var usuario = new Usuario
                {
                    Nome = Nome,
                    Email = Email,
                    ID_Cargo = ID_Cargo,
                    ID_Setor = ID_Setor,
                    Status = 1,
                    Senha = GerarHashMD5("Zennix@2025"),
                    DataNasc = dataNascimento.Value,
                    DataAdm = dataAdmissao.Value,
                    DataDemi = null,
                    Telefone = telefone
                };

                _context.Usuarios.Add(usuario);
                _context.SaveChanges();

                TempData["MensagemSucesso"] = "Usuário criado com sucesso!";
                return RedirectToAction("NovoUsuario");
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "";
                TempData["MensagemErro"] = $"Erro ao criar usuário: {ex.Message} {innerMessage}";
                return RedirectToAction("NovoUsuario");
            }
        }

        /**
            * CriarUsuarioTeste
            *
            * Cria um novo usuário de teste com acesso único.
            * 
            * Tipo de retorno: IActionResult (JSON)
            * - Retorna as credenciais geradas para o usuário de teste.
            *
            * Parâmetros:
            * - [FromBody] UsuarioTesteViewModel model: objeto com nome, email e telefone
        */

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CriarUsuarioTeste([FromBody] UsuarioTesteViewModel model)
        {
            try
            {
                // Validar modelo
                if (model == null || string.IsNullOrEmpty(model.Nome) || string.IsNullOrEmpty(model.Email))
                {
                    return Json(new { sucesso = false, mensagem = "Nome e e-mail são obrigatórios." });
                }

                // Validar se o email já existe
                var emailExistente = await _context.Usuarios
                    .AnyAsync(u => u.Email == model.Email);

                if (emailExistente)
                {
                    return Json(new { sucesso = false, mensagem = "Este e-mail já está cadastrado no sistema." });
                }

                // Gerar senha aleatória
                string senhaGerada = GerarSenhaAleatoria(8);
                
                // Criar usuário de teste
                var usuarioTeste = new Usuario
                {
                    Nome = model.Nome,
                    Email = model.Email,
                    Telefone = model.Telefone,
                    Senha = GerarHashMD5(senhaGerada),
                    TipoUsuario = 1, // 1 = Usuário de teste
                    Status = 1,
                    
                    // Valores padrão para campos obrigatórios que já existem
                    ID_Cargo = 0,
                    ID_Setor = 0,
                    DataNasc = DateTime.Now.AddYears(-18),
                    DataAdm = DateTime.Now
                };

                _context.Usuarios.Add(usuarioTeste);
                await _context.SaveChangesAsync();

                return Json(new 
                { 
                    sucesso = true, 
                    mensagem = "Usuário de teste criado com sucesso!",
                    senha = senhaGerada,
                    email = usuarioTeste.Email,
                    nome = usuarioTeste.Nome
                });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao criar usuário de teste: " + ex.Message });
            }
        }

        private string GerarSenhaAleatoria(int tamanho)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, tamanho)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
