using Nexus.Olx.Aggregates;
using Xunit;

namespace Nexus.Tests.Olx;

public sealed class AdSpoofTests
{
  private const string ValidAdUrl = "https://www.olx.com.br/anuncio/iphone-1513407983";

  [Fact]
  public void Create_ValidIds_Succeeds()
  {
    var result = AdSpoof.Create("op-1", "ad-1", ValidAdUrl);

    Assert.True(result.IsSuccess);
    Assert.Equal("op-1", result.Value!.OperationId);
    Assert.Equal("ad-1", result.Value.AdId);
    Assert.Equal(ValidAdUrl, result.Value.AdUrl);
    Assert.False(result.Value.IsImpersonating);
  }

  [Fact]
  public void Create_WithoutAdUrl_Fails()
  {
    var result = AdSpoof.Create("op-1", "ad-1", "");

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void Create_WithInvalidAdUrl_Fails()
  {
    var result = AdSpoof.Create("op-1", "ad-1", "not-a-url");

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void TryNormalizeAdUrl_AcceptsHttpAndHttps()
  {
    Assert.True(AdSpoof.TryNormalizeAdUrl("https://olx.com.br/item-123", out var https).IsSuccess);
    Assert.Equal("https://olx.com.br/item-123", https);

    Assert.True(AdSpoof.TryNormalizeAdUrl("http://olx.com.br/item-123", out var http).IsSuccess);
    Assert.Equal("http://olx.com.br/item-123", http);
  }

  [Fact]
  public void TryNormalizeAdUrl_RejectsNonHttpScheme()
  {
    var result = AdSpoof.TryNormalizeAdUrl("ftp://olx.com.br/item-123", out _);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void Impersonate_SetsOperatorAndFlag()
  {
    var spoof = AdSpoof.Create("op-1", "ad-1", ValidAdUrl).Value!;

    var result = spoof.Impersonate("operator-1");

    Assert.True(result.IsSuccess);
    Assert.True(spoof.IsImpersonating);
    Assert.Equal("operator-1", spoof.OperatorId);
  }

  [Fact]
  public void Unimpersonate_ClearsOperatorAndFlag()
  {
    var spoof = AdSpoof.Create("op-1", "ad-1", ValidAdUrl).Value!;
    spoof.Impersonate("operator-1");

    var result = spoof.Unimpersonate();

    Assert.True(result.IsSuccess);
    Assert.False(spoof.IsImpersonating);
    Assert.Null(spoof.OperatorId);
  }

  [Fact]
  public void Impersonate_WhenAlreadySpoofedByAnotherOperator_Fails()
  {
    var spoof = AdSpoof.Create("op-1", "ad-1", ValidAdUrl).Value!;
    spoof.Impersonate("operator-1");

    var result = spoof.Impersonate("operator-2");

    Assert.True(result.IsFailure);
    Assert.Equal("operator-1", spoof.OperatorId);
    Assert.True(spoof.IsImpersonating);
  }

  [Fact]
  public void UpdatePriceSpoof_WhenAlreadySpoofedByAnotherOperator_Fails()
  {
    var spoof = AdSpoof.Create("op-1", "ad-1", ValidAdUrl).Value!;
    spoof.Impersonate("operator-1");

    var result = spoof.UpdatePriceSpoof("operator-2", 100m, 80m);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void UpdatePriceSpoof_RequiresAtLeastOnePrice()
  {
    var spoof = AdSpoof.Create("op-1", "ad-1", ValidAdUrl).Value!;

    var result = spoof.UpdatePriceSpoof("operator-1", null, null);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void UpdatePriceSpoof_RequiresActiveImpersonation()
  {
    var spoof = AdSpoof.Create("op-1", "ad-1", ValidAdUrl).Value!;

    var result = spoof.UpdatePriceSpoof("operator-1", 100m, 80m);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void UpdatePriceSpoof_PersistsPrices_WhenImpersonating()
  {
    var spoof = AdSpoof.Create("op-1", "ad-1", ValidAdUrl).Value!;
    spoof.Impersonate("operator-1");

    var result = spoof.UpdatePriceSpoof("operator-1", 100m, 80m);

    Assert.True(result.IsSuccess);
    Assert.Equal(100m, spoof.OriginalPrice);
    Assert.Equal(80m, spoof.PromotionalPrice);
  }
}
