using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ordnance;

/// <summary>
///     Marks a casing as a mine-type ordnance.
///     Uses the same anchor-in-place deployment pattern as RMCLandmineComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCMineCasingComponent : Component
{
    /// <summary>
    /// How long (seconds) it takes to plant the mine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PlacementDelay = 3f;

    /// <summary>
    /// Whether the mine has been planted (anchored to the floor).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Planted;
}

[Serializable, NetSerializable]
public sealed partial class RMCMinePlantDoAfterEvent : SimpleDoAfterEvent;
