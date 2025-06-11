using Content.Shared.RuMC.Vehicles.Components;
using Content.Shared.RuMC.Vehicles.Systems;
using Content.Shared.Vehicles.Components;

namespace Content.Shared.RuMC.Vehicles;

/// <summary>
/// Модуль техники
/// </summary>
public sealed class VehicleModule
{
    /// <summary>
    /// Регистрация компонентов техники
    /// </summary>
    public static void RegisterComponents()
    {
        IoCManager.Register<TankComponent>();
        IoCManager.Register<TankMovementComponent>();
        IoCManager.Register<TankGunComponent>();
        IoCManager.Register<TankDriverComponent>(); // Новый компонент
    }

    /// <summary>
    /// Регистрация систем техники
    /// </summary>
    public static void RegisterSystems()
    {
        IoCManager.Register<TankSystem>();
        IoCManager.Register<TankMovementSystem>();
        IoCManager.Register<TankGunSystem>();
        IoCManager.Register<TankControllerSystem>();
    }
}
