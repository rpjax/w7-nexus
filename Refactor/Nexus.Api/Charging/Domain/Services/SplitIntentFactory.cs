using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Charging.Domain.Services;

public sealed record ShareholderSlice(Guid AccountId, decimal PercentOfRemainder);
public sealed record AgencySlice(Guid OperatorId, decimal OperatorPercent, Guid RecruiterId, decimal RecruiterPercent);

public static class SplitIntentFactory
{
    public static IResult<SplitIntent> Create(
        Guid orangeMemberId,
        decimal orangeLevel1Percent,
        IReadOnlyList<ShareholderSlice> shareholders,
        decimal? managementCutPercent,
        AgencySlice agency)
    {
        if (orangeLevel1Percent is < 0 or > 100)
            return FailCut("Cut nivel-1 do Laranja deve estar em [0, 100].");

        var shareSum = shareholders.Sum(s => s.PercentOfRemainder);
        if (shareSum is < 0 or > 100)
            return FailCut("Acionistas devem somar no maximo 100% da base do nivel 2.");

        var management = managementCutPercent ?? 0m;
        if (management is < 0 or > 100)
            return FailCut("Cut de gestao deve estar em [0, 100].");

        if (agency.OperatorPercent + agency.RecruiterPercent > 100)
            return FailCut("Agenciamento (operador + recrutador) nao pode exceder 100% da base do nivel 3.");

        IReadOnlyList<SplitLine> lines =
        [
            new SplitLine(1, SplitIntent.Orange, orangeLevel1Percent, [new SplitParticipant(orangeMemberId, orangeLevel1Percent)]),
            new SplitLine(2, SplitIntent.Shareholders, shareSum,
                shareholders.Select(s => new SplitParticipant(s.AccountId, s.PercentOfRemainder)).ToList()),
            new SplitLine(3, SplitIntent.OperationManagement, management, []),
            new SplitLine(4, SplitIntent.Agency, agency.OperatorPercent + agency.RecruiterPercent,
            [
                new SplitParticipant(agency.OperatorId, agency.OperatorPercent),
                new SplitParticipant(agency.RecruiterId, agency.RecruiterPercent)
            ]),
            new SplitLine(5, SplitIntent.ResidualOrg, 100m, [])
        ];

        return Result<SplitIntent>.Success(new SplitIntent(lines));
    }

    private static IResult<SplitIntent> FailCut(string message) =>
        Result<SplitIntent>.Failure(Error.Create()
            .WithCode(ChargingErrorCodes.InvalidCut)
            .WithMessage(message)
            .Build());
}
