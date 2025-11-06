/**
    * AlterarSenhaViewModel
    *
    * ViewModel utilizado para alterar a senha de um usuário.
    * Contém apenas os dados necessários para a operação de atualização de senha.
    *
    * Propriedades:
    * - Id (int): Identificador do usuário cuja senha será alterada.
    * - NovaSenha (string): Nova senha que será atribuída ao usuário.
*/

namespace PIM.Models.ViewModels
{
    public class AlterarSenhaViewModel
    {
        public int Id { get; set; }
        public string NovaSenha { get; set; }
    }
}
