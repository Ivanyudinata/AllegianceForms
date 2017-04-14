﻿using AllegianceForms.Engine.Ships;
using AllegianceForms.Orders;
using AllegianceForms.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace AllegianceForms.Engine.AI.Missions
{
    public class CommanderMission
    {
        public List<Ship> IncludedShips { get; set; }
        public BaseAI AI { get; set; }

        public List<DateTime> RecentOrders { get; set; }

        protected Ship.ShipEventHandler _shipHandler;
        protected bool _completed = false;
        protected int _recentOrderDelaySecs = 10;
        protected PointF _centerPos;

        public CommanderMission(BaseAI ai, Ship.ShipEventHandler shipHandler)
        {
            AI = ai;
            IncludedShips = new List<Ship>();
            RecentOrders = new List<DateTime>();
            _shipHandler = shipHandler;
            _centerPos = new PointF(StrategyGame.ScreenWidth / 2, StrategyGame.ScreenHeight / 2);

        }

        public virtual void UpdateMission()
        {
            IncludedShips.RemoveAll(_ => !_.Active);
            var cutOffTime = DateTime.Now.AddSeconds(_recentOrderDelaySecs);
            RecentOrders.RemoveAll(_ => _ > cutOffTime);

            var stillCompleted = MissionComplete();

            if (stillCompleted && !_completed)
            {
                _completed = true;
                IncludedShips.ForEach(_ => _.OrderShip(new DockOrder(_)));
                IncludedShips.Clear();
            }
            else if (_completed && !stillCompleted)
            {
                _completed = false;
            }
        }

        protected void LogOrder()
        {
            RecentOrders.Add(DateTime.Now);
        }

        public virtual bool RequireMorePilots()
        {
            return false;
        }

        public virtual void AddMorePilots()
        {
        }

        public virtual bool MissionComplete()
        {
            return false;
        }
    }
}
