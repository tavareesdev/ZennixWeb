namespace ZennixApi.Models
{
    public class UsuarioLoginResult
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public int ID_Setor { get; set; }
        public int Status { get; set; }
    }
}
