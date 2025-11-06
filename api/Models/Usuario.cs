using System.ComponentModel.DataAnnotations.Schema;

namespace ZennixApi.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Email { get; set; } = string.Empty;
        [NotMapped]
        public string Setor { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty; // senha em MD5
        public int ID_Setor { get; set; } // senha em MD5
        public int? ID_Cargo { get; set; }

        [ForeignKey("ID_Cargo")]
        public Cargo? Cargo { get; set; }
    }
}
