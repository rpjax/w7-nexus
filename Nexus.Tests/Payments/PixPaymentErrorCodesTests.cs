using Nexus.Payments.Errors;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class PixPaymentErrorCodesTests
{
    [Fact]
    public void Codes_AreStableNonEmptyStrings()
    {
        var codes = typeof(PixPaymentErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string));

        Assert.NotEmpty(codes);
        foreach (var field in codes)
        {
            var value = (string)field.GetValue(null)!;
            Assert.False(string.IsNullOrWhiteSpace(value));
            Assert.True(
                value.StartsWith("PixPayment.", StringComparison.Ordinal) ||
                value.StartsWith("Payment.", StringComparison.Ordinal),
                $"Unexpected error code prefix: {value}");
        }
    }
}
