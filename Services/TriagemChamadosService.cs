/**
 * Serviço em segundo plano responsável pela redistribuição automática de chamados em aberto
 * entre os atendentes disponíveis de cada setor.
 *
 * Retorno: Task (quando métodos são assíncronos) ou void (para operações síncronas).
 * O serviço não retorna valores de negócio, apenas executa a rotina de redistribuição.
 *
 * Funcionamento:
 * - Ao iniciar, o serviço agenda uma tarefa recorrente a cada 30 minutos para executar a triagem.
 * - No método DoWork, ele cria um escopo de serviço para acessar o banco de dados via ApplicationDbContext.
 * - Recupera todos os chamados em aberto e filtra usuários aptos a receber chamados,
 *   excluindo os de cargos não permitidos e um usuário específico (ID 2006).
 * - Agrupa os atendentes por setor e redistribui os chamados proporcionalmente entre eles,
 *   garantindo um balanceamento.
 * - Caso o responsável por um chamado seja alterado, registra um histórico informando a troca.
 * - Após a redistribuição, salva as alterações no banco e registra logs de sucesso ou erro.
 *
 * Parâmetros / Operações:
 * - IServiceScopeFactory: cria escopos para resolver dependências, garantindo ciclo de vida adequado.
 * - ILogger<ChamadoTriagemService>: utilizado para registrar logs de informações e erros.
 * - Timer: agenda a execução da rotina a cada 30 minutos.
 * - cargosNaoPermitidos: lista de IDs de cargos que não devem receber chamados.
 * - Usuário com ID 2006 é tratado como o sistema/bot responsável por registrar históricos de alterações.
 *
 * Bibliotecas utilizadas e dependências externas:
 * - Microsoft.Extensions.Hosting (para execução em segundo plano com IHostedService).
 * - Microsoft.Extensions.Logging (para registro de logs).
 * - Microsoft.EntityFrameworkCore (para interação com o banco de dados).
 * - PIM.Models (para uso das entidades ApplicationDbContext, Chamados, Usuarios e HistoricoChamado).
*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PIM.Models;

public class ChamadoTriagemService : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChamadoTriagemService> _logger;

    public ChamadoTriagemService(IServiceScopeFactory scopeFactory, ILogger<ChamadoTriagemService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // roda a cada 30 minutos
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // 🔹 Pega todos os chamados em aberto
            var chamadosAbertos = context.Chamados
                .Where(c => c.Status == "Aberto")
                .ToList();

            // 🔹 Cargos que não devem receber chamados (ex: supervisores)
            var cargosNaoPermitidos = new List<int?> { 8, 9, 10 };

            // 🔹 Pega apenas os usuários que podem receber chamados (excluindo ID 2006)
            var usuariosPermitidos = context.Usuarios
                .Where(u => !cargosNaoPermitidos.Contains(u.ID_Cargo) && u.Id != 2006)
                .ToList();

            // 🔹 Agrupa atendentes permitidos por setor
            var atendentesPorSetor = usuariosPermitidos
                .Where(u => u.ID_Setor.HasValue)
                .GroupBy(u => u.ID_Setor.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var grupo in atendentesPorSetor)
            {
                var idSetor = grupo.Key;
                var atendentes = grupo.Value;

                if (!atendentes.Any())
                    continue;

                // 🔹 Filtra chamados abertos do setor (qualquer chamado cujo atendente pertença ao setor)
                var chamadosDoSetor = chamadosAbertos
                    .Where(c => c.ID_Atendente == null || context.Usuarios.Any(u => u.Id == c.ID_Atendente && u.ID_Setor == idSetor))
                    .ToList();

                if (!chamadosDoSetor.Any())
                    continue;

                int totalAtendentes = atendentes.Count;
                int index = 0;

                foreach (var chamado in chamadosDoSetor)
                {
                    var antigoAtendenteId = chamado.ID_Atendente;
                    var novoAtendente = atendentes[index % totalAtendentes];

                    // Só registra alteração se realmente mudou
                    if (antigoAtendenteId != novoAtendente.Id)
                    {
                        string nomeAntigo = antigoAtendenteId.HasValue
                            ? context.Usuarios.FirstOrDefault(u => u.Id == antigoAtendenteId.Value)?.Nome ?? "Não definido"
                            : "Não definido";

                        string nomeNovo = novoAtendente.Nome;

                        // Atualiza atendente do chamado
                        chamado.ID_Atendente = novoAtendente.Id;

                        // Adiciona histórico
                        var historico = new HistoricoChamado
                        {
                            ID_Chamado = chamado.Id,
                            ID_Usuario = 2006, // usuário "sistema/bot"
                            Data = DateTime.Now,
                            AcaoTomada = $"Alterou o campo Responsavel de \"{nomeAntigo}\" para \"{nomeNovo}\""
                        };
                        context.HistoricoChamado.Add(historico);
                    }

                    index++;
                }

                _logger.LogInformation(
                    $"[{DateTime.Now}] Redistribuídos {chamadosDoSetor.Count} chamados no setor {idSetor} entre {totalAtendentes} atendentes."
                );
            }

            context.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar triagem automática.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
