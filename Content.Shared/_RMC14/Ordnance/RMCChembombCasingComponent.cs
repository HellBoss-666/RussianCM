using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ordnance;

public enum RMCCasingAssemblyStage : byte
{
    Open,    // can insert/remove detonator, fill chemicals
    Sealed,  // screwdriver applied — chemicals locked in, detonator slot locked
    Armed,   // wirecutters applied — ready to trigger
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCChembombCasingComponent : Component
{
    /// <summary>Maximum chemical volume this casing can hold.</summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 MaxVolume;

    /// <summary>Base explosion power before chemical modifiers.</summary>
    [DataField, AutoNetworkedField]
    public float BasePower = 180f;

    /// <summary>Base explosion falloff before chemical modifiers.</summary>
    [DataField, AutoNetworkedField]
    public float BaseFalloff = 80f;

    /// <summary>Number of shrapnel projectiles spawned on detonation.</summary>
    [DataField, AutoNetworkedField]
    public int BaseShards;

    /// <summary>Whether a detonator assembly has been inserted.</summary>
    [DataField, AutoNetworkedField]
    public bool HasActiveDetonator;

    /// <summary>Current assembly stage of the casing.</summary>
    [DataField, AutoNetworkedField]
    public RMCCasingAssemblyStage Stage;

    /// <summary>Name of the solution container holding the chemicals.</summary>
    [DataField]
    public string ChemicalSolution = "chemicals";

    // Fire stat ranges for this casing type
    [DataField]
    public float MinFireIntensity = 3f;
    [DataField]
    public float MaxFireIntensity = 25f;
    [DataField]
    public float MinFireRadius = 1f;
    [DataField]
    public float MaxFireRadius = 5f;
    [DataField]
    public float MinFireDuration = 3f;
    [DataField]
    public float MaxFireDuration = 24f;

    /// <summary>Default fire entity spawned if no reagent-specific fire is set.</summary>
    [DataField, AutoNetworkedField]
    public EntProtoId DefaultFireEntity = "RMCTileFire";

    /// <summary>Minimum effective falloff (CMSS13 minimum = 25).</summary>
    [DataField]
    public float MinFalloff = 25f;

    /// <summary>Shrapnel projectile prototype ID.</summary>
    [DataField]
    public EntProtoId ShrapnelProto = "RMCShrapnel";
}

[Serializable, NetSerializable]
public sealed partial class RMCCasingScrewDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCCasingCutDoAfterEvent : SimpleDoAfterEvent;
