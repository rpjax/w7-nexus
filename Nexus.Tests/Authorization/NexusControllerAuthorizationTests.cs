using Aidan.Core.Errors;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.Controllers;
using Xunit;

namespace Nexus.Tests.Authorization;

public sealed class NexusControllerAuthorizationTests
{
    private readonly TestNexusController _sut = new();

    [Fact]
    public void ToOperationResult_Unauthorized_Returns403()
    {
        var result = OperationResult<string>.Unauthorized(Error.Create()
            .WithCode(AuthorizationErrorCodes.NotOperator)
            .WithMessage("denied")
            .Build());

        var actionResult = _sut.Map(result);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public void ToOperationResult_Failure_Returns422()
    {
        var result = OperationResult<string>.Failure(Error.Create()
            .WithCode("Validation.ERROR")
            .WithMessage("invalid")
            .Build());

        var actionResult = _sut.Map(result);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(422, objectResult.StatusCode);
    }

    [Fact]
    public void ToOperationResult_Success_Returns200WithValue()
    {
        var result = OperationResult<string>.Success("ok");

        var actionResult = _sut.Map(result);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("ok", okResult.Value);
    }

    [Fact]
    public void ToOperationResult_SuccessWithNullValue_ThrowsInvalidOperationException()
    {
        var result = new OperationResult<string>
        {
            IsSuccess = true,
            IsAuthorized = true,
            Value = null
        };

        Assert.Throws<InvalidOperationException>(() => _sut.Map(result));
    }

    private sealed class TestNexusController : NexusController
    {
        public ActionResult Map<T>(IOperationResult<T> result)
            => ToOperationResult(result);
    }
}
