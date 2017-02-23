﻿using AllegianceForms.Engine.Ships;
using System.Drawing;
using System.Linq;

namespace AllegianceForms.Engine.Weapons
{
    public abstract class ShipWeapon : Weapon
    {
        protected ShipWeapon(int fireTimeMS, int refireDelayMS, float range, float damage, Ship shooter, PointF offset)
            : base(fireTimeMS, refireDelayMS, range, damage, shooter, offset)
        {
        }

        public override void DamageTarget()
        {
            var ship = Target as Ship;
            if (ship != null && Shooter.SectorId == Target.SectorId)
            {
                ship.Damage(WeaponDamage);
                if (!ship.Active) Target = null;
            }
        }

        public override void CheckForANewTarget()
        {
            // Always be checking for targets in range and FIRE!
            if (Shooter == null || !Shooter.Active || Shooting) return;

            var t = Target as Ship;
            if (t == null || !t.Active || t.SectorId != Shooter.SectorId || t.Docked || !t.VisibleToTeam[Shooter.Team - 1] || !StrategyGame.WithinDistance(Shooter.CenterX, Shooter.CenterY, Target.CenterX, Target.CenterY, WeaponRange))
            {
                var enemysInRange = StrategyGame.AllUnits.Where(_ => _.Active && _.Team != Shooter.Team && !_.Docked && Shooter.SectorId == _.SectorId && _.VisibleToTeam[Shooter.Team - 1] && _.Type != EShipType.Lifepod && StrategyGame.WithinDistance(Shooter.CenterX, Shooter.CenterY, _.CenterX, _.CenterY, WeaponRange)).ToList();
                
                if (enemysInRange.Count > 1)
                {
                    Target = enemysInRange[StrategyGame.Random.Next(enemysInRange.Count)];
                    Firing = true;
                }
                else if (enemysInRange.Count == 1)
                {
                    Target = enemysInRange[0];
                    Firing = true;
                }
                else
                {
                    Target = null;
                    Firing = false;
                }
            }
        }
    }
}