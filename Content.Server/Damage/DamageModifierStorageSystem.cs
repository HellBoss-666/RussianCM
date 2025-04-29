using System.Collections.Generic;
using Content.Shared.Damage.Components;
using Content.Shared.Storage;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.FixedPoint;

namespace Content.Server.Damage
{
    public sealed class DamageModifierStorageSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageModifierStorageComponent, Content.Shared.Weapons.Melee.Events.GetMeleeDamageEvent>(OnGetMeleeDamage);
        }

        private void OnGetMeleeDamage(EntityUid uid, DamageModifierStorageComponent component, ref Content.Shared.Weapons.Melee.Events.GetMeleeDamageEvent args)
        {
            if (!_entityManager.TryGetComponent(uid, out StorageComponent? storage))
                return;

            if (storage.Container == null || storage.Container.ContainedEntities.Count == 0)
                return;

            FixedPoint2 totalCount = FixedPoint2.Zero;

            foreach (var entity in storage.Container.ContainedEntities)
            {
                if (!_entityManager.TryGetComponent(entity, out MetaDataComponent? meta))
                    continue;

                var protoId = meta.EntityPrototype?.ID;

                if (protoId == null)
                    continue;

                if (!protoId.StartsWith(component.TargetItemBaseId, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (_entityManager.TryGetComponent(entity, out StackComponent? stack))
                {
                    totalCount += (FixedPoint2)stack.Count;
                }
                else
                {
                    totalCount += (FixedPoint2)1;
                }
            }

            if (totalCount == FixedPoint2.Zero)
                return;

            FixedPoint2 totalIncrease = component.DamageIncrease * totalCount;

            var newDamageDict = new Dictionary<string, FixedPoint2>(args.Damage.DamageDict);

            if (newDamageDict.ContainsKey("Blunt"))
                newDamageDict["Blunt"] += totalIncrease;
            else
                newDamageDict["Blunt"] = totalIncrease;

            args.Damage = new Content.Shared.Damage.DamageSpecifier()
            {
                DamageDict = newDamageDict
            };
        }
    }
}
