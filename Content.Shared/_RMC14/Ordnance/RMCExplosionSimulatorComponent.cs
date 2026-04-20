using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ordnance;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCExplosionSimulatorComponent : Component
{
    public static readonly TimeSpan ProcessingDuration = TimeSpan.FromSeconds(120);

    [DataField, AutoNetworkedField]
    public RMCExplosionSimulatorTarget SelectedTarget = RMCExplosionSimulatorTarget.Marines;

    [DataField, AutoNetworkedField]
    public bool IsProcessing;

    [DataField, AutoNetworkedField]
    public TimeSpan ProcessingEnd;

    [DataField, AutoNetworkedField]
    public bool SimulationReady;

    [DataField, AutoNetworkedField]
    public NetEntity? ChamberCamera;

    [DataField, AutoNetworkedField]
    public bool LastHasExplosion;

    [DataField, AutoNetworkedField]
    public float LastTotalIntensity;

    [DataField, AutoNetworkedField]
    public float LastIntensitySlope;

    [DataField, AutoNetworkedField]
    public float LastMaxIntensity;

    [DataField, AutoNetworkedField]
    public bool LastHasFire;

    [DataField, AutoNetworkedField]
    public float LastFireIntensity;

    [DataField, AutoNetworkedField]
    public float LastFireRadius;

    [DataField, AutoNetworkedField]
    public float LastFireDuration;

    // Server-only transient state
    public EntityUid ChamberMapEnt = EntityUid.Invalid;
    public EntityUid ChamberCameraEnt = EntityUid.Invalid;
    public EntityUid ProcessingActor = EntityUid.Invalid;
    public bool PendingExplosion;
    public TimeSpan ExplosionAt;
    public float PendingTotalIntensity;
    public float PendingIntensitySlope;
    public float PendingMaxIntensity;
}

public enum RMCExplosionSimulatorTarget : byte
{
    Marines = 0,
    SpecialForces = 1,
    Xenomorphs = 2,
}
