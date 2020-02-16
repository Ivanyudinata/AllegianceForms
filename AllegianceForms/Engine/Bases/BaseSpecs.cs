﻿using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AllegianceForms.Engine.Bases
{
    public class BaseSpecs
    {
        public List<BaseSpec> Bases { get; set; }
        private StrategyGame _game;

        private BaseSpecs(StrategyGame game, IEnumerable<BaseSpec> items)
        {
            Bases = items.ToList();
            _game = game;
        }

        public static BaseSpecs LoadBaseSpecs(StrategyGame game, string baseFile)
        {
            var cfg = new CsvConfiguration()
            {
                WillThrowOnMissingField = false,
                IgnoreBlankLines = true,
                AllowComments = true
            };

            using (var textReader = File.OpenText(baseFile))
            {
                var csv = new CsvReader(textReader, cfg);

                var records = csv.GetRecords<BaseSpec>().ToList();

                return new BaseSpecs(game, records);
            }
        }

        public static bool IsTower(EBaseType type)
        {
            return type == EBaseType.MissileTower || type == EBaseType.Tower || type == EBaseType.RepairTower || type == EBaseType.Minefield || type == EBaseType.ShieldTower;
        }

        public Base CreateBase(EBaseType baseType, int team, Color teamColour, int sectorId, bool addPilots = true)
        {
            var spec = Bases.FirstOrDefault(_ => _.Type == baseType);
            if (spec == null) return null;

            var t = team - 1;
            var faction = _game.Faction[t];
            var research = _game.TechTree[t].ResearchedUpgrades;
            var settings = _game.GameSettings;
            var alliance = (t < 0) ? -1 : settings.TeamAlliance[t];
            if (addPilots && _game.TotalPilots[t] + spec.Pilots < _game.GameSettings.MaximumPilots)
            {
                _game.DockedPilots[t] += spec.Pilots;
                _game.TotalPilots[t] += spec.Pilots;
            }

            if (baseType == EBaseType.Shipyard)
            {
                faction.CapitalMaxDrones += settings.InitialCapitalMaxDrones;
            }

            var bse = new Base(_game, baseType, spec.Width, spec.Height, teamColour, team, alliance, spec.Health * settings.StationHealthMultiplier[spec.Type] * faction.Bonuses.Health, sectorId);

            bse.ScanRange = spec.ScanRange * research[EGlobalUpgrade.ScanRange] * faction.Bonuses.ScanRange;
            bse.Signature = spec.Signature * research[EGlobalUpgrade.ShipSignature] * settings.StationSignatureMultiplier[spec.Type] * faction.Bonuses.Signature;

            return bse;
        }

        public void DestroyBase(EBaseType baseType, int team)
        {
            if (team == 0) return;
            var spec = Bases.FirstOrDefault(_ => _.Type == baseType);
            if (spec == null) return;

            var t = team - 1;
            _game.DockedPilots[t] -= spec.Pilots;
            _game.TotalPilots[t] -= spec.Pilots;

            var faction = _game.Faction[t];
            var settings = _game.GameSettings;
            if (baseType == EBaseType.Shipyard)
            {
                faction.CapitalMaxDrones -= settings.InitialCapitalMaxDrones;
            }
        }

        public void CaptureBase(EBaseType baseType, int team, int newTeam)
        {
            if (team == 0 || newTeam == 0) return;
            var spec = Bases.FirstOrDefault(_ => _.Type == baseType);
            if (spec == null) return;

            _game.DockedPilots[team - 1] -= spec.Pilots;
            _game.TotalPilots[team - 1] -= spec.Pilots;
            _game.DockedPilots[newTeam - 1] += spec.Pilots;
            _game.TotalPilots[newTeam - 1] += spec.Pilots;
        }
    }

    public class BaseSpec
    {
        public int Id { get; set; }
        public EBaseType Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Health { get; set; }
        public int ScanRange { get; set; }
        public float Signature { get; set; }
        public int Pilots { get; set; }
    }
}
