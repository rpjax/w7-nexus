using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Mandates.Domain.Catalog;

public readonly record struct PresetGrantSpec(string Capability, MandateScope Scope);

public static class PresetCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<PresetGrantSpec>> Bundles =
        new Dictionary<string, IReadOnlyList<PresetGrantSpec>>(StringComparer.OrdinalIgnoreCase)
        {
            [PresetIds.Recruiter] =
            [
                new(Capabilities.Recrutar, MandateScope.CarteiraDirect()),
                new(Capabilities.ConcederRecrutamento, MandateScope.CarteiraDirect()),
                new(Capabilities.ConcederMandato, MandateScope.CarteiraDirect())
            ],
            [PresetIds.OperationsManager] =
            [
                new(Capabilities.GerirOperacao, MandateScope.OperationAll()),
                new(Capabilities.Onboard, MandateScope.OperationAll()),
                new(Capabilities.ConcederMandato, MandateScope.OperationAll())
            ],
            [PresetIds.Accountant] =
            [
                new(Capabilities.RegistrarMovimentoFinanceiro, MandateScope.Organization()),
                new(Capabilities.VerFinanceiroAmplo, MandateScope.Organization())
            ],
            [PresetIds.Gateways] =
            [
                new(Capabilities.GerirGateways, MandateScope.Organization()),
                new(Capabilities.Onboard, MandateScope.Organization())
            ],
            [PresetIds.Operator] =
            [
                new(Capabilities.AtuarComoOperador, MandateScope.Organization())
            ],
            [PresetIds.Orange] =
            [
                new(Capabilities.AtuarComoLaranja, MandateScope.Organization())
            ]
        };

    public static bool TryGetBundle(string presetId, out IReadOnlyList<PresetGrantSpec> grants)
    {
        if (!PresetIds.IsKnown(presetId))
        {
            grants = [];
            return false;
        }

        grants = Bundles[PresetIds.Normalize(presetId)];
        return true;
    }
}
