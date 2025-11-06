/**
    * ChamadoController.cs
    *
    * Controlador responsável pelo gerenciamento completo de chamados dentro do sistema.
    *
    * Contém métodos para:
    * - Abrir chamados (telas e submissão)
    * - Buscar chamados com filtros avançados
    * - Visualizar detalhes de chamados, incluindo histórico, comentários e anexos
    * - Adicionar, listar e remover comentários
    * - Fazer upload, listar e remover anexos
    * - Atualizar campos específicos de um chamado
    * - Registrar ações no histórico de chamados
    *
    * Funcionalidades detalhadas:
    * 1. AbrirChamado (GET): 
    *    - Exibe a tela para abertura de chamado.
    *    - Recupera informações do usuário logado (nome, setor) e lista de setores disponíveis.
    *
    * 2. AbrirChamado (POST):
    *    - Processa a submissão de um novo chamado.
    *    - Valida usuário, setor solicitado e existência de supervisor.
    *    - Cria o chamado, salva anexos (se houver) e registra ação no histórico.
    *
    * 3. BuscarChamados:
    *    - Retorna uma lista de chamados filtrados por parâmetros opcionais (número, título, datas,
    *      responsável, setor, status, solicitante e situação).
    *    - Diferencia chamadas do time e solicitações do usuário comum.
    *    - Calcula situação do chamado (No Prazo, Atenção, Atrasado, Finalizado).
    *
    * 4. DadosChamado:
    *    - Retorna os detalhes completos de um chamado específico.
    *    - Inclui informações do solicitante, responsável, setor, prioridade, histórico, comentários e anexos.
    *    - Calcula situação do chamado e carrega listas de critérios e prioridades para edição.
    *
    * 5. UploadAnexo:
    *    - Recebe um arquivo enviado pelo usuário e o salva no servidor.
    *    - Cria registro do anexo no banco e registra a ação no histórico.
    *
    * 6. ListaAnexos:
    *    - Retorna partial view com os anexos de um chamado específico.
    *
    * 7. RemoverAnexo:
    *    - Exclui fisicamente o arquivo do servidor e remove o registro do banco.
    *    - Registra a ação no histórico.
    *
    * 8. SalvarComentario:
    *    - Adiciona um comentário a um chamado.
    *    - Registra a ação no histórico.
    *
    * 9. ListaComentarios:
    *    - Retorna partial view com os comentários de um chamado.
    *
    * 10. RemoverComentario:
    *     - Remove um comentário de um chamado.
    *     - Registra a ação no histórico.
    *
    * 11. AtualizarCampo:
    *     - Permite atualizar dinamicamente campos de um chamado, como Status, Descrição, DataInicio,
    *       DataFim, Solicitante, Responsável, Setor, Nível de prioridade e Prioridade.
    *     - Garante consistência dos dados e registra a alteração no histórico.
    *
    * 12. RegistrarHistorico (private):
    *     - Função auxiliar para registrar qualquer ação realizada em um chamado.
    *     - Recebe ID do chamado, ID do usuário e descrição da ação.
    *
    * Classes auxiliares:
    * - AtualizacaoCampoModel: Model utilizado para envio de dados de atualização de campos via AJAX.
    *
    * Dependências:
    * - Entity Framework Core (_context) para manipulação de Chamados, Usuarios, Setores, Anexos,
    *   Comentarios e HistoricoChamado.
    * - System.IO para manipulação de arquivos no servidor.
    * - HttpContext.Session para recuperar dados do usuário logado.
    * - Microsoft.AspNetCore.Http para IFormCollection e IFormFile.
    * - Microsoft.AspNetCore.Mvc para ActionResult, JsonResult e PartialView.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIM.Models;
using PIM.Helpers;
using System;
using System.Linq;
using PIM.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PIM.Controllers
{
    public class ChamadosController : Controller
    {
        private readonly ApplicationDbContext _context;

        /**
            * Construtor da classe ChamadosController.
            *
            * Este construtor injeta uma instância do ApplicationDbContext para permitir que o controller
            * acesse o banco de dados usando Entity Framework Core.
            *
            * Parâmetros:
            *   context (ApplicationDbContext) - Instância do contexto do banco de dados que será usada
            *                                   para operações de CRUD relacionadas aos chamados.
            *
            * Uso:
            *   O ASP.NET Core automaticamente resolve e fornece a instância de ApplicationDbContext
            *   através de Dependency Injection ao criar o controller.
        */
        public ChamadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        /**
            * Exibe o painel de chamados para o usuário logado.
            *
            * Dependendo do parâmetro isChamadosTime e do cargo do usuário, o painel exibirá:
            * - Minhas solicitações: chamados que o usuário abriu.
            * - Chamados do time: chamados do setor do usuário ou de todos os setores se for diretor.
            *
            * Parâmetros:
            *   isChamadosTime (bool) - Indica se o usuário deseja visualizar os chamados do time.
            *                           Valor padrão: false (minhas solicitações).
            *
            * Comportamento:
            *   1. Armazena na sessão a escolha do painel (isChamadosTime).
            *   2. Recupera o usuário logado da sessão.
            *      - Se não houver usuário logado, redireciona para a página de login.
            *   3. Preenche ViewBags com dados auxiliares:
            *      - Responsáveis: lista de nomes distintos de usuários.
            *      - Setores: lista de setores distintos.
            *      - Status: lista fixa de status ("Aberto", "Finalizado", "Em andamento").
            *   4. Recupera os chamados conforme:
            *      - Se isChamadosTime == true:
            *          - Diretor: todos os setores.
            *          - Outros cargos: apenas chamados do setor do usuário.
            *      - Se isChamadosTime == false: apenas chamados do solicitante logado.
            *   5. Calcula o total de chamados ativos (não concluídos) e agrupa por situação:
            *      - No Prazo: até 1 dia desde DataInicio.
            *      - Atenção: entre 2 e 3 dias.
            *      - Atrasado: 4 ou mais dias.
            *   6. Define ViewBag.TipoPainel para indicar o tipo de painel renderizado.
            *
            * Retorno:
            *   IActionResult - View contendo a lista de chamados apropriada ao usuário e informações
            *                   auxiliares para exibição.
        */

        public IActionResult PainelSolicitante(bool isChamadosTime = false)
        {
            HttpContext.Session.SetString("isChamadosTime", isChamadosTime.ToString());

            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            if (usuario == null)
                return RedirectToAction("Index", "Login");

            ViewBag.Responsaveis = _context.Usuarios.Select(u => u.Nome).Distinct().ToList();
            ViewBag.Setores = _context.Setores.Select(s => s.Descricao).Distinct().ToList();
            ViewBag.Statuses = new[] { "Aberto", "Finalizado", "Em andamento" }.ToList();

            List<ChamadoComUsuario> chamados;

            int idDiretor = 10;

            if (isChamadosTime)
            {
                if (usuario.ID_Cargo == idDiretor)
                {
                    // Diretor vê todos os setores
                    chamados = _context.ChamadosComUsuarios
                        .FromSqlInterpolated($@"
                            SELECT 
                                c.Id, 
                                c.Titulo, 
                                c.DataInicio, 
                                c.DataFim,
                                s.Descricao as Setor,
                                u.Nome as Responsavel,
                                soli.Descricao as SetorSoli,
                                uSoli.Nome as Solicitante,
                                c.Status,
                                u.ID_Setor AS ID_SetorAtendente,
                                CASE 
                                    WHEN c.Status = 'Concluído' THEN 'Finalizado'
                                    WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) <= 1 THEN 'No Prazo'
                                    WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) BETWEEN 2 AND 3 THEN 'Atencao'
                                    WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) >= 4 THEN 'Atrasado'
                                    ELSE 'Indefinido'
                                END AS Situacao
                            FROM Chamados c
                            LEFT JOIN Usuarios u ON c.ID_Atendente = u.Id
                            LEFT JOIN Usuarios uSoli ON c.ID_Solicitante = uSoli.Id
                            LEFT JOIN Setores s ON u.ID_Setor = s.Id
                            LEFT JOIN Setores soli ON uSoli.ID_Setor = soli.Id
                            ORDER BY c.Id DESC")
                        .ToList();
                }
                else
                {
                    // Não diretor: só do setor do usuário
                    chamados = _context.ChamadosComUsuarios
                        .FromSqlInterpolated($@"
                            SELECT 
                                c.Id, 
                                c.Titulo, 
                                c.DataInicio, 
                                c.DataFim,
                                s.Descricao as Setor,
                                u.Nome as Responsavel,
                                u.ID_Setor AS ID_SetorAtendente,
                                soli.Descricao as SetorSoli,
                                uSoli.Nome as Solicitante,
                                c.Status,
                                CASE 
                                    WHEN c.Status = 'Concluído' THEN 'Finalizado'
                                    WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) <= 1 THEN 'No Prazo'
                                    WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) BETWEEN 2 AND 3 THEN 'Atencao'
                                    WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) >= 4 THEN 'Atrasado'
                                    ELSE 'Indefinido'
                                END AS Situacao
                            FROM Chamados c
                            LEFT JOIN Usuarios u ON c.ID_Atendente = u.Id
                            LEFT JOIN Usuarios uSoli ON c.ID_Solicitante = uSoli.Id
                            LEFT JOIN Setores s ON u.ID_Setor = s.Id
                            LEFT JOIN Setores soli ON uSoli.ID_Setor = soli.Id
                            WHERE u.ID_Setor = {usuario.ID_Setor}
                            ORDER BY c.Id DESC")
                        .ToList();
                }
            }
            else
            {
                // Minhas solicitações (independente do cargo)
                chamados = _context.ChamadosComUsuarios
                    .FromSqlInterpolated($@"
                        SELECT 
                            c.Id, 
                            c.Titulo, 
                            c.DataInicio, 
                            c.DataFim,
                            uAtendente.Nome as Responsavel,
                            uAtendente.ID_Setor AS ID_SetorAtendente,
                            sAtendente.Descricao as Setor,
                            c.Status,
                            sSolicitante.Descricao as SetorSoli,
                            uSolicitante.Nome as Solicitante,
                            CASE 
                                WHEN c.Status = 'Concluído' THEN 'Finalizado'
                                WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) <= 1 THEN 'No Prazo'
                                WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) BETWEEN 2 AND 3 THEN 'Atencao'
                                WHEN DATEDIFF(DAY, c.DataInicio, GETDATE()) >= 4 THEN 'Atrasado'
                                ELSE 'Indefinido'
                            END AS Situacao
                        FROM Chamados c
                        LEFT JOIN Usuarios uAtendente ON c.ID_Atendente = uAtendente.Id
                        LEFT JOIN Usuarios uSolicitante ON c.ID_Solicitante = uSolicitante.Id
                        LEFT JOIN Setores sAtendente ON uAtendente.ID_Setor = sAtendente.Id
                        LEFT JOIN Setores sSolicitante ON uSolicitante.ID_Setor = sSolicitante.Id
                        WHERE c.ID_Solicitante = {usuario.Id}
                        ORDER BY c.Id DESC")
                    .ToList();
            }

            ViewBag.TipoPainel = isChamadosTime ? "ChamadosDoTime" : "MinhasSolicitacoes";

            var hoje = DateTime.Now.Date;

            var chamadosAtivos = chamados
                .Where(c => c.Status != "Concluído")
                .ToList();

            var todosChamados = chamados
                .ToList();

            ViewBag.TotalChamados = todosChamados.Count;

            // calcula diferença em dias
            Func<DateTime?, int?> diferencaDias = data =>
                data.HasValue ? (int?)(hoje - data.Value.Date).TotalDays : null;

            ViewBag.NoPrazo = chamadosAtivos.Count(c =>
                diferencaDias(c.DataInicio) <= 1);

            ViewBag.Atencao = chamadosAtivos.Count(c =>
                diferencaDias(c.DataInicio) >= 2 &&
                diferencaDias(c.DataInicio) <= 3);

            ViewBag.Atrasados = chamadosAtivos.Count(c =>
                diferencaDias(c.DataInicio) >= 4);


            return View(chamados);
        }

        /**
            * Busca chamados no sistema aplicando filtros opcionais e retornando o resultado em JSON.
            *
            * O método distingue entre:
            *  - Minhas solicitações: chamados abertos pelo usuário logado.
            *  - Chamados do time: chamados do setor do usuário ou de todos os setores caso seja diretor.
            *
            * Parâmetros:
            *   numero (int?)          - Número do chamado.
            *   titulo (string)        - Título parcial do chamado.
            *   dataAbertura (DateTime?) - Data de abertura do chamado.
            *   dataFim (DateTime?)    - Data de encerramento do chamado.
            *   responsavel (string)   - Nome do responsável pelo chamado.
            *   setor (string)         - Setor do responsável.
            *   status (string)        - Status do chamado ("Aberto", "Concluído", etc.).
            *   setorSoli (string)     - Setor do solicitante (apenas se for painel do time).
            *   solicitante (string)   - Nome do solicitante (apenas se for painel do time).
            *   situacao (string)      - Situação do chamado ("No Prazo", "Atencao", "Atrasado", "Finalizado").
            *
            * Comportamento:
            * 1. Recupera o usuário logado da sessão; se não estiver autenticado, retorna erro JSON.
            * 2. Define a base da query conforme se o usuário visualiza chamados do time ou apenas suas solicitações.
            *    - Se for do time e não diretor, limita aos chamados do setor do usuário.
            * 3. Aplica filtros opcionais fornecidos nos parâmetros.
            * 4. Trai os dados para memória para calcular a propriedade "Situacao" de cada chamado:
            *    - Finalizado: Status == "Concluído".
            *    - No Prazo: até 1 dia desde DataInicio.
            *    - Atenção: 2 a 3 dias desde DataInicio.
            *    - Atrasado: mais de 3 dias desde DataInicio.
            * 5. Filtra por Situacao caso o parâmetro seja informado.
            * 6. Retorna a lista de chamados como JSON com campos formatados para exibição.
            *
            * Retorno:
            *   JsonResult - Lista de chamados filtrados e formatados para exibição na interface do usuário.
            *                Cada chamado inclui: Id, Título, DataInicio, DataFim, Responsável, Setor, Status,
            *                SetorSoli (opcional), Solicitante (opcional), Situacao.
        */
        
        [HttpGet]
        public IActionResult BuscarChamados(
            int? numero,
            string titulo,
            DateTime? dataAbertura,
            DateTime? dataFim,
            string responsavel,
            string setor,
            string status,
            string setorSoli,
            string solicitante,
            string situacao)
        {
            bool isChamadosTime = HttpContext.Session.GetString("isChamadosTime") == "True";
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            if (usuario == null)
                return Json(new { error = "Usuário não autenticado" });

            int idDiretor = 10;

            bool filtrosVazios =
                !numero.HasValue &&
                string.IsNullOrEmpty(titulo) &&
                !dataAbertura.HasValue &&
                !dataFim.HasValue &&
                string.IsNullOrEmpty(responsavel) &&
                string.IsNullOrEmpty(setor) &&
                string.IsNullOrEmpty(status) &&
                string.IsNullOrEmpty(setorSoli) &&
                string.IsNullOrEmpty(solicitante) &&
                string.IsNullOrEmpty(situacao);

            // ----------------------
            // Base query
            // ----------------------
            IQueryable<ChamadoComUsuario> queryBase;

            if (isChamadosTime)
            {
                queryBase = from c in _context.Chamados
                            join u in _context.Usuarios on c.ID_Atendente equals u.Id into at
                            from u in at.DefaultIfEmpty()
                            join uSoli in _context.Usuarios on c.ID_Solicitante equals uSoli.Id into soli
                            from uSoli in soli.DefaultIfEmpty()
                            join s in _context.Setores on u.ID_Setor equals s.Id into stAt
                            from s in stAt.DefaultIfEmpty()
                            join sSoli in _context.Setores on uSoli.ID_Setor equals sSoli.Id into stSoli
                            from sSoli in stSoli.DefaultIfEmpty()
                            select new ChamadoComUsuario
                            {
                                Id = c.Id,
                                Titulo = c.Titulo,
                                DataInicio = c.DataInicio,
                                DataFim = c.DataFim,
                                Setor = s != null ? s.Descricao : "",
                                Responsavel = u != null ? u.Nome : "",
                                SetorSoli = sSoli != null ? sSoli.Descricao : "",
                                Solicitante = uSoli != null ? uSoli.Nome : "",
                                Status = c.Status,
                                ID_SetorAtendente = u != null ? u.ID_Setor : 0
                            };

                // Se não for diretor, limitar ao setor do usuário
                if (usuario.ID_Cargo != idDiretor)
                    queryBase = queryBase.Where(x => x.ID_SetorAtendente == usuario.ID_Setor);
            }
            else
            {
                // Usuário comum: apenas minhas solicitações
                queryBase = from c in _context.Chamados
                            join u in _context.Usuarios on c.ID_Atendente equals u.Id into at
                            from u in at.DefaultIfEmpty()
                            join s in _context.Setores on u.ID_Setor equals s.Id into st
                            from s in st.DefaultIfEmpty()
                            where c.ID_Solicitante == usuario.Id
                            select new ChamadoComUsuario
                            {
                                Id = c.Id,
                                Titulo = c.Titulo,
                                DataInicio = c.DataInicio,
                                DataFim = c.DataFim,
                                Responsavel = u != null ? u.Nome : "",
                                Setor = s != null ? s.Descricao : "",
                                Status = c.Status,
                                SetorSoli = "",
                                Solicitante = "",
                                ID_SetorAtendente = 0
                            };
            }

            // ----------------------
            // Aplica filtros
            // ----------------------
            if (numero.HasValue)
                queryBase = queryBase.Where(c => c.Id == numero.Value);

            if (!string.IsNullOrEmpty(titulo))
                queryBase = queryBase.Where(c => EF.Functions.Like(c.Titulo, $"%{titulo}%"));

            if (dataAbertura.HasValue)
                queryBase = queryBase.Where(c => c.DataInicio.Date == dataAbertura.Value.Date);

            if (dataFim.HasValue)
                queryBase = queryBase.Where(c => c.DataFim.HasValue && c.DataFim.Value.Date == dataFim.Value.Date);

            if (!string.IsNullOrEmpty(responsavel) && responsavel != "Selecione uma opção")
                queryBase = queryBase.Where(c => EF.Functions.Like(c.Responsavel, $"%{responsavel}%"));

            if (!string.IsNullOrEmpty(setor) && setor != "Selecione uma opção")
                queryBase = queryBase.Where(c => EF.Functions.Like(c.Setor, $"%{setor}%"));

            if (!string.IsNullOrEmpty(setorSoli) && setorSoli != "Selecione uma opção")
                queryBase = queryBase.Where(c => EF.Functions.Like(c.SetorSoli, $"%{setorSoli}%"));

            if (!string.IsNullOrEmpty(solicitante) && solicitante != "Selecione uma opção")
                queryBase = queryBase.Where(c => EF.Functions.Like(c.Solicitante, $"%{solicitante}%"));

            if (!string.IsNullOrEmpty(status) && status != "Selecione uma opção")
                queryBase = queryBase.Where(c => c.Status == status);

            // Trazer para memória para calcular Situacao
            var lista = queryBase.OrderByDescending(c => c.Id).ToList();

            var resultados = lista.Select(c =>
            {
                string calcSituacao = c.Status == "Concluído"
                    ? "Finalizado"
                    : (DateTime.Now - c.DataInicio).TotalDays <= 1 ? "No Prazo"
                    : (DateTime.Now - c.DataInicio).TotalDays >= 2 && (DateTime.Now - c.DataInicio).TotalDays <= 3 ? "Atencao"
                    : "Atrasado";

                // Filtra por situacao se informado
                if (!string.IsNullOrEmpty(situacao) && situacao != "Selecione uma opção" && calcSituacao != situacao)
                    return null;

                return new
                {
                    c.Id,
                    c.Titulo,
                    DataInicio = c.DataInicio.ToString("dd/MM/yyyy HH:mm"),
                    DataFim = c.DataFim.HasValue ? c.DataFim.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                    c.Responsavel,
                    c.Setor,
                    c.Status,
                    SetorSoli = isChamadosTime ? c.SetorSoli : null,
                    Solicitante = isChamadosTime ? c.Solicitante : null,
                    Situacao = calcSituacao
                };
            }).Where(x => x != null).ToList();

            return Json(resultados);
        }

        /**
            * Busca os nomes dos responsáveis (usuários) de um setor específico.
            *
            * Este método é utilizado para popular listas ou filtros dependentes do setor selecionado.
            * Retorna apenas nomes distintos e ignora usuários com o nome "ChatGPT".
            *
            * Parâmetros:
            *   setor (string) - Descrição do setor para o qual se deseja buscar os responsáveis.
            *                    Se for null ou vazio, retorna uma lista vazia.
            *
            * Comportamento:
            * 1. Verifica se o parâmetro setor está vazio; se sim, retorna lista vazia em JSON.
            * 2. Utiliza Entity Framework Core para buscar todos os usuários cujo Setor tenha a
            *    descrição igual ao parâmetro informado e cujo nome não seja "ChatGPT".
            * 3. Seleciona apenas os nomes distintos dos usuários encontrados.
            * 4. Retorna o resultado como JSON.
            *
            * Retorno:
            *   JsonResult - Lista de strings contendo os nomes dos responsáveis do setor informado.
        */

        [HttpGet]
        public JsonResult BuscarResponsaveisPorSetor(string setor)
        {
            if (string.IsNullOrEmpty(setor))
                return Json(new List<string>());

            // Garante que carrega o Setor junto para comparação
            var responsaveis = _context.Usuarios
                .Include(u => u.Setor) // importante para EF Core
                .Where(u => u.Setor != null && u.Setor.Descricao == setor && u.Nome != "ChatGPT")
                .Select(u => u.Nome)
                .Distinct()
                .ToList();

            return Json(responsaveis);
        }

        /**
            * Recupera todos os dados detalhados de um chamado específico para exibição na tela de detalhes.
            *
            * Este método busca o chamado pelo ID fornecido e retorna informações completas,
            * incluindo solicitante, responsável, setores, prioridade, histórico, comentários e anexos.
            * Também calcula a situação do chamado com base na data de início e no status.
            *
            * Parâmetros:
            *   id (int) - ID do chamado a ser buscado.
            *   tipoPainel (string) - Indica o tipo de painel de origem (por exemplo, "ChamadosDoTime" ou "MinhasSolicitacoes").
            *
            * Comportamento:
            * 1. Atribui o tipo de painel à ViewBag para referência na View.
            * 2. Busca o chamado no banco de dados, incluindo:
            *    - Solicitante e responsável
            *    - Setores do responsável e do solicitante
            *    - Critérios e prioridades
            *    - Comentários do chamado
            *    - Histórico de ações do chamado
            *    - Anexos associados
            * 3. Calcula a situação do chamado:
            *    - "Finalizado" se Status == "Concluído"
            *    - "No Prazo" se até 1 dia desde DataInicio
            *    - "Atencao" se 2-3 dias desde DataInicio
            *    - "Atrasado" se mais de 3 dias desde DataInicio
            * 4. Cria um ChamadoDetalhesViewModel com todos os dados.
            * 5. Busca funcionários do setor responsável para exibição em dropdowns.
            * 6. Busca dados do usuário logado, incluindo cargo, para exibição na View.
            *
            * Retorno:
            *   IActionResult - Retorna a View "dadosChamado" com um objeto ChamadoDetalhesViewModel preenchido.
            *                  Se o chamado não for encontrado, retorna NotFound().
        */
        
        public IActionResult DadosChamado(int id, string tipoPainel)
        {
            ViewBag.TipoPainel = tipoPainel;

            // Trazer o chamado do banco
            var chamadoDb = (from c in _context.Chamados
                             join solicitante in _context.Usuarios on c.ID_Solicitante equals solicitante.Id
                             join atendente in _context.Usuarios on c.ID_Atendente equals atendente.Id into atJoin
                             from atendente in atJoin.DefaultIfEmpty()
                             join setorAtendente in _context.Setores on atendente.ID_Setor equals setorAtendente.Id into stAtJoin
                             from setorAt in stAtJoin.DefaultIfEmpty()
                             join setorSolicitante in _context.Setores on solicitante.ID_Setor equals setorSolicitante.Id into stSoliJoin
                             from setorSoli in stSoliJoin.DefaultIfEmpty()
                             join criterio in _context.CriterioPrioridades on c.ID_CriterioPrioridades equals criterio.Id into criterioJoin
                             from criterioDesc in criterioJoin.DefaultIfEmpty()
                             join prioridade in _context.Prioridades on c.PrioridadeId equals prioridade.Id into prioridadeJoin
                             from prioridadeDesc in prioridadeJoin.DefaultIfEmpty()
                             where c.Id == id
                             select new
                             {
                                 Chamado = c,
                                 SolicitanteNome = solicitante.Nome,
                                 ResponsavelNome = atendente != null ? atendente.Nome : "Não atribuído",
                                 SetorResponsavelDescricao = setorAt != null ? setorAt.Descricao : "Não definido",
                                 SetorSolicitanteDescricao = setorSoli != null ? setorSoli.Descricao : "Não definido",
                                 NivelPrioridadeDescricao = criterioDesc != null ? criterioDesc.Descricao : "Não definido",
                                 PrioridadeDescricao = prioridadeDesc != null ? prioridadeDesc.Nome : "Não definido",
                                 SetorResponsavelId = setorAt != null ? setorAt.Id : 0,

                                 Comentarios = (from cm in _context.Comentarios
                                                join u in _context.Usuarios on cm.ID_Usuarios equals u.Id
                                                where cm.ID_Chamados == c.Id
                                                orderby cm.Data descending
                                                select new ComentarioViewModel
                                                {
                                                    Id = cm.Id,
                                                    NomeUsuario = u.Nome,
                                                    IdUsuario = u.Id,
                                                    Texto = cm.Texto,
                                                    Data = cm.Data
                                                }).ToList(),

                                 Historico = (from h in _context.HistoricoChamado
                                              join u in _context.Usuarios on h.ID_Usuario equals u.Id
                                              where h.ID_Chamado == c.Id
                                              orderby h.Data descending
                                              select new HistoricoViewModel
                                              {
                                                  NomeUsuario = u.Nome,
                                                  IdUsuario = u.Id,
                                                  AcaoTomada = h.AcaoTomada,
                                                  Data = h.Data
                                              }).ToList(),

                                 Anexos = (from a in _context.Anexos
                                           join u in _context.Usuarios on a.ID_Usuario equals u.Id
                                           where a.ID_Chamado == c.Id
                                           orderby a.ID descending
                                           select new AnexoViewModel
                                           {
                                               Id = a.ID,
                                               NomeArquivo = a.NomeArquivo,
                                               CaminhoArquivo = a.CaminhoArquivo,
                                               NomeUsuario = u.Nome,
                                               IdUsuario = a.ID_Usuario,
                                               DataEnvio = a.Data
                                           }).ToList()
                             })
                            .FirstOrDefault();

            if (chamadoDb == null)
                return NotFound();

            // Calcular a Situação em C# após buscar os dados
            var hoje = DateTime.Now.Date;
            string situacao;
            if (chamadoDb.Chamado.Status == "Concluído")
                situacao = "Finalizado";
            else if ((hoje - chamadoDb.Chamado.DataInicio.Date).TotalDays <= 1)
                situacao = "No Prazo";
            else if ((hoje - chamadoDb.Chamado.DataInicio.Date).TotalDays <= 3)
                situacao = "Atencao";
            else
                situacao = "Atrasado";

            // Montar ViewModel
            var criterios = _context.CriterioPrioridades
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Descricao} (Nível {c.Nivel})"
                }).ToList();

            var prioridades = _context.Prioridades
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Nome
                }).ToList();

            var chamadoViewModel = new ChamadoDetalhesViewModel
            {
                Id = chamadoDb.Chamado.Id,
                Titulo = chamadoDb.Chamado.Titulo,
                Descricao = chamadoDb.Chamado.Descricao,
                DataInicio = chamadoDb.Chamado.DataInicio,
                DataFim = chamadoDb.Chamado.DataFim,
                Status = chamadoDb.Chamado.Status,
                Solicitante = chamadoDb.SolicitanteNome,
                Responsavel = chamadoDb.ResponsavelNome,
                ResponsavelId = chamadoDb.Chamado.ID_Atendente,
                Setor = chamadoDb.SetorResponsavelDescricao,
                SetorSoli = chamadoDb.SetorSolicitanteDescricao,
                NivelPrioridadeDescricao = chamadoDb.NivelPrioridadeDescricao,
                PrioridadeDescricao = chamadoDb.PrioridadeDescricao,
                Comentarios = chamadoDb.Comentarios,
                Historico = chamadoDb.Historico,
                Anexos = chamadoDb.Anexos,
                NivelPrioridadeId = chamadoDb.Chamado.ID_CriterioPrioridades,
                PrioridadeId = chamadoDb.Chamado.PrioridadeId,
                Criterios = criterios,
                Prioridades = prioridades,
                SolicitanteId = chamadoDb.Chamado.ID_Solicitante,
                Situacao = situacao
            };

            // Buscar funcionários do setor responsável
            int setorResponsavelId = chamadoDb.SetorResponsavelId;
            var funcionariosSetorResponsavel = _context.Usuarios
                .Where(u => u.ID_Setor == setorResponsavelId)
                .Select(u => new FuncionarioViewModel
                {
                    Id = u.Id,
                    Nome = u.Nome
                }).ToList();

            ViewBag.FuncionariosSetorResponsavel = funcionariosSetorResponsavel;

            // Dados do usuário logado
            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");

            if (usuarioLogado != null)
            {
                var cargoUsuario = (from u in _context.Usuarios
                                    join c in _context.Cargos on u.ID_Cargo equals c.Id
                                    where u.Id == usuarioLogado.Id
                                    select c.Descricao).FirstOrDefault();

                ViewBag.CargoUsuarioLogado = cargoUsuario ?? "";
            }
            else
            {
                ViewBag.CargoUsuarioLogado = "";
            }

            ViewBag.UsuarioLogadoId = usuarioLogado?.Id;

            return View("dadosChamado", chamadoViewModel);
        }

        /**
            * Faz o upload de um arquivo anexo para um chamado específico.
            *
            * Este método recebe um arquivo enviado pelo usuário, salva-o em uma pasta
            * organizada por ID do chamado e registra o anexo no banco de dados. Também
            * adiciona uma entrada no histórico do chamado indicando a ação.
            *
            * Parâmetros:
            *   idChamado (int) - ID do chamado ao qual o anexo será vinculado.
            *   arquivo (IFormFile) - Arquivo enviado pelo usuário.
            *
            * Comportamento:
            * 1. Recupera o usuário logado da sessão.
            * 2. Valida se o arquivo foi enviado; se não, retorna BadRequest.
            * 3. Gera um nome único para o arquivo com GUID para evitar conflitos.
            * 4. Cria a pasta de destino em "wwwroot/uploads/chamados/{idChamado}" caso não exista.
            * 5. Salva o arquivo no diretório do servidor.
            * 6. Registra o anexo no banco de dados, incluindo nome, caminho relativo, formato, ID do usuário e data.
            * 7. Registra histórico do chamado informando que o arquivo foi anexado.
            *
            * Retorno:
            *   IActionResult - Retorna:
            *     - Ok(string) em caso de sucesso com mensagem "Arquivo salvo com sucesso."
            *     - BadRequest(string) se nenhum arquivo for enviado.
            *
            * Observações:
            * - Garante que cada arquivo tenha um nome único utilizando GUID.
            * - O caminho relativo do arquivo é armazenado no banco para referência futura.
        */
        
        [HttpPost]
        public async Task<IActionResult> UploadAnexo(int idChamado, IFormFile arquivo)
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");

            if (arquivo == null || arquivo.Length == 0)
                return BadRequest("Nenhum arquivo recebido.");

            var extensao = Path.GetExtension(arquivo.FileName);
            var nomeBase = Path.GetFileNameWithoutExtension(arquivo.FileName);
            var nomeUnico = $"{nomeBase}_{Guid.NewGuid()}{extensao}";

            var pastaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/chamados", idChamado.ToString());
            if (!Directory.Exists(pastaDestino))
                Directory.CreateDirectory(pastaDestino);

            var caminhoCompleto = Path.Combine(pastaDestino, nomeUnico);
            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            var caminhoRelativo = $"/uploads/chamados/{idChamado}/{nomeUnico}";

            var anexo = new Anexo
            {
                NomeArquivo = nomeUnico,
                CaminhoArquivo = caminhoRelativo,
                Formato = extensao,
                ID_Chamado = idChamado,
                ID_Usuario = usuario.Id,
                Data = DateTime.Now
            };

            _context.Anexos.Add(anexo);
            await _context.SaveChangesAsync();

            await RegistrarHistorico(idChamado, usuario.Id, $"Anexou o arquivo \"{arquivo.FileName}\"");

            return Ok("Arquivo salvo com sucesso.");
        }

        /**
            * Retorna a lista de anexos de um chamado específico.
            *
            * Este método busca todos os anexos associados a um determinado chamado,
            * ordena-os pela data de envio em ordem decrescente e transforma cada
            * registro em um ViewModel apropriado para exibição parcial na interface.
            *
            * Parâmetros:
            *   idChamado (int) - ID do chamado cujos anexos serão listados.
            *
            * Comportamento:
            * 1. Consulta o banco de dados para recuperar todos os anexos relacionados ao chamado.
            * 2. Ordena os anexos pela data de envio, do mais recente para o mais antigo.
            * 3. Projeta cada anexo em um `AnexoViewModel`, contendo:
            *    - Id
            *    - NomeArquivo
            *    - CaminhoArquivo
            *    - NomeUsuario (nome do usuário que anexou o arquivo)
            *    - IdUsuario
            *    - DataEnvio
            * 4. Retorna uma PartialView `_ListaAnexos` com a lista de anexos.
            *
            * Retorno:
            *   Task<IActionResult> - PartialView contendo a lista de anexos.
            *
            * Observações:
            * - O método é assíncrono para não bloquear a thread da aplicação durante a consulta ao banco.
            * - Ideal para atualizar dinamicamente a seção de anexos via AJAX.
        */
        
        public async Task<IActionResult> ListaAnexos(int idChamado)
        {
            var anexos = await _context.Anexos
                            .Where(a => a.ID_Chamado == idChamado)
                            .OrderByDescending(a => a.Data)
                            .Select(a => new AnexoViewModel
                            {
                                Id = a.ID,
                                NomeArquivo = a.NomeArquivo,
                                CaminhoArquivo = a.CaminhoArquivo,
                                NomeUsuario = a.Usuario.Nome,
                                IdUsuario = a.ID_Usuario,
                                DataEnvio = a.Data
                            })
                            .ToListAsync();

            return PartialView("_ListaAnexos", anexos);
        }

        /**
            * Método HTTP POST responsável por remover um anexo de um chamado no sistema.
            * 
            * Fluxo:
            * 1. Recupera o usuário da sessão atual.
            * 2. Busca o anexo no banco de dados a partir do ID informado.
            * 3. Caso o anexo não seja encontrado, retorna `NotFound()`.
            * 4. Monta o caminho físico do arquivo salvo em `wwwroot` e verifica se o arquivo existe.
            *    - Se existir, o arquivo físico é excluído.
            * 5. Remove o anexo da base de dados e salva as alterações.
            * 6. Registra no histórico do chamado a ação do usuário que removeu o anexo.
            * 7. Retorna `Ok()` indicando sucesso.
            * 
            * Parâmetros:
            * @param id - Identificador único do anexo a ser removido.
            * 
            * Retorno:
            * - `Ok()` em caso de sucesso.
            * - `NotFound()` caso o anexo não exista.
        */

        [HttpPost]
        public async Task<IActionResult> RemoverAnexo(int id)
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            var anexo = await _context.Anexos.FindAsync(id);
            if (anexo == null)
                return NotFound();

            var caminhoFisico = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", anexo.CaminhoArquivo.TrimStart('/'));
            if (System.IO.File.Exists(caminhoFisico))
                System.IO.File.Delete(caminhoFisico);

            _context.Anexos.Remove(anexo);
            await _context.SaveChangesAsync();

            await RegistrarHistorico(anexo.ID_Chamado, usuario.Id, $"Removeu o anexo \"{anexo.NomeArquivo}\"");

            return Ok();
        }

        /**
            * Salva um novo comentário em um chamado específico.
            * 
            * Fluxo:
            * 1. Recupera o usuário autenticado da sessão.
            * 2. Valida se o usuário está autenticado e se o texto do comentário não está vazio.
            * 3. Verifica se o chamado informado existe no banco de dados.
            * 4. Cria e persiste um novo comentário relacionado ao chamado e ao usuário.
            * 5. Registra no histórico a ação de adicionar um comentário.
            * 6. Redireciona para a página de detalhes do chamado.
            *
            * @param chamadoId Identificador único do chamado onde o comentário será adicionado.
            * @param texto Conteúdo textual do comentário a ser salvo.
            * @return Retorna um BadRequest em caso de falha (usuário não autenticado, texto inválido ou chamado inexistente),
            *         ou redireciona para a ação `DadosChamado` em caso de sucesso.
        */

        [HttpPost]
        public async Task<IActionResult> SalvarComentario(int chamadoId, string texto)
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");

            if (usuario == null || string.IsNullOrWhiteSpace(texto))
                return BadRequest("Usuário não autenticado ou comentário vazio.");

            // Verifica se o chamado existe antes de salvar o comentário
            var chamado = await _context.Chamados.FindAsync(chamadoId);
            if (chamado == null)
                return BadRequest("Chamado não encontrado.");

            var comentario = new Comentario
            {
                Texto = texto.Trim(),
                Data = DateTime.Now,
                ID_Chamados = chamadoId,
                ID_Usuarios = usuario.Id
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            await RegistrarHistorico(chamadoId, usuario.Id, "Adicionou um comentário");

            return RedirectToAction("DadosChamado", new { id = chamadoId });
        }

        /**
            * Recupera e exibe a lista de comentários associados a um chamado específico.
            * 
            * Este método realiza as seguintes operações:
            * - Obtém o usuário logado da sessão atual para identificar seu ID.
            * - Filtra os comentários relacionados ao chamado informado pelo parâmetro `idChamado`.
            * - Ordena os comentários em ordem decrescente de data.
            * - Converte os resultados em uma lista de objetos `ComentarioViewModel`, contendo
            *   informações do comentário, data, texto, ID do usuário e nome do autor.
            * - Armazena o ID do usuário logado em `ViewBag.UsuarioLogadoId` para uso na View.
            * - Retorna a PartialView `_ListaComentarios` preenchida com os comentários filtrados.
            * 
            * @param idChamado ID do chamado cujos comentários devem ser listados.
            * @return PartialView `_ListaComentarios` contendo a lista de comentários.
        */

        [HttpGet]
        public IActionResult ListaComentarios(int idChamado)
        {
            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            ViewBag.UsuarioLogadoId = usuarioLogado?.Id;

            var comentarios = _context.Comentarios
                .Where(c => c.ID_Chamados == idChamado)
                .OrderByDescending(c => c.Data)
                .Select(c => new ComentarioViewModel
                {
                    Id = c.Id,
                    NomeUsuario = c.Usuario.Nome,
                    IdUsuario = c.ID_Usuarios,
                    Data = c.Data,
                    Texto = c.Texto
                }).ToList();

            return PartialView("_ListaComentarios", comentarios);
        }

        /**
            * Remove um comentário específico do chamado.
            *
            * Este método busca o comentário pelo ID recebido no formulário. Caso o comentário exista, 
            * ele é removido do banco de dados e a ação é registrada no histórico do chamado.
            *
            * Fluxo:
            * 1. Recupera o usuário logado da sessão.
            * 2. Localiza o comentário no banco pelo ID informado.
            * 3. Se não encontrado, retorna NotFound().
            * 4. Remove o comentário encontrado.
            * 5. Salva as alterações no banco de dados.
            * 6. Registra no histórico a ação de remoção do comentário.
            * 7. Retorna Ok() em caso de sucesso.
            *
            * @param id Identificador único do comentário a ser removido.
            * @return IActionResult Retorna Ok() se a operação foi concluída ou NotFound() se o comentário não existir.
        */

        [HttpPost]
        public async Task<IActionResult> RemoverComentario([FromForm] int id)
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            var comentario = await _context.Comentarios.FindAsync(id);
            if (comentario == null)
                return NotFound();

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            await RegistrarHistorico(comentario.ID_Chamados, usuario.Id, "Removeu um comentário");

            return Ok();
        }

        /**
            * Atualiza dinamicamente um campo específico de um chamado no sistema.
            *
            * IActionResult (assíncrono): Retorna Ok() em caso de atualização bem-sucedida, 
            * NotFound() se o chamado não for encontrado, ou BadRequest() em situações de erro de validação.
            *
            * Funcionamento:
            * 1. Recupera o usuário logado a partir da sessão.
            * 2. Busca o chamado no banco de dados pelo ID informado no objeto recebido.
            * 3. Verifica se o chamado existe; caso contrário, retorna NotFound().
            * 4. Identifica qual campo será atualizado com base no parâmetro `dados.Campo`.
            *    - Status → altera o status e ajusta a DataFim automaticamente (regras de negócio).
            *    - Descricao → atualiza o texto da descrição.
            *    - DataInicio / DataFim → converte string recebida em DateTime e salva.
            *    - Solicitante → busca usuário correspondente e atualiza ID.
            *    - Responsavel → valida ID numérico, atualiza responsável e registra nomes antigo/novo.
            *    - Setor → não permite alteração direta, apenas valida existência.
            *    - Nivel (Critério de prioridade) → valida ID, substitui pelo novo critério e registra alteração.
            *    - Prioridade → valida ID, substitui pela nova prioridade e registra alteração.
            *    - Demais valores → retorna erro de campo não suportado.
            * 5. Salva as mudanças no banco de dados.
            * 6. Registra no histórico do chamado a modificação realizada, exibindo valores antigo e novo.
            * 7. Retorna Ok() em caso de sucesso.
            *
            * Parâmetros:
            * @param dados (AtualizacaoCampoModel) Objeto contendo:
            *        - Id (int): Identificador do chamado a ser atualizado.
            *        - Campo (string): Nome do campo que será modificado.
            *        - Valor (string): Novo valor a ser atribuído ao campo.
            *
            * Dependências:
            * - Entity Framework Core para consultas e persistência de dados.
            * - Sessão HTTP para recuperar usuário logado.
            * - Método auxiliar RegistrarHistorico() para rastrear alterações.
        */

        [HttpPost]
        public async Task<IActionResult> AtualizarCampo([FromBody] AtualizacaoCampoModel dados)
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            var chamado = await _context.Chamados.FindAsync(dados.Id);
            if (chamado == null) return NotFound();

            string valorAntigo = "";
            string valorNovo = dados.Valor;

            switch (dados.Campo)
            {
                case "Status":
                    valorAntigo = chamado.Status;
                    chamado.Status = dados.Valor;

                    if (dados.Valor == "Concluído")
                    {
                        chamado.DataFim = DateTime.Now; // Regra 1
                    }
                    else
                    {
                        chamado.DataFim = null; // Regra 2
                    }
                    break;
                case "Descricao":
                    valorAntigo = chamado.Descricao;
                    chamado.Descricao = dados.Valor;
                    break;
                case "DataInicio":
                    valorAntigo = chamado.DataInicio.ToString("dd/MM/yyyy");
                    if (DateTime.TryParse(dados.Valor, out var dataInicio))
                        chamado.DataInicio = dataInicio;
                    break;
                case "DataFim":
                    valorAntigo = chamado.DataFim?.ToString("dd/MM/yyyy") ?? "-";
                    if (DateTime.TryParse(dados.Valor, out var dataFim))
                        chamado.DataFim = dataFim;
                    break;
                case "Solicitante":
                    var solicitanteUsuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Nome == dados.Valor);
                    if (solicitanteUsuario == null)
                        return BadRequest("Solicitante inválido");
                    valorAntigo = "ID " + chamado.ID_Solicitante.ToString();
                    chamado.ID_Solicitante = solicitanteUsuario.Id;
                    break;
                case "Responsavel":
                    if (!int.TryParse(dados.Valor, out int novoResponsavelId))
                        return BadRequest("Responsável inválido");

                    var novoResponsavel = await _context.Usuarios.FindAsync(novoResponsavelId);
                    if (novoResponsavel == null)
                        return BadRequest("Responsável não encontrado");

                    var responsavelAntigo = await _context.Usuarios.FindAsync(chamado.ID_Atendente);
                    string nomeResponsavelAntigo = responsavelAntigo != null ? responsavelAntigo.Nome : "Não atribuído";

                    valorAntigo = nomeResponsavelAntigo;
                    valorNovo = novoResponsavel.Nome;

                    chamado.ID_Atendente = novoResponsavel.Id;
                    break;
                case "Setor":
                    var setor = await _context.Setores.FirstOrDefaultAsync(s => s.Descricao == dados.Valor);
                    if (setor == null)
                        return BadRequest("Setor inválido");
                    valorAntigo = "(Sem atualização direta, setor vinculado ao atendente)";
                    break;
                case "Nivel":
                    if (!int.TryParse(dados.Valor, out int novoCriterioId))
                        return BadRequest("Nível inválido");

                    var criterio = await _context.CriterioPrioridades.FindAsync(novoCriterioId);
                    if (criterio == null)
                        return BadRequest("Nível não encontrado");

                    var criterioAntigo = await _context.CriterioPrioridades.FindAsync(chamado.ID_CriterioPrioridades);
                    string descricaoAntiga = criterioAntigo != null ? criterioAntigo.Descricao : "Não definido";

                    valorAntigo = descricaoAntiga;
                    valorNovo = criterio.Descricao;

                    chamado.ID_CriterioPrioridades = criterio.Id;
                    break;
                case "Prioridade":
                    if (!int.TryParse(dados.Valor, out int novaPrioridadeId))
                        return BadRequest("Prioridade inválida");

                    var prioridade = await _context.Prioridades.FindAsync(novaPrioridadeId);
                    if (prioridade == null)
                        return BadRequest("Prioridade não encontrada");

                    var prioridadeAntiga = await _context.Prioridades.FindAsync(chamado.PrioridadeId);
                    string descricaoPrioridadeAntiga = prioridadeAntiga != null ? prioridadeAntiga.Nome : "Não definido";

                    valorAntigo = descricaoPrioridadeAntiga;
                    valorNovo = prioridade.Nome;

                    chamado.PrioridadeId = prioridade.Id;
                    break;
                default:
                    return BadRequest("Campo não suportado.");
            }

            await _context.SaveChangesAsync();

            await RegistrarHistorico(chamado.Id, usuario.Id, $"Alterou o campo {dados.Campo} de \"{valorAntigo}\" para \"{valorNovo}\"");

            return Ok();
        }
        
        // Representa o modelo utilizado para atualização de um campo específico em um registro.
        public class AtualizacaoCampoModel
        {
            public int Id { get; set; }
            public string Campo { get; set; }
            public string Valor { get; set; }
        }

        /**
            * Registra uma entrada no histórico de um chamado no sistema.
            *
            * Task: Retorna uma Task assíncrona representando a operação de persistência no banco de dados.
            *
            * Este método cria um objeto do tipo HistoricoChamado associando o ID do chamado, 
            * o ID do usuário que executou a ação, a descrição da ação e a data/hora atual. 
            * Em seguida, adiciona o objeto ao contexto do Entity Framework e salva as alterações 
            * no banco de dados de forma assíncrona.
            *
            * Parâmetros:
            * - idChamado: Identificador do chamado que sofrerá o registro no histórico.
            * - idUsuario: Identificador do usuário que executou a ação.
            * - acao: Texto descrevendo a ação realizada pelo usuário.
            *
            * Bibliotecas e dependências:
            * - Utiliza Entity Framework Core para persistência de dados (_context).
            * - Dependência de HistoricoChamado como entidade mapeada para o banco.
        */

        private async Task RegistrarHistorico(int idChamado, int idUsuario, string acao)
        {
            var historico = new HistoricoChamado
            {
                ID_Chamado = idChamado,
                ID_Usuario = idUsuario,
                AcaoTomada = acao,
                Data = DateTime.Now
            };

            _context.HistoricoChamado.Add(historico);
            await _context.SaveChangesAsync();
        }

        /**
            * Recupera o histórico de ações de um chamado específico.
            *
            * IActionResult: Retorna uma PartialView contendo a lista de ações realizadas no chamado.
            *
            * Este método consulta o banco de dados utilizando Entity Framework Core para obter todas as 
            * entradas de histórico associadas ao ID do chamado fornecido. As entradas são ordenadas 
            * de forma decrescente pela data de execução da ação. Em seguida, cada entrada é projetada 
            * em um objeto HistoricoViewModel contendo o nome do usuário que executou a ação, seu ID, 
            * a ação tomada e a data correspondente.
            *
            * Parâmetros:
            * - idChamado: Identificador do chamado cujo histórico será recuperado.
            *
            * Bibliotecas e dependências:
            * - Entity Framework Core (_context) para consultas ao banco.
            * - Dependência de HistoricoChamado como entidade mapeada no banco.
            * - HistoricoViewModel para projetar os dados retornados.
        */
 
        [HttpGet]
        public IActionResult ListaHistorico(int idChamado)
        {
            var historico = _context.HistoricoChamado
                .Where(h => h.ID_Chamado == idChamado)
                .OrderByDescending(h => h.Data)
                .Select(h => new HistoricoViewModel
                {
                    NomeUsuario = h.Usuario.Nome,
                    IdUsuario = h.Usuario.Id,
                    AcaoTomada = h.AcaoTomada,
                    Data = h.Data
                }).ToList();

            return PartialView("_ListaHistorico", historico);
        }

        /**
            * Exibe a página de abertura de um novo chamado.
            *
            * IActionResult: Retorna a View de abertura de chamado, populando dados do usuário e dos setores.
            *
            * Este método verifica se há um usuário autenticado na sessão. Se não houver, redireciona para 
            * a página de login. Caso o usuário esteja autenticado, ele preenche o ViewBag com o nome do 
            * solicitante e a descrição do setor associado ao usuário. Também recupera a lista de todos 
            * os setores do banco de dados e a atribui ao ViewBag.Setores, permitindo que sejam exibidos 
            * em campos de seleção na View.
            *
            * Bibliotecas e dependências:
            * - Entity Framework Core (_context) para consulta à tabela Setores.
            * - Sessão HttpContext.Session para recuperação de informações do usuário.
            * - Dependência da classe Usuario para mapear informações do usuário logado.
        */

        public IActionResult AbrirChamado()
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");

            if (usuario == null)
                return RedirectToAction("Login", "Usuario");

            ViewBag.NomeSolicitante = usuario.Nome;
            ViewBag.SetorSolicitante = usuario.Setor?.Descricao;

            var setores = _context.Setores.ToList();
            ViewBag.Setores = setores;

            return View();
        }

        /**
            * Processa a submissão de um novo chamado, salvando os dados do chamado, anexos e histórico.
            *
            * Task<IActionResult>: Retorna um redirecionamento para o painel do solicitante ou para a página
            * de abertura de chamado em caso de erro.
            *
            * Este método realiza as seguintes operações passo a passo:
            * 1. Recupera o usuário logado da sessão. Se não houver usuário autenticado, redireciona
            *    para a página de login.
            * 2. Lê os dados do formulário, incluindo o setor solicitado, título e descrição do chamado.
            * 3. Valida se o setor solicitado existe no banco de dados. Se não existir, retorna à página
            *    de abertura de chamado com uma mensagem de erro.
            * 4. Busca o supervisor do setor solicitado para atribuir automaticamente como atendente.
            *    Se não houver supervisor, retorna à página de abertura de chamado com mensagem de erro.
            * 5. Cria uma instância do objeto Chamado, atribuindo os dados do formulário, data atual,
            *    status "Aberto", solicitante logado, prioridade padrão e o supervisor como atendente.
            * 6. Salva o chamado no banco de dados e obtém o ID gerado.
            * 7. Se houver arquivos anexados, cria a pasta de uploads se necessário, salva cada arquivo
            *    no servidor com nome único e registra os dados no banco de anexos (Anexo).
            * 8. Registra a ação no histórico do chamado (HistoricoChamado), informando que o chamado
            *    foi aberto pelo usuário.
            * 9. Salva todas as alterações no banco de dados.
            * 10. Exibe uma mensagem de sucesso via TempData e redireciona para o PainelSolicitante.
            *
            * Parâmetros:
            * - IFormCollection form: Contém os campos do formulário de abertura do chamado (título,
            *   descrição, setor solicitado, etc.).
            * - List<IFormFile> inputAnexo: Lista de arquivos enviados pelo usuário como anexos.
            *
            * Bibliotecas e dependências:
            * - Entity Framework Core (_context) para persistência de Chamados, Anexos e Histórico.
            * - System.IO para manipulação de arquivos no servidor.
            * - HttpContext.Session para recuperar informações do usuário logado.
        */

        [HttpPost]
        public async Task<IActionResult> AbrirChamado(IFormCollection form, List<IFormFile> inputAnexo)
        {
            var usuario = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            if (usuario == null)
                return RedirectToAction("Login", "Usuario");

            string setorSolicitado = form["setorSolicitado"];

            // Buscar o setor solicitado no banco
            int setorId = int.Parse(form["setorSolicitado"]);

            var setor = await _context.Setores.FirstOrDefaultAsync(s => s.Id == setorId);
            if (setor == null)
            {
                TempData["MensagemErro"] = "Setor solicitado inválido.";
                return RedirectToAction("AbrirChamado");
            }

            // Buscar o supervisor do setor solicitado
            var supervisor = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.ID_Setor == setor.Id && u.Cargo.Descricao == "Supervisor");

            if (supervisor == null)
            {
                TempData["MensagemErro"] = "Não é possível abrir chamado no momento. Nenhum supervisor foi encontrado para o setor solicitado.";
                return RedirectToAction("AbrirChamado");
            }

            // Criar o chamado com atendente já atribuído ao supervisor
            var chamado = new Chamado
            {
                Titulo = form["tituloChamado"],
                Descricao = form["descricao"],
                DataInicio = DateTime.Now,
                Status = "Aberto",
                ID_Solicitante = usuario.Id,
                ID_CriterioPrioridades = 1,
                ID_Atendente = supervisor.Id
            };

            _context.Chamados.Add(chamado);
            await _context.SaveChangesAsync(); // Gera o ID do chamado

            int chamadoId = chamado.Id;

            // Salvar os anexos (se houver)
            if (inputAnexo != null && inputAnexo.Count > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var file in inputAnexo)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                    var extension = Path.GetExtension(file.FileName);
                    var uniqueName = $"{fileName}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.Anexos.Add(new Anexo
                    {
                        NomeArquivo = file.FileName,
                        CaminhoArquivo = "/uploads/" + uniqueName,
                        Formato = extension,
                        ID_Chamado = chamadoId,
                        ID_Usuario = usuario.Id,
                        Data = DateTime.Now
                    });
                }
            }

            // Adicionar histórico
            _context.HistoricoChamado.Add(new HistoricoChamado
            {
                Data = DateTime.Now,
                AcaoTomada = "Chamado aberto pelo usuário.",
                ID_Usuario = usuario.Id,
                ID_Chamado = chamadoId
            });

            await _context.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Chamado aberto com sucesso!";
            return RedirectToAction("PainelSolicitante");
        }
    }
}
