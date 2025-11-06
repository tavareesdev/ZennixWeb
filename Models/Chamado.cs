/**
    * Chamado:
    * - Representa um chamado ou ticket no sistema de atendimento.
    * - Propriedades:
    *   - Id: Identificador único do chamado.
    *   - Titulo: Título resumido do chamado.
    *   - DataInicio: Data e hora de abertura do chamado.
    *   - DataFim: Data e hora de fechamento do chamado (pode ser nula).
    *   - Status: Situação atual do chamado (ex: Aberto, Concluído, Pendente).
    *   - Descricao: Descrição detalhada do chamado.
    *   - ID_Solicitante: Identificador do usuário que abriu o chamado.
    *   - ID_Atendente: Identificador do usuário responsável pelo atendimento (pode ser nulo).
    *   - ID_CriterioPrioridades: Referência ao critério de prioridade aplicado (opcional).
    *   - PrioridadeId: Referência à prioridade do chamado (opcional).
*/

namespace PIM.Models
{
    public class Chamado
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string Status { get; set; }
        public string Descricao { get; set; }
        public int ID_Solicitante { get; set; }
        public int? ID_Atendente { get; set; }
        public int? ID_CriterioPrioridades { get; set; }
        public int? PrioridadeId { get; set; }

    }

}
