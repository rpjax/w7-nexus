using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Requests;
using Nexus.Charges.Application.Responses;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Errors;

namespace Nexus.Charges.Application.Services;

public sealed class GatewayCredentialsResolver : IGatewayCredentialsResolver
{
    private IOperationRepository _operationRepository { get; }
    private ITeamRepository _teamRepository { get; }
    private IFrendzApiCredentialsRepository _frendzCredentialsRepository { get; }
    private ISigiloPayApiCredentialsRepository _sigiloPayCredentialsRepository { get; }
    private IWintechApiCredentialsRepository _wintechCredentialsRepository { get; }
    private IGatewayCredentialsGroupRepository _gatewayCredentialsGroupRepository { get; }

    public GatewayCredentialsResolver(
        IOperationRepository operationRepository,
        ITeamRepository teamRepository,
        IFrendzApiCredentialsRepository frendzCredentialsRepository,
        ISigiloPayApiCredentialsRepository sigiloPayCredentialsRepository,
        IWintechApiCredentialsRepository wintechCredentialsRepository,
        IGatewayCredentialsGroupRepository gatewayCredentialsGroupRepository)
    {
        _operationRepository = operationRepository;
        _teamRepository = teamRepository;
        _frendzCredentialsRepository = frendzCredentialsRepository;
        _sigiloPayCredentialsRepository = sigiloPayCredentialsRepository;
        _wintechCredentialsRepository = wintechCredentialsRepository;
        _gatewayCredentialsGroupRepository = gatewayCredentialsGroupRepository;
    }

    public async Task<IResult<ResolveCredentialsResponse>> ResolveCredentialsAsync(ResolveCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return Result.Create<ResolveCredentialsResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                    .WithMessage("O ID da operação é obrigatório.")
                    .Build())
                .Build();
        }

        var operatorId = request.OperatorId?.Trim();
        if (operatorId is not null && string.IsNullOrWhiteSpace(operatorId))
        {
            return Result.Create<ResolveCredentialsResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorInvalid)
                    .WithMessage("O ID do operador não pode estar vazio quando informado.")
                    .Build())
                .Build();
        }

        var operation = await _operationRepository.AsQueryable()
            .Where(x => x.Id == operationId)
            .FirstOrDefaultAsync();

        if (operation is null)
        {
            return Result.Create<ResolveCredentialsResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperationNotFound)
                    .WithMessage($"A operação '{operationId}' não foi encontrada.")
                    .Build())
                .Build();
        }

        Team? team = null;
        if (!string.IsNullOrWhiteSpace(operatorId))
        {
            var teams = await _teamRepository.AsQueryable()
                .Where(t =>
                    t.OperationId == operationId &&
                    t.OperatorIds.Contains(operatorId))
                .ToArrayAsync();

            if (teams.Length == 0)
            {
                return Result.Create<ResolveCredentialsResponse>()
                    .WithError(Error.Create()
                        .WithCode(PixPaymentErrorCodes.TeamNotFound)
                        .WithMessage($"Não há equipe na operação '{operationId}' com o operador informado.")
                        .Build())
                    .Build();
            }

            if (teams.Length > 1)
            {
                return Result.Create<ResolveCredentialsResponse>()
                    .WithError(Error.Create()
                        .WithCode(PixPaymentErrorCodes.TeamAmbiguous)
                        .WithMessage("Há mais de uma equipe compatível com o operador informado.")
                        .Build())
                    .Build();
            }

            team = teams[0];
        }

        IGatewayCredentialScope scope = (IGatewayCredentialScope?)team ?? operation;
        var resolvedCredentials = await ResolveScopedCredentialsAsync(scope);

        if (resolvedCredentials.Length == 0)
        {
            return Result.Create<ResolveCredentialsResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.NoGatewayServicesAvailable)
                    .WithMessage(team is null
                        ? "Não há credenciais de gateway disponíveis para esta operação."
                        : "Não há credenciais de gateway disponíveis para esta equipe.")
                    .Build())
                .Build();
        }

        return Result.Create<ResolveCredentialsResponse>()
            .WithValue(new ResolveCredentialsResponse
            {
                Credentials = resolvedCredentials
                    .Select(c => new GatewayCredentialReference
                    {
                        Gateway = c.Gateway,
                        CredentialId = c.CredentialId,
                    })
                    .ToArray(),
                StrawManIdByCredentialId = resolvedCredentials.ToDictionary(
                    c => c.CredentialId,
                    c => c.StrawManId,
                    StringComparer.Ordinal),
            })
            .Build();
    }

    private sealed record ResolvedChargeCredential(
        PaymentGateway Gateway,
        string CredentialId,
        string StrawManId);

    private async Task<ResolvedChargeCredential[]> ResolveScopedCredentialsAsync(IGatewayCredentialScope scope)
    {
        var allowedCredentialIds = await ResolveAllowedCredentialIdsAsync(scope);
        var frendz = await GetFrendzCredentialsAsync(scope, allowedCredentialIds);
        var sigiloPay = await GetSigiloPayCredentialsAsync(scope, allowedCredentialIds);
        var wintech = await GetWintechCredentialsAsync(scope, allowedCredentialIds);

        return frendz
            .Concat(sigiloPay)
            .Concat(wintech)
            .Where(c => !string.IsNullOrWhiteSpace(c.StrawManId))
            .ToArray();
    }

    private async Task<string[]> ResolveAllowedCredentialIdsAsync(IGatewayCredentialScope scope)
    {
        return scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual => scope.GatewayCredentialsIds.ToArray(),
            GatewaySelectionStrategy.PerGroup => await ResolveGroupCredentialIdsAsync(scope),
            _ => Array.Empty<string>(),
        };
    }

    private async Task<string[]> ResolveGroupCredentialIdsAsync(IGatewayCredentialScope scope)
    {
        var groupIds = scope.GatewayCredentialsGroupIds.ToArray();
        if (groupIds.Length == 0)
            return Array.Empty<string>();

        var groups = await _gatewayCredentialsGroupRepository.AsQueryable()
            .Where(g => groupIds.Contains(g.Id))
            .ToArrayAsync();

        return groups
            .SelectMany(g => g.GatewayCredentialsIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<ResolvedChargeCredential[]> GetFrendzCredentialsAsync(
        IGatewayCredentialScope scope,
        string[] allowedCredentialIds)
    {
        if (scope.GatewaySelectionStrategy is GatewaySelectionStrategy.PerStrawman &&
            scope.StrawManIds.Count == 0)
        {
            return Array.Empty<ResolvedChargeCredential>();
        }

        var strawmanIds = scope.StrawManIds.ToArray();
        var query = _frendzCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup =>
                query.Where(x => allowedCredentialIds.Contains(x.Id)),
            GatewaySelectionStrategy.PerStrawman =>
                query.Where(x => !string.IsNullOrWhiteSpace(x.StrawManId) && strawmanIds.Contains(x.StrawManId)),
            _ => query,
        };

        var credentials = await query.ToArrayAsync();
        return credentials
            .Select(c => new ResolvedChargeCredential(PaymentGateway.Frendz, c.Id, (c.StrawManId ?? string.Empty).Trim()))
            .ToArray();
    }

    private async Task<ResolvedChargeCredential[]> GetSigiloPayCredentialsAsync(
        IGatewayCredentialScope scope,
        string[] allowedCredentialIds)
    {
        if (scope.GatewaySelectionStrategy is GatewaySelectionStrategy.PerStrawman &&
            scope.StrawManIds.Count == 0)
        {
            return Array.Empty<ResolvedChargeCredential>();
        }

        var strawmanIds = scope.StrawManIds.ToArray();
        var query = _sigiloPayCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup =>
                query.Where(x => allowedCredentialIds.Contains(x.Id)),
            GatewaySelectionStrategy.PerStrawman =>
                query.Where(x => !string.IsNullOrWhiteSpace(x.StrawManId) && strawmanIds.Contains(x.StrawManId)),
            _ => query,
        };

        var credentials = await query.ToArrayAsync();
        return credentials
            .Select(c => new ResolvedChargeCredential(PaymentGateway.SigiloPay, c.Id, (c.StrawManId ?? string.Empty).Trim()))
            .ToArray();
    }

    private async Task<ResolvedChargeCredential[]> GetWintechCredentialsAsync(
        IGatewayCredentialScope scope,
        string[] allowedCredentialIds)
    {
        if (scope.GatewaySelectionStrategy is GatewaySelectionStrategy.PerStrawman &&
            scope.StrawManIds.Count == 0)
        {
            return Array.Empty<ResolvedChargeCredential>();
        }

        var strawmanIds = scope.StrawManIds.ToArray();
        var query = _wintechCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup =>
                query.Where(x => allowedCredentialIds.Contains(x.Id)),
            GatewaySelectionStrategy.PerStrawman =>
                query.Where(x => !string.IsNullOrWhiteSpace(x.StrawManId) && strawmanIds.Contains(x.StrawManId)),
            _ => query,
        };

        var credentials = await query.ToArrayAsync();
        return credentials
            .Select(c => new ResolvedChargeCredential(PaymentGateway.Wintech, c.Id, (c.StrawManId ?? string.Empty).Trim()))
            .ToArray();
    }
}
