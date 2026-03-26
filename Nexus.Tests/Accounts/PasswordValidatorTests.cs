using Nexus.Accounts.ErrorCodes;
using Nexus.Accounts.Infrastructure;
using Xunit;

namespace Nexus.Tests.Accounts;

public sealed class PasswordValidatorTests
{
    private readonly PasswordValidator _sut = new();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateForCreationAsync_EmptyOrNull_ReturnsFailure(string? password)
    {
        var result = await _sut.ValidateForCreationAsync(password ?? "");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.PasswordTooShort);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1234567")]
    public async Task ValidateForCreationAsync_TooShort_ReturnsFailure(string password)
    {
        var result = await _sut.ValidateForCreationAsync(password);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.PasswordTooShort);
    }

    [Fact]
    public async Task ValidateForCreationAsync_ValidLength_ReturnsSuccess()
    {
        var result = await _sut.ValidateForCreationAsync("12345678");

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateForChangeAsync_EmptyOrNull_ReturnsFailure(string? password)
    {
        var result = await _sut.ValidateForChangeAsync(password ?? "");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.PasswordTooShort);
    }

    [Fact]
    public async Task ValidateForChangeAsync_ValidLength_ReturnsSuccess()
    {
        var result = await _sut.ValidateForChangeAsync("newpassword123");

        Assert.True(result.IsSuccess);
    }
}
