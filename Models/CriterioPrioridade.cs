/**
    * CriterioPrioridade:
    * - Representa um critério utilizado para definir a prioridade de um chamado.
    * - Propriedades:
    *   - Id: Identificador único do critério.
    *   - Descricao: Descrição do critério.
    *   - Nivel: Nível do critério, que pode indicar a importância ou urgência.
    *
    * Observações:
    * - Usado para categorizar chamados com base em critérios específicos de prioridade.
*/

namespace PIM.Models
{
    public class CriterioPrioridade
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
        public int Nivel { get; set; }
    }
}