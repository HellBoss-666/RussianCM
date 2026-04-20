using Robust.Shared.GameObjects;

namespace Content.Shared._RuMC14.Ordnance;

[RegisterComponent]
public sealed partial class RMCOrdnanceAssemblyComponent : Component
{
    [DataField]
    public EntityUid? Left;

    [DataField]
    public EntityUid? Right;
}
