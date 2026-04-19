using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ordnance;

/// <summary>
///     Marks an entity as an ordnance assembly part (igniter or timer).
///     Two parts of compatible types are combined via interaction to produce a detonator assembly.
/// </summary>
public enum RMCOrdnancePartType : byte
{
    Igniter,
    Timer,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOrdnancePartComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public RMCOrdnancePartType PartType;
}
