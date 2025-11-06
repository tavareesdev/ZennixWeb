/**
    * Setor:
    * - Representa um setor ou departamento dentro da empresa.
    *
    * Propriedades:
    * - Id: Identificador único do setor (PK).
    * - Descricao: Nome ou descrição do setor.
    *
    * Observações:
    * - Pode ser utilizado para categorizar usuários, chamados ou outras entidades do sistema.
*/

namespace PIM.Models
{
    public class Setor
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
    }
}
