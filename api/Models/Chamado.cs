namespace ZennixApi.Models
{
    public class Chamado
    {
        public int Id { get; set; }
        public string? Titulo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? Status { get; set; }
        public string? Descricao { get; set; }
        public int ID_Solicitante { get; set; }
        public int? ID_Atendente { get; set; }
        public int? ID_CriterioPrioridades { get; set; }
        public int? PrioridadeId { get; set; }

    }

}