﻿using AllegianceForms.Engine.Rocks;
using AllegianceForms.Engine.Ships;
using AllegianceForms.Orders;
using System.Drawing;
using System.Linq;

namespace AllegianceForms.Engine.AI.Missions
{
    public class MinerMission : CommanderMission
    {
        public MinerMission(StrategyGame game, CommanderAI ai, Ship.ShipEventHandler shipEvent) : base(game, ai, shipEvent)
        {
        }

        public override void UpdateMission()
        {
            base.UpdateMission();

            var t = AI.Team - 1;
            
            var possibleRocks = _game.ResourceAsteroids.Where(_ => _.Active && _.VisibleToTeam[t] && _.AvailableResources > 0 && !_.BeingMined).ToList();
            var ships = _game.AllUnits.Where(_ => _.Active && _.Team == AI.Team && _.Type == EShipType.Miner && _.CurrentOrder == null).ToList();
            if (possibleRocks.Count == 0 || ships.Count == 0) return;

            var totalResourcesInSector = new int[_game.Map.Sectors.Count];
            var enemiesInSector = new int[_game.Map.Sectors.Count];
            var friendliesInSector = new int[_game.Map.Sectors.Count];

            foreach (var r in possibleRocks)
            {
                totalResourcesInSector[r.SectorId] += r.AvailableResources;
            }

            for (var i = 0; i < totalResourcesInSector.Length; i++)
            {
                if (totalResourcesInSector[i] == 0) continue;
                enemiesInSector[i] = _game.AllUnits.Count(_ => _.Active && _.VisibleToTeam[t] && _.Alliance != AI.Alliance && _.CanAttackShips());
                friendliesInSector[i] = _game.AllUnits.Count(_ => _.Active && _.Alliance == AI.Alliance && _.CanAttackShips());
            }

            var bestScore = float.MaxValue;
            var bestSectorId = -1;
            var startSectorId = ships[0].SectorId;
            var maxHops = _game.Map.Wormholes.Count + 4;

            for (var i = 0; i < totalResourcesInSector.Length; i++)
            {
                if (totalResourcesInSector[i] == 0 || enemiesInSector[i] > friendliesInSector[i]) continue;
                
                var path = _game.Map.ShortestPath(AI.Team, startSectorId, i);
                var newHops = path == null ? int.MaxValue : path.Count();

                var score = newHops + enemiesInSector[i] - friendliesInSector[i] - (1f * totalResourcesInSector[i] / ResourceAsteroid.MaxResources);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestSectorId = i;
                }
            }

            if (bestSectorId == -1) return;
            var centerPos = new PointF(StrategyGame.ScreenWidth / 2f, StrategyGame.ScreenHeight / 2f);

            // Order our miner ships into the best sector if there is no enemy presense > our own
            foreach (var s in ships)
            {
                var m = s as MinerShip;
                if (m == null) continue;

                if (m.SectorId != bestSectorId)
                {
                    m.OrderShip(new NavigateOrder(_game, m, bestSectorId));
                    LogOrder();
                }
                m.OrderShip(new MineOrder(_game, bestSectorId, centerPos, PointF.Empty), true);
                LogOrder();
            }
        }
    }
}