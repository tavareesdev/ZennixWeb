/**
    * FuncionarioViewModel:
    * - Representa informações básicas de um funcionário para uso em views ou dropdowns.
    * - Propriedades:
    *   - Id: Identificador único do funcionário.
    *   - Nome: Nome completo do funcionário.
*/

namespace PIM.Models.ViewModels
{
    public class FuncionarioViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
    }
}
