using Content.Server.Vehicles.Components;
using Content.Server.Vehicles.Systems;
using Content.Shared.Vehicles.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Vehicles;

public sealed class VehicleModule
{
    public void RegisterComponents()
    {
        IoCManager.Register<TankComponent>();
        IoCManager.Register<TankMovementComponent>();
        IoCManager.Register<TankGunComponent>();
        IoCManager.Register<TankDriverComponent>(); // Новый компонент
    }

    public void RegisterSystems()
    {
        IoCManager.Register<TankSystem>();
        IoCManager.Register<TankMovementSystem>();
        IoCManager.Register<TankGunSystem>();
        IoCManager.Register<TankControllerSystem>();
    }
}
