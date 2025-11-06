/**
    * ChamadoComUsuario:
    * - Representa uma visão de chamado com informações adicionais do usuário e setor.
    * - Usado principalmente em consultas que fazem JOIN entre chamados, usuários e setores.
    * - Propriedades:
    *   - Id: Identificador único do chamado.
    *   - Titulo: Título resumido do chamado.
    *   - DataInicio: Data e hora de abertura do chamado.
    *   - DataFim: Data e hora de fechamento do chamado (pode ser nula).
    *   - Responsavel: Nome do atendente responsável pelo chamado.
    *   - Setor: Nome do setor do atendente (opcional, pode ser nulo).
    *   - Status: Situação atual do chamado (ex: Aberto, Concluído, Pendente).
    *   - SetorSoli: Nome do setor do solicitante (opcional).
    *   - Solicitante: Nome do usuário que abriu o chamado (opcional).
    *   - Situacao: Situação detalhada ou observações adicionais (opcional).
    *   - ID_SetorAtendente: Identificador do setor do atendente (opcional).
    * 
    * Observação:
    * - Essa entidade não possui chave primária definida no banco, sendo usada apenas como resultado de consultas.
*/

namespace PIM.Models
{
    public class ChamadoComUsuario
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public string Responsavel { get; set; } = string.Empty;
        public string? Setor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Colunas específicas do "Chamados do Time" que nem sempre existem na outra query
        public string? SetorSoli { get; set; }
        public string? Solicitante { get; set; }
        public string? Situacao { get; set; }
        public int? ID_SetorAtendente { get; set; }
    }
}
