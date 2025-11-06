/**
    * Cargo:
    * - Representa o cargo ou função de um usuário na empresa.
    * - Propriedades:
    *   - Id: Identificador único do cargo.
    *   - Descricao: Nome ou descrição do cargo (ex: Diretor, Coordenador, Analista).
*/

namespace PIM.Models
{
    public class Cargo
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
    }
}
