/**
    * HomeController
    *
    * Controlador responsável pela página inicial do sistema, carregamento de dados para dashboards,
    * gráficos e ranking de desempenho de atendentes.
    * 
    * Funcionalidades:
    * - Renderizar a view principal com ranking de atendentes e gráficos de chamados.
    * - Fornecer dados filtrados para gráficos via requisições AJAX.
    * - Filtrar funcionários por setor para dropdowns.
    *
    * Dependências:
    * - ApplicationDbContext para acesso aos dados de chamados, usuários e setores.
    * - ILogger<HomeController> para registro de logs e tratamento de exceções.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIM.Models;
using PIM.Models.ViewModels;
using PIM.Helpers;

namespace PIM.Controllers
{
    public class HomeController : Controller
    { // ← ADICIONE ESTA LINHA
        public IActionResult Landing()
        {
            return View();
        }
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        /**
            * Construtor HomeController
            *
            * Inicializa o controlador com dependências necessárias.
            *
            * Tipo de retorno: N/A
            *
            * Funcionamento detalhado:
            * - Recebe instâncias de ApplicationDbContext e ILogger<HomeController>.
            * - Atribui as instâncias às propriedades privadas do controlador.
            *
            * Parâmetros:
            * - ApplicationDbContext context: contexto do banco de dados.
            * - ILogger<HomeController> logger: logger para registrar informações e erros.
        */

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /**
            * Index
            *
            * Carrega a view principal do sistema com ranking, dados de gráficos e informações do usuário logado.
            *
            * Tipo de retorno: IActionResult
            * - Retorna View(List<RankingViewModel>) com os dados preparados.
            *
            * Funcionamento detalhado:
            * 1. Popula dropdowns de setores e funcionários.
            * 2. Recupera ID do usuário logado na sessão.
            * 3. Calcula ranking TOP 3 dos atendentes que mais concluíram chamados, excluindo ChatGPT.
            * 4. Gera dados agregados para gráficos de quantidade de chamados por status.
            * 5. Define indicadores adicionais (ex.: desempenho).
            * 6. Em caso de erro, captura exceção e retorna view com lista vazia.
        */

        public IActionResult Index()
        {
            try
            {
                // Popula dropdowns de filtros
                ViewBag.Setores = _context.Setores.ToList();
                ViewBag.Funcionarios = _context.Usuarios.ToList();
                var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
                ViewBag.UsuarioLogadoId = usuarioLogado?.Id;

                // Ranking TOP 3 — atendentes que mais concluíram chamados (filtra só não nulos e exclui ChatGPT)
                var ranking = (from c in _context.Chamados
                               join u in _context.Usuarios on c.ID_Atendente equals u.Id
                               join s in _context.Setores on u.ID_Setor equals s.Id
                               where c.Status == "Concluído" && c.ID_Atendente != null && u.Nome != "ChatGPT"
                               group c by new { u.Nome, u.Id, s.Descricao } into g
                               select new RankingViewModel
                               {
                                   Nome = g.Key.Nome,
                                   Setor = g.Key.Descricao,
                                   Concluidos = g.Count(),
                                   Id = g.Key.Id
                               })
                               .OrderByDescending(r => r.Concluidos)
                               .Take(3)
                               .ToList();

                // Dados para o gráfico: quantidade por status
                var dadosGrafico = _context.Chamados
                    .GroupBy(c => c.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Quantidade = g.Count()
                    })
                    .OrderByDescending(g => g.Quantidade)
                    .ToList();

                ViewBag.DadosGrafico = dadosGrafico;
                ViewBag.Desempenho = "Bom"; // Ajuste conforme regra do seu sistema

                return View(ranking);
            }
            catch (Exception ex)
            {
                ViewBag.Erro = "Erro ao carregar dados: " + ex.Message;
                return View(new List<RankingViewModel>());
            }
        }

        /**
            * GetDadosGrafico
            *
            * Retorna dados agregados para gráficos com base em filtros opcionais de setor e funcionário.
            *
            * Tipo de retorno: IActionResult
            * - Retorna Json contendo Status e Quantidade de chamados, ou mensagem de erro.
            *
            * Funcionamento detalhado:
            * 1. Cria query base com JOIN entre chamados e usuários.
            * 2. Aplica filtro por setor, se fornecido.
            * 3. Aplica filtro por funcionário, se fornecido.
            * 4. Agrupa por status do chamado e conta quantidade.
            * 5. Ordena resultados por quantidade em ordem decrescente.
            * 6. Captura exceções e retorna JSON com mensagem de erro.
            *
            * Parâmetros:
            * - int? setorId: ID do setor para filtro opcional.
            * - int? funcionarioId: ID do funcionário para filtro opcional.
        */

        [HttpGet]
        public IActionResult GetDadosGrafico(int? setorId, int? funcionarioId)
        {
            try
            {
                // Base da query com JOIN para acessar os usuários
                var query = from c in _context.Chamados
                            join u in _context.Usuarios on c.ID_Atendente equals u.Id into cu
                            from u in cu.DefaultIfEmpty()
                            where u == null || u.Nome != "ChatGPT" // 🔹 exclui ChatGPT
                            select new { Chamado = c, Usuario = u };

                if (setorId.HasValue)
                {
                    query = query.Where(x => x.Usuario != null && x.Usuario.ID_Setor == setorId.Value);
                }

                if (funcionarioId.HasValue)
                {
                    query = query.Where(x => x.Chamado.ID_Atendente == funcionarioId.Value);
                }

                var dadosGrafico = query
                    .GroupBy(x => x.Chamado.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Quantidade = g.Count()
                    })
                    .OrderByDescending(g => g.Quantidade)
                    .ToList();

                return Json(dadosGrafico);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message });
            }
        }

        /**
            * GetFuncionariosBySetor
            *
            * Retorna lista de funcionários de um setor específico para preencher dropdowns na interface.
            *
            * Tipo de retorno: IActionResult
            * - Retorna Json contendo Id e Nome dos funcionários filtrados, ou mensagem de erro.
            *
            * Funcionamento detalhado:
            * 1. Cria query para todos os usuários.
            * 2. Aplica filtro por setor, se fornecido.
            * 3. Exclui o usuário "ChatGPT" da lista.
            * 4. Ordena lista por nome do funcionário.
            * 5. Retorna JSON com dados ou captura exceção e retorna JSON com mensagem de erro.
            *
            * Parâmetros:
            * - int? setorId: ID do setor para filtro opcional.
        */
        
        [HttpGet]
        public IActionResult GetFuncionariosBySetor(int? setorId)
        {
            try
            {
                var funcionarios = _context.Usuarios.AsQueryable();

                if (setorId.HasValue)
                {
                    funcionarios = funcionarios.Where(f => f.ID_Setor == setorId.Value);
                }

                var lista = funcionarios
                    .Where(f => f.Nome != "ChatGPT") // 🔹 garante que ChatGPT não aparece no dropdown também
                    .Select(f => new { f.Id, f.Nome })
                    .OrderBy(f => f.Nome)
                    .ToList();

                return Json(lista);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message });
            }
        }
    }
}
