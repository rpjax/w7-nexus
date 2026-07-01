using Aidan.Core.Patterns;
using Nexus.Charges.Application;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Models;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Requests.Victim;
using Nexus.Olx.Application.Responses;
using Nexus.Olx.Application.Services;
using Nexus.Olx.Errors;
using Xunit;

namespace Nexus.Tests.Olx;

public sealed class VictimTests
{
    private const string ValidAdUrl = "https://www.olx.com.br/anuncio/iphone-1513407983";

    [Fact]
    public async Task CreatePixPaymentAsync_WhenOperationIdMissing_ReturnsFailure()
    {
        var sut = CreateSut(new StubChargeService());

        var result = await sut.CreatePixPaymentAsync(new CreatePixPaymentRequest
        {
            OperationId = "",
            Value = 150m,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AdPatchErrorCodes.OperationIdInvalid);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_WhenSuccessful_ReturnsJsCompatibleResponse()
    {
        var chargeService = new StubChargeService
        {
            ChargeResult = Result<CreatePixChargeResponse>.Success(new CreatePixChargeResponse
            {
                Id = "gw-1",
                PixCode = "00020126580014BR.GOV.BCB.PIX",
                PaymentRecipient = ChargeDefaults.PaymentRecipient,
                ExpirationTimeSeconds = ChargeDefaults.ExpirationTimeSeconds,
            }),
        };

        var sut = CreateSut(chargeService);

        var result = await sut.CreatePixPaymentAsync(new CreatePixPaymentRequest
        {
            OperationId = "op-1",
            Value = 150m,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("00020126580014BR.GOV.BCB.PIX", result.Value!.PixCode);
        Assert.Equal(150m, result.Value.Value);
        Assert.Equal(ChargeDefaults.ExpirationTimeSeconds, result.Value.ExpirationTimeSeconds);
        Assert.Equal(ChargeDefaults.PaymentRecipient, result.Value.PaymentRecipient);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_WhenAdIdProvided_InfersOperatorIdFromAdPatch()
    {
        var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;
        patch.Impersonate("operator-1");

        var chargeService = new StubChargeService
        {
            ChargeResult = Result<CreatePixChargeResponse>.Success(new CreatePixChargeResponse
            {
                PixCode = "pix-code",
                PaymentRecipient = ChargeDefaults.PaymentRecipient,
                ExpirationTimeSeconds = ChargeDefaults.ExpirationTimeSeconds,
            }),
        };

        var sut = CreateSut(chargeService, patch: patch);

        var result = await sut.CreatePixPaymentAsync(new CreatePixPaymentRequest
        {
            OperationId = "op-1",
            AdId = "ad-1",
            Value = 99.90m,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("operator-1", chargeService.LastRequest?.OperatorId);
        Assert.Equal(99.90m, chargeService.LastRequest?.Amount);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_WhenAdPatchMissing_ReturnsFailure()
    {
        var sut = CreateSut(new StubChargeService(), patch: null);

        var result = await sut.CreatePixPaymentAsync(new CreatePixPaymentRequest
        {
            OperationId = "op-1",
            AdId = "missing-ad",
            Value = 10m,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AdPatchErrorCodes.AdPatchNotFound);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_WhenAdPatchHasNoOperator_ReturnsFailure()
    {
        var patch = AdPatch.Create("op-1", "ad-1", ValidAdUrl).Value!;
        var sut = CreateSut(new StubChargeService(), patch: patch);

        var result = await sut.CreatePixPaymentAsync(new CreatePixPaymentRequest
        {
            OperationId = "op-1",
            AdId = "ad-1",
            Value = 10m,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AdPatchErrorCodes.OperatorNotFound);
    }

    private static Victim CreateSut(
        StubChargeService chargeService,
        AdPatch? patch = null) =>
        new(new StubAdPatchQueryService(patch), chargeService);

    private sealed class StubAdPatchQueryService : IAdPatchQueryService
    {
        private readonly AdPatch? _patch;

        public StubAdPatchQueryService(AdPatch? patch) =>
            _patch = patch;

        public Task<IResult<ListPatchedAdsResponse>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            IResult<ListPatchedAdsResponse> result = Result<ListPatchedAdsResponse>.Success(new ListPatchedAdsResponse());
            return Task.FromResult(result);
        }

        public Task<AdPatch?> FindByOperationAndAdAsync(
            string operationId,
            string adId,
            CancellationToken cancellationToken = default)
        {
            if (_patch is null)
                return Task.FromResult<AdPatch?>(null);

            return Task.FromResult(
                string.Equals(_patch.OperationId, operationId, StringComparison.Ordinal)
                && string.Equals(_patch.AdId, adId, StringComparison.Ordinal)
                    ? _patch
                    : null);
        }
    }

    private sealed class StubChargeService : IChargeService
    {
        public CreatePixChargeRequest? LastRequest { get; private set; }
        public IResult<CreatePixChargeResponse>? ChargeResult { get; init; }

        public Task<IResult<CreatePixChargeResponse>> CreatePixChargeAsync(CreatePixChargeRequest request)
        {
            LastRequest = request;
            return Task.FromResult(ChargeResult ?? Result<CreatePixChargeResponse>.Success(new CreatePixChargeResponse
            {
                PixCode = "default-pix-code",
                PaymentRecipient = ChargeDefaults.PaymentRecipient,
                ExpirationTimeSeconds = ChargeDefaults.ExpirationTimeSeconds,
            }));
        }
    }
}
