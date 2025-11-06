using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZennixApi.Models
{
    public class HistoricoChamado
    {
        public int Id { get; set; }
        
        public int ID_Chamado { get; set; }
        [ForeignKey(nameof(ID_Chamado))]
        public Chamado? Chamado { get; set; }

        public int ID_Usuario { get; set; }
        [ForeignKey(nameof(ID_Usuario))]
        public Usuario? Usuario { get; set; }

        public string? AcaoTomada { get; set; }
        public DateTime Data { get; set; }
    }
}
