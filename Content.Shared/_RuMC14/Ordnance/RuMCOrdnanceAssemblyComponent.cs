using Robust.Shared.GameStates;

namespace Content.Shared._RuMC14.Ordnance;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOrdnanceAssemblyComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCOrdnancePartType? LeftPartType;

    [DataField, AutoNetworkedField]
    public RMCOrdnancePartType? RightPartType;

    [DataField, AutoNetworkedField]
    public bool
}
