/**
    * Usuario:
    * - Representa um usuário do sistema, contendo informações pessoais, de login e vinculações a setores e cargos.
    *
    * Propriedades:
    * - Id: Identificador único do usuário (PK).
    * - Nome: Nome completo do usuário.
    * - Email: Endereço de email usado para login.
    * - Senha: Senha do usuário, geralmente armazenada como hash.
    * - ID_Setor: Referência ao setor ao qual o usuário pertence (FK opcional).
    * - Setor: Propriedade de navegação para o setor vinculado.
    * - ID_Cargo: Referência ao cargo do usuário (FK opcional).
    * - Cargo: Propriedade de navegação para o cargo vinculado.
    * - Status: Indica se o usuário está ativo (1) ou inativo (0).
    * - DataNasc: Data de nascimento do usuário (opcional).
    * - DataAdm: Data de admissão do usuário (opcional).
    * - DataDemi: Data de demissão do usuário (opcional).
    * - Telefone: Número de telefone do usuário.
    *
    * Observações:
    * - As propriedades de navegação Setor e Cargo permitem acessar dados relacionados sem precisar de join explícito.
    * - Campos de data são opcionais para permitir usuários sem informações completas.
*/

using System.ComponentModel.DataAnnotations.Schema;

namespace PIM.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nome { get; set; }

        public string Email { get; set; }

        public string Senha { get; set; }

        public int? ID_Setor { get; set; }

        [ForeignKey("ID_Setor")]
        public Setor Setor { get; set; }

        public int? ID_Cargo { get; set; }

        [ForeignKey("ID_Cargo")]
        public Cargo Cargo { get; set; }  // Propriedade de navegação para Cargo

        public int Status { get; set; }

        public DateTime? DataNasc { get; set; }

        public DateTime? DataAdm { get; set; }
        
        public DateTime? DataDemi { get; set; }

        public string Telefone { get; set; }

        public int TipoUsuario { get; set; } = 0; // 0 = Normal, 1 = Teste

    }
}
