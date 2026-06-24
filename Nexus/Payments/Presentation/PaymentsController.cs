using Aidan.Core.Errors;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;

namespace Nexus.Payments.Presentation;

[Route("api/payments")]
public sealed class PaymentsController : WebController
{
    [HttpPost("search")]
    [Obsolete("Use POST /api/administrator/payments/search, /api/operator/payments/search or /api/straw-man/payments/search.")]
    public ActionResult SearchAsync([FromBody] SearchPaymentsRequest? request)
    {
        _ = request;
        return ProblemResponse(410, Error.Create()
            .WithCode("Payment.SEARCH_ENDPOINT_DEPRECATED")
            .WithMessage("Este endpoint foi descontinuado. Use os endpoints de pagamentos por perfil (administrador, operador ou laranja).")
            .Build());
    }

    [HttpPost("{paymentId}/withdraw")]
    [Obsolete("Use POST api/withdrawals to create a withdrawal linked to one or more payments.")]
    public ActionResult WithdrawAsync(string paymentId)
    {
        _ = paymentId;
        return ProblemResponse(410, Error.Create()
            .WithCode("Payment.WITHDRAW_ENDPOINT_DEPRECATED")
            .WithMessage("Este endpoint foi descontinuado. Crie um saque via POST api/withdrawals vinculando os pagamentos desejados.")
            .Build());
    }
}
