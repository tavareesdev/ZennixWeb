/**
    * Prioridades:
    * - Representa os diferentes níveis de prioridade que podem ser atribuídos a chamados ou tarefas no sistema.
    *
    * Propriedades:
    * - Id: Identificador único da prioridade (PK).
    * - Nome: Nome ou descrição da prioridade (ex.: "Alta", "Média", "Baixa").
    *
    * Observações:
    * - Usado para categorizar e ordenar chamados com base na urgência ou importância.
*/

namespace PIM.Models
{
    public class Prioridades
    {
        public int Id { get; set; }
        public string Nome { get; set; }
    }

}