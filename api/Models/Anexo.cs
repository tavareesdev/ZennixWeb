using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZennixApi.Models
{
    public class Anexo
    {
        public int ID { get; set; }

        public string? NomeArquivo { get; set; }
        public string? CaminhoArquivo { get; set; }
        public string? Formato { get; set; }

        public int ID_Chamado { get; set; }
        public int ID_Usuario { get; set; }

        public DateTime Data { get; set; }

        // Corrigido: define manualmente as FKs
        [ForeignKey("ID_Chamado")]
        public Chamado? Chamado { get; set; }

        [ForeignKey("ID_Usuario")]
        public Usuario? Usuario { get; set; }
    }
}
