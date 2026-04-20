using Content.Shared._RuMC14.Ordnance;
using Robust.Client.GameObjects;

namespace Content.Client._RuMC14.Ordnance;

public sealed class RMCOrdnanceAssemblyVisualsSystem : VisualizerSystem<RMCOrdnanceAssemblyComponent>
{
    private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOrdnanceAssemblyComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private string GetState(RMCOrdnancePartType type, bool left)
    {
        return type switch
        {
            RMCOrdnancePartType.Igniter => left ? "igniter_left" : "igniter_right",
            RMCOrdnancePartType.Timer => left ? "timer_left" : "timer_right",
            RMCOrdnancePartType.Signaler => left ? "signaller_left" : "signaller_right",
            RMCOrdnancePartType.Proximity => left ? "prox_left" : "prox_right",
            _ => "base"
        };
    }

    private void OnAppearanceChanged(EntityUid uid,
        RMCOrdnanceAssemblyComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.AppearanceData.TryGetValue(RMCAssemblyVisualKey.LeftType, out var leftObj) &&
            leftObj is RMCOrdnancePartType left)
        {
            _sprite.LayerSetRsiState(uid, 0, GetState(left, true));
        }

        if (args.AppearanceData.TryGetValue(RMCAssemblyVisualKey.RightType, out var rightObj) &&
            rightObj is RMCOrdnancePartType right)
        {
            _sprite.LayerSetRsiState(uid, 1, GetState(right, false));
        }
    }
}
