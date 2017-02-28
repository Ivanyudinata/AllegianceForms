﻿using AllegianceForms.Engine;
using AllegianceForms.Engine.Rocks;
using AllegianceForms.Engine.Ships;
using System.Drawing;
using System.Linq;

namespace AllegianceForms.Orders
{
    public class MineOrder : MoveOrder
    {
        private ResourceAsteroid _targetAsteroid;

        public MineOrder(int sectorId) : this(sectorId, Point.Empty, Point.Empty)
        {
        }

        public MineOrder(int sectorId, PointF targetPosition, PointF offset) : base(sectorId, targetPosition, offset)
        {
            OrderPen.Color = Color.Gold;
        }

        private void SearchForAnotherRock(MinerShip miner)
        {
            _targetAsteroid = StrategyGame.ClosestDistance(OrderPosition.X, OrderPosition.Y, StrategyGame.ResourceAsteroids.Where(_ => _.SectorId == miner.SectorId && _.VisibleToTeam[miner.Team - 1] && _.AvailableResources > 0 && !_.BeingMined));

            if (_targetAsteroid != null)
            {
                var p = _targetAsteroid.CenterPoint;
                OrderPosition = new PointF(p.X + Offset.X, p.Y + Offset.Y);
                OrderSectorId = miner.SectorId;
                miner.Target = _targetAsteroid;
                miner.Target.BeingMined = true;

                if (miner.Team == 1) SoundEffect.Play(ESounds.vo_miner_intransit);
            }
            else
            {
                // No more rocks to mine, done for now...
                OrderComplete = true;

                if (miner.Team == 1) SoundEffect.Play(ESounds.vo_sal_minerpartial);
                miner.OrderShip(new DockOrder(miner, true), true);
            }
        }

        public override void Update(Ship ship)
        {
            base.Update(ship);
            OrderComplete = false;

            var miner = (MinerShip)ship;

            if (_targetAsteroid == null)
            {
                SearchForAnotherRock(miner);
            }
            else
            {
                if (StrategyGame.WithinDistance(_targetAsteroid.CenterX, _targetAsteroid.CenterY, ship.CenterX, ship.CenterY, MinerShip.MineDistance))
                {
                    miner.StopMoving();
                    miner.Mining = true;
                    miner.Target = _targetAsteroid;
                    _targetAsteroid.BeingMined = true;
                }
                if (_targetAsteroid.AvailableResources == 0)
                {
                    _targetAsteroid.BeingMined = false;
                    miner.Mining = false;
                    miner.Target = null;
                    SearchForAnotherRock(miner);
                }
            }
        }

        public override void Cancel(Ship ship)
        {
            var miner = (MinerShip)ship;

            if (miner.Target != null)
            {
                miner.Target.BeingMined = false;
                miner.Target = null;
            }
        }
    }
}