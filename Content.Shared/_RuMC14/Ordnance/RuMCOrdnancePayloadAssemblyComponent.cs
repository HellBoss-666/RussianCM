using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RuMC14.Ordnance;

[RegisterComponent]
public sealed partial class RMCOrdnancePayloadAssemblyComponent : Component
{
    [DataField]
    public string FuelSolution = "fuel";

    [DataField]
    public string ChemicalSolution = "chemicals";

    [DataField]
    public FixedPoint2 RequiredFuel = 60;

    [DataField(required: true)]
    public List<RMCOrdnancePayloadAssemblyResult> Results = new();
}

[DataDefinition]
public sealed partial class RMCOrdnancePayloadAssemblyResult
{
    [DataField(required: true)]
    public EntProtoId Payload = default!;

    [DataField(required: true)]
    public EntProtoId Result = default!;
}
