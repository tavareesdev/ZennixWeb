/**
    * RankingViewModel:
    * - Representa os dados de ranking de atendimento para exibição em dashboards ou relatórios.
    * - Propriedades:
    *   - Nome: Nome do funcionário/atendente.
    *   - Setor: Nome do setor ao qual o atendente pertence.
    *   - Concluidos: Quantidade de chamados concluídos pelo atendente.
*/

namespace PIM.Models.ViewModels
{
    public class RankingViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Setor { get; set; } = string.Empty;
        public int Concluidos { get; set; }
        public int Id { get; set; }
    }
}
