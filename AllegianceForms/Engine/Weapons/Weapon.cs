﻿using AllegianceForms.Engine.Ships;
using System.Drawing;

namespace AllegianceForms.Engine.Weapons
{
    public abstract class Weapon
    {
        public bool Firing { get; set; }
        public bool Shooting { get; protected set; }
        public int ShootingTicks { get; protected set; }
        public int ShootingDelayTicks { get; protected set; }
        public float WeaponRange { get; set; }
        public float WeaponDamage { get; set; }
        public GameEntity Target { get; set; }
        public Ship Shooter { get; set; }
        public PointF FireOffset { get; set; }

        protected bool _damageOnShotEnd = true;
        protected ESounds _weaponSound = ESounds.plasmaac1;
        protected int _shootingStop = int.MaxValue;
        protected int _shootingNext = 0;
        
        protected Weapon(int fireTicks, int refireTicks, float range, float damage, Ship shooter, PointF offset)
        {
            ShootingTicks = _shootingStop = fireTicks;
            ShootingDelayTicks = refireTicks;
            Shooting = false;
            Shooter = shooter;
            WeaponRange = range;
            WeaponDamage = damage;
            FireOffset = offset;
        }

        public virtual void Update(int currentSectorId)
        {
            _shootingStop--;
            _shootingNext--;

            if (!Shooting && Firing && _shootingNext <= 0 && Target != null && Target.Active)
            {
                Shooting = true;
                _shootingStop = ShootingTicks;
                if (currentSectorId == Shooter.SectorId) SoundEffect.Play(_weaponSound);
            }

            if (Shooting && _shootingStop <= 0)
            {
                if (_damageOnShotEnd) DamageTarget();
                Shooting = false;
                _shootingNext = ShootingDelayTicks;
            }

            CheckForANewTarget();
        }

        public abstract void Draw(Graphics g, int currentSectorId);

        public abstract void DamageTarget();

        public abstract void CheckForANewTarget();

    }
}
