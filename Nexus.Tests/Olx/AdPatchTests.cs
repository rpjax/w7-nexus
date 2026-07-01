using Nexus.Olx.Aggregates;
using Xunit;

namespace Nexus.Tests.Olx;

public sealed class AdPatchTests
{
  private const string ValidAdUrl = "https://www.olx.com.br/anuncio/iphone-1513407983";

  [Fact]
  public void Create_ValidIds_Succeeds()
  {
    var result = AdPatch.Create("op-1", "ad-1", ValidAdUrl);

    Assert.True(result.IsSuccess);
    Assert.Equal("op-1", result.Value!.OperationId);
    Assert.Equal("ad-1", result.Value.AdId);
    Assert.Equal(ValidAdUrl, result.Value.AdUrl);
    Assert.False(result.Value.IsImpersonating);
  }

  [Fact]
  public void Create_WithoutAdUrl_Fails()
  {
    var result = AdPatch.Create("op-1", "ad-1", "");

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void Create_WithInvalidAdUrl_Fails()
  {
    var result = AdPatch.Create("op-1", "ad-1", "http://");

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void TryNormalizeAdUrl_AcceptsHttpAndHttps()
  {
    Assert.True(AdPatch.TryNormalizeAdUrl("https://olx.com.br/item-123", out var https).IsSuccess);
    Assert.Equal("https://olx.com.br/item-123", https);

    Assert.True(AdPatch.TryNormalizeAdUrl("http://olx.com.br/item-123", out var http).IsSuccess);
    Assert.Equal("http://olx.com.br/item-123", http);
  }

  [Fact]
  public void TryNormalizeAdUrl_RejectsNonHttpScheme()
  {
    var result = AdPatch.TryNormalizeAdUrl("ftp://olx.com.br/item-123", out _);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void Impersonate_SetsOperatorAndFlag()
  {
    var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;

    var result = patch.Impersonate("operator-1");

    Assert.True(result.IsSuccess);
    Assert.True(patch.IsImpersonating);
    Assert.Equal("operator-1", patch.OperatorId);
  }

  [Fact]
  public void Unimpersonate_ClearsOperatorAndFlag()
  {
    var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;
    patch.Impersonate("operator-1");

    var result = patch.Unimpersonate();

    Assert.True(result.IsSuccess);
    Assert.False(patch.IsImpersonating);
    Assert.Null(patch.OperatorId);
  }

  [Fact]
  public void Impersonate_WhenAlreadyPatchedByAnotherOperator_Fails()
  {
    var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;
    patch.Impersonate("operator-1");

    var result = patch.Impersonate("operator-2");

    Assert.True(result.IsFailure);
    Assert.Equal("operator-1", patch.OperatorId);
    Assert.True(patch.IsImpersonating);
  }

  [Fact]
  public void UpdatePricePatch_WhenAlreadyPatchedByAnotherOperator_Fails()
  {
    var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;
    patch.Impersonate("operator-1");

    var result = patch.UpdatePricePatch("operator-2", 100m, 80m);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void UpdatePricePatch_RequiresAtLeastOnePrice()
  {
    var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;

    var result = patch.UpdatePricePatch("operator-1", null, null);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void UpdatePricePatch_RequiresActiveImpersonation()
  {
    var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;

    var result = patch.UpdatePricePatch("operator-1", 100m, 80m);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void UpdatePricePatch_PersistsPrices_WhenImpersonating()
  {
    var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;
    patch.Impersonate("operator-1");

    var result = patch.UpdatePricePatch("operator-1", 100m, 80m);

    Assert.True(result.IsSuccess);
    Assert.Equal(100m, patch.OriginalPrice);
    Assert.Equal(80m, patch.PromotionalPrice);
  }
}
