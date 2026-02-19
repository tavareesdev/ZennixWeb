using System.ComponentModel.DataAnnotations;

namespace PIM.Models.ViewModels
{
    public class UsuarioTesteViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; }

        public string Telefone { get; set; }
    }
}