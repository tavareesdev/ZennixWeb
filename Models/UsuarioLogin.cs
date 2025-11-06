/**
    * UsuarioLogin:
    * - Representa os dados de login de um usuário.
    *
    * Propriedades:
    * - Email: Endereço de email utilizado para autenticação.
    * - Senha: Senha do usuário, enviada para validação (normalmente é convertida em hash antes de comparar com o banco).
    *
    * Observações:
    * - Ambos os campos são obrigatórios para o processo de login.
    * - Essa classe é usada principalmente em formulários de login ou APIs de autenticação.
*/

namespace PIM.Models
{
    public class UsuarioLogin
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
