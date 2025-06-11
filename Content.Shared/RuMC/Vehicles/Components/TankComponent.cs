using Robust.Shared.GameStates;

namespace Content.Shared.RuMC.Vehicles.Components;

/// <summary>
/// Атрибуты танка
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TankComponent : Component
{
    [DataField]
    public float MaxHealth = 1000f;

    [DataField]
    public float Health = 1000f;

    [DataField]
    public float Armor = 500f;

    [DataField]
    public float MaxFuel = 200f;

    [DataField]
    public float Fuel = 200f;

    [DataField]
    public float FuelConsumptionRate = 0.5f; // Потребление топлива в секунду
}
