/**
    * Comentario:
    * - Representa um comentário feito em um chamado por um usuário.
    * - Propriedades:
    *   - Id: Identificador único do comentário.
    *   - Texto: Conteúdo do comentário (obrigatório).
    *   - Data: Data e hora em que o comentário foi registrado.
    *   - ID_Chamados: Chave estrangeira para o chamado ao qual o comentário pertence.
    *   - Chamado: Referência à entidade Chamado associada.
    *   - ID_Usuarios: Chave estrangeira para o usuário que fez o comentário.
    *   - Usuario: Referência à entidade Usuario que realizou o comentário.
    *
    * Observações:
    * - As FKs são definidas manualmente com [ForeignKey] e [Column] para mapeamento correto no banco.
    * - O campo Texto é obrigatório e mapeado explicitamente para a coluna "Comentario".
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIM.Models
{
    public class Comentario
    {
        public int Id { get; set; }

        [Column("Comentario")]
        [Required]
        public string Texto { get; set; } = string.Empty;

        public DateTime Data { get; set; }

        [ForeignKey(nameof(Chamado))] // Define a chave estrangeira manualmente
        [Column("ID_Chamados")]
        public int ID_Chamados { get; set; }

        public Chamado Chamado { get; set; }

        [ForeignKey(nameof(Usuario))] // Define a chave estrangeira manualmente
        [Column("ID_Usuarios")]
        public int ID_Usuarios { get; set; }

        public Usuario Usuario { get; set; }
    }
}
