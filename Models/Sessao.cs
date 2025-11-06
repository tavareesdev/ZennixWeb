/**
    * Sessao:
    * - Representa a sessão de um usuário no sistema, controlando login e logout.
    *
    * Propriedades:
    * - Id: Identificador único da sessão (PK).
    * - UsuarioId: Identificador do usuário associado à sessão (FK).
    * - DataInicio: Data e hora em que a sessão foi iniciada.
    * - DataFim: Data e hora em que a sessão foi encerrada; pode ser nulo enquanto a sessão estiver ativa.
    * - Ativa: Indica se a sessão está atualmente ativa (true) ou encerrada (false).
    * - Usuario: Referência ao objeto Usuario associado (opcional).
    *
    * Observações:
    * - Permite controlar múltiplas sessões por usuário e inativar sessões antigas ao realizar novo login.
*/

namespace PIM.Models
{
    public class Sessao
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public DateTime DataInicio { get; set; } = DateTime.Now;
        public DateTime? DataFim { get; set; } // agora pode ser nulo
        public bool Ativa { get; set; } = true;

        public Usuario? Usuario { get; set; }
    }
}
