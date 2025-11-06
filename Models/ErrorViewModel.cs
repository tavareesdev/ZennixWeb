/**
    * ErrorViewModel:
    * - Modelo utilizado para exibir informações de erro em views do ASP.NET MVC.
    * - Propriedades:
    *   - RequestId: Armazena o identificador da requisição atual, útil para rastreamento de erros.
    *   - ShowRequestId: Indica se o RequestId está disponível para exibição na view.
    *
    * Observações:
    * - Normalmente usado em conjunto com a view "Error.cshtml" para apresentar detalhes do erro ao usuário ou para logs.
*/

namespace PIM.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
