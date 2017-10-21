﻿using AllegianceForms.Engine.AI;
using AllegianceForms.Engine.Bases;
using AllegianceForms.Engine.Factions;
using AllegianceForms.Engine.Generation;
using AllegianceForms.Engine.Map;
using AllegianceForms.Engine.QuickChat;
using AllegianceForms.Engine.Rocks;
using AllegianceForms.Engine.Ships;
using AllegianceForms.Engine.Tech;
using AllegianceForms.Engine.Weapons;
using AllegianceForms.Orders;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using static AllegianceForms.Engine.Bases.Base;
using static AllegianceForms.Engine.Ships.Ship;

namespace AllegianceForms.Engine
{
    public class StrategyGame
    {
        public delegate void GameEventHandler(object sender, EGameEventType e);
        public event GameEventHandler GameEvent;

        public const int ScreenPositionOffset_Left = 0;
        public const int ScreenPositionOffset_Top = 0;
        public const int ScreenPositionOffset_Width = -230;
        public const int ScreenPositionOffset_Height = 0;
        
        public static int ScreenWidth = 0;
        public static int ScreenHeight = 0;

        public const int ResourcesInitial = 4000;
        public const int ResourceRegularAmount = 1;
        public const float BaseConversionRate = 4f;
        public const string ShipDataFile = ".\\Data\\Ships.txt";
        public const string BaseDataFile = ".\\Data\\Bases.txt";
        public const string TechDataFile = ".\\Data\\Tech.txt";
        public const string QuickChatDataFile = ".\\Data\\QuickChatCommands.txt";
        public const string RockPicDir = ".\\Art\\Rocks\\";
        public const string IconPicDir = ".\\Art\\Trans\\";
        public const string SoundsDir = ".\\Art\\Sounds\\";
        public const string GamePresetFolder = ".\\Data\\GamePresets";
        public const string FactionPresetFolder = ".\\Data\\FactionPresets";
        public const string MapFolder = ".\\Data\\Maps";
        public static double SqrtTwo = Math.Sqrt(2);
        public static Random Random = new Random();
        public static RandomName RandomName = new RandomName();

        public static Pen HealthBorderPen = new Pen(Color.DimGray, 1);
        public static Pen BaseBorderPen = new Pen(Color.Gray, 2);
        public static Brush ShieldBrush = new SolidBrush(Color.CornflowerBlue);
        public static Brush ResourceBrush = new SolidBrush(Color.MintCream);

        private static StringFormat _centeredFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private static DateTime _nextBbrSoundAllowed = DateTime.MinValue;
        private static TimeSpan _nextBbrSoundDelay = new TimeSpan(0, 0, 3);

        public GameMap Map;
        public GameStats GameStats;
        public GameSettings GameSettings;
        
        public int NumTeams = 2;
        public int PlayerCurrentSectorId = 0;

        public ShipSpecs Ships;
        public BaseSpecs Bases;
        public int[] DockedPilots;
        public int[] Credits;
        public BaseAI[] AICommanders;
        public TechTree[] TechTree;
        public Faction[] Faction;
        public Faction[] Winners;
        public Faction[] Loosers;
        public QuickComms QuickChat;

        public List<Ship> AllUnits = new List<Ship>();
        public List<Base> AllBases = new List<Base>();

        public List<Asteroid> AllAsteroids = new List<Asteroid>();
        public List<ResourceAsteroid> ResourceAsteroids = new List<ResourceAsteroid>();        
        public List<Asteroid> BuildableAsteroids = new List<Asteroid>();
        public List<Minefield> Minefields = new List<Minefield>();
        public List<MissileProjectile> Missiles = new List<MissileProjectile>();

        public Brush[] TeamBrushes;
        public Brush[] TextBrushes;
        public Pen[] SelectedPens;
        public Image[] MinefieldImages;

        public static double AngleBetweenPoints(PointF from, PointF to)
        {
            var deltaX = to.X - from.X;
            var deltaY = to.Y - from.Y;
            
            return Math.Atan2(deltaY, deltaX) * (180 / Math.PI);
        }
        
        public static int PerceivedBrightness(Color c)
        {
            return (int)Math.Sqrt(
            c.R * c.R * .299 +
            c.G * c.G * .587 +
            c.B * c.B * .114);
        }

        public static bool WithinDistance(float x1, float y1, float x2, float y2, float d)
        {
            var dx = (x1 - x2);
            var dy = (y1 - y2);

            return (dx * dx + dy * dy) < d * d;
        }

        public static PointF GetNewPoint(PointF p, float d, float angle)
        {
            var rad = (Math.PI / 180) * angle;
            return new PointF((float)(p.X + d * Math.Cos(rad)), (float)(p.Y + d * Math.Sin(rad)));
        }

        public static double DistanceBetween(Point p1, Point p2)
        {
            var dx = (p1.X - p2.X);
            var dy = (p1.Y - p2.Y);

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static T ClosestDistance<T>(float x, float y, IEnumerable<T> check) where T : GameEntity
        {
            return check.OrderBy(_ => ((x - _.CenterX) * (x - _.CenterX) + (y - _.CenterY) * (y - _.CenterY))).FirstOrDefault();
        }

        public static float Lerp(float firstFloat, float secondFloat, DateTime startTime, TimeSpan duration)
        {
            var by = (float)((DateTime.Now - startTime).TotalMilliseconds / duration.TotalMilliseconds);

            return firstFloat * by + secondFloat * (1 - by);
        }

        // Offset the order position evenly for these units...
        public static void SpreadOrderEvenly<T>(StrategyGame game, List<Ship> units, int currentSectorId, PointF centerPos, bool append = false) where T : ShipOrder
        {
            var columns = (int)Math.Round(Math.Sqrt(units.Count), 0);
            var offset = units.Max(_ => _.Image.Width) + 4;
            var origX = centerPos.X - (int)(columns / 2.0f * offset);
            var origY = centerPos.Y - (int)(columns / 2.0f * offset);
            var orderPos = new PointF(origX, origY);

            for (var i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u.SectorId != currentSectorId) continue;

                ShipOrder order;

                if (typeof(T) == typeof(RefineOrder) || typeof(T) == typeof(DockOrder))
                    order = (T)Activator.CreateInstance(typeof(T), game, u);
                else
                    order = (T)Activator.CreateInstance(typeof(T), game, currentSectorId);

                order.OrderPosition = orderPos;
                order.Offset = new PointF(orderPos.X - origX, orderPos.Y - origY);
                u.OrderShip(order, append);

                if ((i + 1) % columns == 0)
                {
                    orderPos.X = origX;
                    orderPos.Y += offset;
                }
                else
                {
                    orderPos.X += offset;
                }
            }
        }

        public static void DrawCenteredText(Graphics g, Brush brush, string text, Rectangle rect)
        {
            g.DrawString(text, SystemFonts.SmallCaptionFont, brush, rect, _centeredFormat);
        }

        public static Color NewAlphaColour(int A, Color color)
        {
            return Color.FromArgb(A, color.R, color.G, color.B);
        }

        public GameEntity NextWormholeEnd(int team, int fromSectorId, int toSectorId, out GameEntity _otherEnd)
        {
            var path = Map.ShortestPath(team, fromSectorId, toSectorId);

            _otherEnd = null;
            if (path == null || path.Count == 0) return null;

            var nextSector = path[path.Count - 1];

            foreach (var w in Map.Wormholes)
            {
                if (w.End1.SectorId == fromSectorId && nextSector.Id == w.End2.SectorId)
                {
                    _otherEnd = w.End2;
                    return w.End1;
                }
                else if (w.End2.SectorId == fromSectorId && nextSector.Id == w.End1.SectorId)
                {
                    _otherEnd = w.End1;
                    return w.End2;
                }
            }

            return null;  
        }

        public Base ClosestSectorWithBase(int team, int fromSectorId)
        {
            var t = team - 1;

            var thisSectorBase = AllBases.FirstOrDefault(_ => _.Active && _.Team == team && _.SectorId == fromSectorId && _.CanLaunchShips());
            if (thisSectorBase != null)
            {
                return thisSectorBase;
            }

            var otherSectorBases = AllBases.Where(_ => _.Active && _.Team == team && _.SectorId != fromSectorId && _.CanLaunchShips()).ToList();
            var minHops = int.MaxValue;
            Base targetBase = null;

            foreach ( var b in otherSectorBases)
            {
                var path = Map.ShortestPath(team, fromSectorId, b.SectorId);
                var newHops = path == null ? int.MaxValue : path.Count();

                if (newHops < minHops)
                {
                    minHops = newHops;
                    targetBase = b;
                }
            }

            return targetBase;
        }

        internal static bool RandomChance(float v)
        {
            return StrategyGame.Random.NextDouble() <= v;
        }

        public Base RandomEnemyBase(int team, out Base launchBase)
        {
            launchBase = null;

            var t = team - 1;
            var alliance = GameSettings.TeamAlliance[t];

            // Check each sector for visible enemy bases & our team's bases
            var sectorEnemyBases = new int[Map.Sectors.Count];
            var sectorTeamBases = new int[Map.Sectors.Count];
            var enemySectors = new List<int>();
            var teamSectors = new List<int>();
            var enemiesFound = false;

            foreach (var b in AllBases)
            {
                if (!b.Active) continue;

                if (b.Team == team && b.CanLaunchShips())
                {
                    sectorTeamBases[b.SectorId]++;
                }

                if (b.Alliance != alliance && b.VisibleToTeam[t])
                {
                    enemiesFound = true;
                    sectorEnemyBases[b.SectorId]++;
                }
            }
            if (!enemiesFound) return null;

            // If there are any sectors with both, battle there!
            var possibleEnemySectors = new List<int>();
            for (var i = 0; i < sectorEnemyBases.Length; i++)
            {
                if (sectorEnemyBases[i] > 0 && sectorTeamBases[i] > 0)
                    possibleEnemySectors.Add(i);

                if (sectorEnemyBases[i] > 0) enemySectors.Add(i);
                if (sectorTeamBases[i] > 0) teamSectors.Add(i);
            }

            if (possibleEnemySectors.Count > 0)
            {
                var combatSector = possibleEnemySectors[Random.Next(possibleEnemySectors.Count - 1)];
                var targetBases = AllBases.Where(_ => _.Active && _.VisibleToTeam[t] && _.Alliance != alliance && _.SectorId == combatSector).ToList();
                var launchBases = AllBases.Where(_ => _.Active && _.Team == team && _.SectorId == combatSector && _.CanLaunchShips()).ToList();
                launchBase = launchBases[Random.Next(launchBases.Count - 1)];
                return targetBases[Random.Next(targetBases.Count - 1)];
            }

            // Otherwise, find enemy sectors within 1 jumps from ours
            var possibleTeamSectors = new List<int>();
            for (var i = 0; i < teamSectors.Count; i++)
            {
                for (var j = 0; j < enemySectors.Count; j++)
                {
                    if (Map.AreSectorsWithinJumps(team, 1, teamSectors[i], enemySectors[j]))
                    {
                        possibleEnemySectors.Add(enemySectors[j]);
                        possibleTeamSectors.Add(teamSectors[i]);
                    }
                }
            }

            if (possibleEnemySectors.Count > 0)
            {
                // Choose a random target & launch site
                var r = Random.Next(possibleEnemySectors.Count - 1);
                var targetSector = possibleEnemySectors[r];
                var launchSector = possibleTeamSectors[r];

                var targetBases = AllBases.Where(_ => _.Active && _.VisibleToTeam[t] && _.Alliance != alliance && _.SectorId == targetSector).ToList();
                var launchBases = AllBases.Where(_ => _.Active && _.Team == team && _.SectorId == launchSector && _.CanLaunchShips()).ToList();

                launchBase = launchBases[Random.Next(launchBases.Count - 1)];
                return targetBases[Random.Next(targetBases.Count - 1)];
            }

            // Fall back to targeting their last base!
            return LastEnemyBase(team, out launchBase);
        }

        public Base LastEnemyBase(int team, out Base launchBase)
        {
            var t = team - 1;
            var alliance = GameSettings.TeamAlliance[t];

            // Simply launch from our last base, targeting their last base!
            launchBase = AllBases.LastOrDefault(_ => _.Active && _.Team == team && _.CanLaunchShips());
            return AllBases.LastOrDefault(_ => _.Active && _.VisibleToTeam[t] && _.Alliance != alliance);
        }

        public Base ClosestEnemyBase(int team, out Base launchingBase)
        {
            var t = team - 1;
            var alliance = GameSettings.TeamAlliance[t];
            var maxHops = Map.Wormholes.Count + 4;
            var minHops = int.MaxValue;
            Base targetBase = null;

            launchingBase = AllBases.FirstOrDefault(_ => _.Active && _.Team == team && _.CanLaunchShips());
            if (launchingBase == null) return targetBase;

            var launchingSector = launchingBase.SectorId;

            var otherSectorBases = AllBases.Where(_ => _.Active && _.VisibleToTeam[t] && _.Alliance != alliance && _.SectorId != launchingSector).ToList();
            foreach (var b in otherSectorBases)
            {
                var path = Map.ShortestPath(team, launchingSector, b.SectorId);
                var newHops = path == null ? int.MaxValue : path.Count();

                if (newHops < minHops)
                {
                    minHops = newHops;
                    targetBase = b;
                }
            }

            return targetBase;
        }
        
        public int NumberOfCapitalDrones(int team, string name)
        {
            var type = (EShipType)Enum.Parse(typeof(EShipType), name);
            return (from c in AllUnits
                    where c.Active
                    && c.Team == team
                    && c.Type == type
                    select c).Count();
        }

        public int NumberOfMinerDrones(int team)
        {
            return (from c in AllUnits
                    where c.Active
                    && c.Team == team
                    && c.Type == EShipType.Miner
                    select c).Count();
        }

        public int NumberOfConstructionDrones(string name, int team)
        {
            var bType = TechItem.GetBaseType(name);

            var cons = (from c in AllUnits
                        where c.Active
                        && c.Team == team
                        && c.Type == EShipType.Constructor
                        && c.GetType() == typeof(BuilderShip)
                        select c as BuilderShip).ToList();
            return cons.Count(_ => _.BaseType == bType);
        }
        
        public void UpdateVisibility(bool init = false, int currentSectorId = -1)
        {
            var soundPlayed = false;
            var preVis = false;
            var playerAlliance = GameSettings.TeamAlliance[0];

            lock (AllUnits)
            {
                foreach (var s in AllUnits)
                {
                    for (var t = 0; t < NumTeams; t++)
                    {
                        if (s.Team == t + 1) continue;
                        var alliance = GameSettings.TeamAlliance[t];

                        if (alliance == s.Alliance)
                        {
                            s.VisibleToTeam[t] = true;
                            continue;
                        }

                        var thisAi = AICommanders[t];
                        if (thisAi != null && thisAi.CheatVisibility)
                        {
                            s.VisibleToTeam[t] = true;
                            continue;
                        }

                        var thatAi = AICommanders[s.Team - 1];
                        if (thatAi != null && thatAi.ForceVisible)
                        {
                            s.VisibleToTeam[t] = true;
                            continue;
                        }

                        preVis = s.VisibleToTeam[t];
                        s.VisibleToTeam[t] = false;
                        if (IsVisibleToAlliance(s, alliance))
                        {
                            s.VisibleToTeam[t] = true;

                            if (!preVis && !soundPlayed && t == 0 && s.SectorId == currentSectorId)
                            {
                                SoundEffect.Play(ESounds.newtargetenemy);
                                soundPlayed = true;
                            }

                            if (!preVis && alliance == playerAlliance && s.CanAttackBases())
                            {
                                ESounds sound;

                                if (AllBases.Any(_ => _.Active && _.Team == 1 && _.SectorId == s.SectorId && _.CanLaunchShips()))
                                {
                                    OnGameEvent(new GameAlert(s.SectorId, $"Station at risk by {s.Type} in {Map.Sectors[s.SectorId]}!"), EGameEventType.ImportantMessage);
                                    sound = ESounds.vo_sal_stationrisk;
                                }
                                else if (Ship.IsCapitalShip(s.Type))
                                {
                                    sound = ESounds.vo_sal_capitalsighted;
                                }
                                else
                                {
                                    sound = ESounds.vo_sal_bombersighted;
                                }

                                if (_nextBbrSoundAllowed < DateTime.Now)
                                {
                                    SoundEffect.Play(sound, true);
                                    _nextBbrSoundAllowed = DateTime.Now + _nextBbrSoundDelay;
                                }
                            }
                        }
                    }
                }
            }

            lock (AllBases)
            {
                foreach (var s in AllBases)
                {
                    for (var t = 0; t < NumTeams; t++)
                    {
                        // Once visible, bases are always visible!
                        if (s.Team == t + 1 || s.VisibleToTeam[t]) continue;
                        var alliance = GameSettings.TeamAlliance[t];

                        if (alliance == s.Alliance)
                        {
                            s.VisibleToTeam[t] = true;
                            continue;
                        }

                        var thisAi = AICommanders[t];
                        if (thisAi != null && thisAi.CheatVisibility)
                        {
                            s.VisibleToTeam[t] = true;
                            continue;
                        }

                        var thatAi = AICommanders[s.Team - 1];
                        if (thatAi != null && thatAi.ForceVisible)
                        {
                            s.VisibleToTeam[t] = true;
                            continue;
                        }

                        if (IsVisibleToAlliance(s, alliance))
                        {
                            if (!soundPlayed && t == 0 && s.SectorId == currentSectorId)
                            {
                                SoundEffect.Play(ESounds.newtargetenemy);
                                soundPlayed = true;
                            }
                            s.VisibleToTeam[t] = true;
                        }
                    }
                }
            }

            foreach (var s in AllAsteroids)
            {
                for (var t = 0; t < NumTeams; t++)
                {
                    // Once visible, asteroids are always visible!
                    if (s.VisibleToTeam[t]) continue;

                    var alliance = GameSettings.TeamAlliance[t];
                    
                    var thisAi = AICommanders[t];
                    if (thisAi != null && thisAi.CheatVisibility)
                    {
                        s.VisibleToTeam[t] = true;
                        continue;
                    }

                    if (IsVisibleToAlliance(s, alliance))
                    {
                        if (!soundPlayed && t == 0 && s.SectorId == currentSectorId)
                        {
                            if (!init) SoundEffect.Play(ESounds.noncriticalmessage);
                            soundPlayed = true;
                        }
                        s.VisibleToTeam[t] = true;
                    }
                }
            }

            foreach (var w in Map.Wormholes)
            {
                for (var t = 0; t < NumTeams; t++)
                {
                    var team = t + 1;
                    // Once visible, wormholes are always visible!
                    var s1 = w.End1;
                    var s2 = w.End2;
                    if (s1.VisibleToTeam[t] || s2.VisibleToTeam[t]) continue;

                    var thisAi = AICommanders[t];
                    if (thisAi != null && thisAi.CheatVisibility)
                    {
                        w.SetVisibleToTeam(team, true);
                        continue;
                    }

                    if (IsVisibleToTeam(s1, team) || IsVisibleToTeam(s2, team))
                    {
                        if (t == 0 && (s2.SectorId == currentSectorId || s1.SectorId == currentSectorId))
                        {
                            SoundEffect.Play(ESounds.newtargetneutral, true);
                            soundPlayed = true;
                        }
                        w.SetVisibleToTeam(team, true);
                    }
                }
            }

            foreach (var s in Map.Sectors)
            {
                s.UpdateColours();
            }
        }
        
        public void ProcessGameEvent(object sender, EGameEventType e, ShipEventHandler f_ShipEvent)
        {

            if (e == EGameEventType.DroneBuilt)
            {
                var tech = sender as TechItem;
                if (tech == null) return;

                var b1 = AllBases.Where(_ => _.Team == tech.Team && _.Type == EBaseType.Starbase).LastOrDefault();
                if (b1 == null) return;
                Ship drone;

                var colour = Color.FromArgb(GameSettings.TeamColours[tech.Team - 1]);
                if (tech.Name == "Miner")
                {
                    drone = Ships.CreateMinerShip(tech.Team, colour, b1.SectorId);
                    if (drone == null) return;

                    if (tech.Team == 1)
                    {
                        OnGameEvent(new GameAlert(drone.SectorId, $"New miner launching in {Map.Sectors[drone.SectorId]}."), EGameEventType.ImportantMessage);
                        SoundEffect.Play(ESounds.vo_miner_report4duty);
                    }
                }
                else if (tech.Type == ETechType.ShipyardConstruction)
                {
                    b1 = AllBases.Where(_ => _.Team == tech.Team && _.Type == EBaseType.Shipyard).LastOrDefault();
                    if (b1 == null) return;

                    drone = Ships.CreateShip(tech.Name, tech.Team, colour, b1.SectorId);
                    if (drone == null) return;
                }
                else
                {
                    var bType = TechItem.GetBaseType(tech.Name);

                    drone = Ships.CreateBuilderShip(bType, tech.Team, colour, b1.SectorId);
                    if (drone == null) return;
                    var builder = drone as BuilderShip;
                    if (builder == null) return;

                    if (tech.Team == 1) PlayConstructorRequestSound(builder);
                }

                drone.CenterX = b1.CenterX;
                drone.CenterY = b1.CenterY;
                drone.ShipEvent += f_ShipEvent;
                drone.OrderShip(new MoveOrder(this, b1.SectorId, b1.GetNextBuildPosition(), Point.Empty));

                AddUnit(drone);
            }
            else if (e == EGameEventType.ResearchComplete)
            {
                var tech = sender as TechItem;
                if (tech == null) return;
                if (TechItem.IsGlobalUpgrade(tech.Name)) tech.ApplyGlobalUpgrade(TechTree[tech.Team - 1]);

                if (tech.Team == 1)
                {
                    if (tech.IsShipType())
                        SoundEffect.Play(ESounds.vo_sal_shiptech);
                    else
                        SoundEffect.Play(ESounds.vo_sal_researchcomplete);
                }
            }
        }

        public void PlayConstructorRequestSound(BuilderShip builder)
        {
            if (builder == null) return;

            _constructorCheckNext = ConstructorCheckDelay;
            string message;

            if (BaseSpecs.IsTower(builder.BaseType))
            {
                var sound = ESounds.vo_request_tower;
                if (builder.BaseType == EBaseType.Minefield) sound = ESounds.vo_request_minefield;

                message = $"{builder.BaseType} requesting location...";
                SoundEffect.Play(sound);
            }
            else
            {
                message = $"Constructor requesting {builder.TargetRockType} rock...";

                switch (builder.TargetRockType)
                {
                    case EAsteroidType.Resource:
                        SoundEffect.Play(ESounds.vo_request_builderhelium);
                        break;
                    case EAsteroidType.Generic:
                        SoundEffect.Play(ESounds.vo_request_buildergeneric);
                        break;
                    case EAsteroidType.Carbon:
                        SoundEffect.Play(ESounds.vo_request_buildercarbon);
                        break;
                    case EAsteroidType.Silicon:
                        SoundEffect.Play(ESounds.vo_request_buildersilicon);
                        break;
                    case EAsteroidType.Uranium:
                        SoundEffect.Play(ESounds.vo_request_builderuranium);
                        break;
                }
            }

            OnGameEvent(new GameAlert(builder.SectorId, message), EGameEventType.ImportantMessage);
        }

        public void ProcessShipEvent(Ship sender, EShipEventType e, ShipEventHandler f_shipEvent, BaseEventHandler b_baseEvent)
        {
            if (e == EShipEventType.ShipDestroyed)
            {
                if (sender.Type == EShipType.Miner) GameStats.TotalMinersDestroyed[sender.Team - 1]++;
                if (sender.Type == EShipType.Constructor) GameStats.TotalConstructorsDestroyed[sender.Team - 1]++;

                // Launch a Lifepod for each pilot
                var lifepods = new List<Ship>();
                for (var i = 0; i < sender.NumPilots; i++)
                {
                    var lifepod = Ships.CreateLifepod(sender.Team, sender.Colour, sender.SectorId);
                    lifepod.CenterX = sender.CenterX;
                    lifepod.CenterY = sender.CenterY;
                    lifepod.ShipEvent += f_shipEvent;
                    lifepods.Add(lifepod);
                }
                sender.NumPilots = 0;

                if (lifepods.Count > 1)
                {
                    SpreadOrderEvenly<MoveOrder>(this, lifepods, sender.SectorId, sender.CenterPoint);
                }
                lifepods.ForEach(_ => _.OrderShip(new PodDockOrder(this, _, true), true));

                AddUnits(lifepods);
            }
            else if (e == EShipEventType.BuildingStarted)
            {
                var b = sender as BuilderShip;
                if (b != null && BaseSpecs.IsTower(b.BaseType))
                {
                    if (b.BaseType == EBaseType.Minefield)
                    {
                        // Add a Minefield
                        var t = b.Team - 1;
                        var mineTech = TechTree[t].HasResearchedTech("Advanced Minefield") ? 1.5f : 1f;
                        Minefields.Add(new Minefield(b, Point.Empty, 100, 120 * 20 * mineTech, MinefieldImages[t], 1 * mineTech));
                    }
                    else
                    {
                        // Add a Tower
                        var type = (EShipType)Enum.Parse(typeof(EShipType), b.BaseType.ToString());
                        var tower = Ships.CreateTowerShip(type, b.Team, b.Colour, b.SectorId);
                        if (tower == null) return;

                        tower.CenterX = b.CenterX;
                        tower.CenterY = b.CenterY;
                        tower.ShipEvent += f_shipEvent;

                        AddUnit(tower);
                    }

                    b.Active = false;
                }
            }
            else if (e == EShipEventType.BuildingFinished)
            {
                var b = sender as BuilderShip;
                if (b != null)
                {
                    b.Target.BuildingComplete();
                    BuildableAsteroids.Remove(b.Target);
                    AllAsteroids.Remove(b.Target);
                    var r = b.Target as ResourceAsteroid;
                    if (r != null) ResourceAsteroids.Remove(r);

                    var newBase = b.GetFinishedBase();
                    newBase.BaseEvent += b_baseEvent;
                    AddBase(newBase);
                    GameStats.TotalBasesBuilt[sender.Team - 1]++;
                    UnlockTech(newBase.Type, newBase.Team);

                    if (newBase.Team == 1)
                    {
                        var secured = (newBase.CanLaunchShips() && !AllBases.Any(_ => _.Active && _.SectorId == sender.SectorId && _.CanLaunchShips()));
                        switch (newBase.Type)
                        {
                            case EBaseType.Outpost:
                                SoundEffect.Play(ESounds.vo_builder_outpost, true);
                                break;
                            case EBaseType.Resource:
                                SoundEffect.Play(ESounds.vo_builder_refinery, true);
                                break;
                            case EBaseType.Starbase:
                                SoundEffect.Play(ESounds.vo_builder_garrison, true);
                                break;
                            case EBaseType.Supremacy:
                                SoundEffect.Play(ESounds.vo_builder_supremecy, true);
                                break;
                            case EBaseType.Tactical:
                                SoundEffect.Play(ESounds.vo_builder_tactical, true);
                                break;
                            case EBaseType.Expansion:
                                SoundEffect.Play(ESounds.vo_builder_expansion, true);
                                break;
                            case EBaseType.Shipyard:
                                SoundEffect.Play(ESounds.vo_builder_shipyard, true);
                                break;
                        }

                        if (secured) SoundEffect.Play(ESounds.vo_sal_sectorsecured);
                    }
                }
            }
        }

        public void ProcessBaseEvent(Base sender, EBaseEventType e, int senderTeam)
        {
            if (e == EBaseEventType.BaseDestroyed)
            {
                if (sender.Team == 1 && !AllBases.Any(_ => _.Active && _.Team == 1 && _.SectorId == sender.SectorId && _.CanLaunchShips()))
                {
                    SoundEffect.Play(ESounds.vo_sal_sectorlost, true);
                }

                GameStats.TotalBasesDestroyed[sender.Team - 1]++;

                if (senderTeam == 1)
                {
                    switch (sender.Type)
                    {
                        case (EBaseType.Expansion):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_destroy_enemyexpansion : ESounds.vo_destroy_expansion, true);
                            break;

                        case (EBaseType.Supremacy):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_destroy_enemysupremecy : ESounds.vo_destroy_supremecy, true);
                            break;

                        case (EBaseType.Outpost):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_destroy_enemyoutpost : ESounds.vo_destroy_outpost, true);
                            break;

                        case (EBaseType.Starbase):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_destroy_enemygarrison : ESounds.vo_destroy_garrison, true);
                            break;

                        case (EBaseType.Tactical):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_destroy_enemytactical : ESounds.vo_destroy_tactical, true);
                            break;

                        case EBaseType.Resource:
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_destroy_enemyrefinery : ESounds.vo_destroy_refinery, true);
                            break;

                        case (EBaseType.Shipyard):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_destroy_enemyshipyard : ESounds.vo_destroy_shipyard, true);
                            break;
                    }
                }

                CheckForGameEnd();
            }
            else if (e == EBaseEventType.BaseCaptured)
            {
                if (senderTeam == 1)
                {
                    switch (sender.Type)
                    {
                        case (EBaseType.Expansion):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_capture_expansion : ESounds.vo_capture_enemyexpansion, true);
                            break;

                        case (EBaseType.Supremacy):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_capture_supremecy : ESounds.vo_capture_enemysupremecy, true);
                            break;

                        case (EBaseType.Outpost):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_capture_outpost : ESounds.vo_capture_enemyoutpost, true);
                            break;

                        case (EBaseType.Starbase):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_capture_garrison : ESounds.vo_capture_enemygarrison, true);
                            break;

                        case (EBaseType.Tactical):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_capture_tactical : ESounds.vo_capture_enemytactical, true);
                            break;

                        case (EBaseType.Shipyard):
                            SoundEffect.Play(sender.Team != 1 ? ESounds.vo_capture_shipyard : ESounds.vo_capture_enemyshipyard, true);
                            break;
                    }

                    if (sender.Team != 1)
                    {
                        if (!AllBases.Any(_ => _.Active && _.Team == 1 && _.SectorId == sender.SectorId && _.CanLaunchShips()))
                        {
                            SoundEffect.Play(ESounds.vo_sal_sectorlost, true);
                        }
                    }
                }

                CheckForGameEnd();
            }
        }

        private void CheckForGameEnd()
        {
            if (!AllBases.Any(_ => _.Team == 1 && _.Active && _.CanLaunchShips()))
            {
                OnGameEvent(null, EGameEventType.GameLost);
            }
            else if (!AllBases.Any(_ => _.Alliance != GameSettings.TeamAlliance[0] && _.Active && _.CanLaunchShips()))
            {
                OnGameEvent(null, EGameEventType.GameWon);
            }
        }

        public void AddUnit(Ship s)
        {
            lock (AllUnits)
            {
                AllUnits.Add(s);
            }
        }

        public void AddUnits(ICollection<Ship> s)
        {
            lock (AllUnits)
            {
                AllUnits.AddRange(s);
            }
        }

        public void AddBase(Base b)
        {
            lock (AllBases)
            {
                AllBases.Add(b);
            }
        }

        private bool IsVisibleToTeam(GameEntity s, int team)
        {
            var teamBases = AllBases.Where(_ => _.Active && _.Team == team && s.SectorId == _.SectorId).ToList();
            var closestBase = ClosestDistance(s.CenterX, s.CenterY, teamBases);
            if (closestBase != null)
            {
                var requiredD = (int)(closestBase.ScanRange * s.Signature);
                if (WithinDistance(s.CenterX, s.CenterY, closestBase.CenterX, closestBase.CenterY, requiredD))
                {
                    return true;
                }
            }

            var teamShips = AllUnits.Where(_ => _.Active && _.Team == team && s.SectorId == _.SectorId).ToList();
            var closestShip = ClosestDistance(s.CenterX, s.CenterY, teamShips);
            if (closestShip != null)
            {
                var requiredD = (int)(closestShip.ScanRange * s.Signature);
                if (WithinDistance(s.CenterX, s.CenterY, closestShip.CenterX, closestShip.CenterY, requiredD))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsVisibleToAlliance(GameEntity s, int alliance)
        {
            var teamBases = AllBases.Where(_ => _.Active && _.Alliance == alliance && s.SectorId == _.SectorId).ToList();
            var closestBase = ClosestDistance(s.CenterX, s.CenterY, teamBases);
            if (closestBase != null)
            {
                var requiredD = (int)(closestBase.ScanRange * s.Signature);
                if (WithinDistance(s.CenterX, s.CenterY, closestBase.CenterX, closestBase.CenterY, requiredD))
                {
                    return true;
                }
            }

            var teamShips = AllUnits.Where(_ => _.Active && _.Alliance == alliance && s.SectorId == _.SectorId).ToList();
            var closestShip = ClosestDistance(s.CenterX, s.CenterY, teamShips);
            if (closestShip != null)
            {
                var requiredD = (int)(closestShip.ScanRange * s.Signature);
                if (WithinDistance(s.CenterX, s.CenterY, closestShip.CenterX, closestShip.CenterY, requiredD))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddResources(int team, int resources, bool sound = true)
        {
            if (resources == 0) return;
            if (team == 1 && sound) SoundEffect.Play(ESounds.payday, true);

            var t = team - 1;
            var amount = (int)(resources * BaseConversionRate * GameSettings.ResourceConversionRateMultiplier * TechTree[t].ResearchedUpgrades[EGlobalUpgrade.MinerEfficiency] * Faction[t].Bonuses.MiningEfficiency);
            Credits[t] += amount;
            GameStats.TotalResourcesMined[t] += resources;
        }

        public int SpendCredits(int team, int amount)
        {
            var spentAmount = Math.Min(amount, Credits[team - 1]);

            Credits[team - 1] -= spentAmount;

            return spentAmount;
        }

        public void SetupGame(GameSettings settings)
        {
            GameSettings = settings;
            NumTeams = settings.NumTeams;
            GameStats = new GameStats(NumTeams);

            DockedPilots = new int[NumTeams];
            Credits = new int[NumTeams];
            Faction = new Faction[NumTeams];
            TechTree = new TechTree[NumTeams];
            TeamBrushes = new Brush[NumTeams];
            SelectedPens = new Pen[NumTeams];
            TextBrushes = new Brush[NumTeams];
            AICommanders = new BaseAI[NumTeams];
            MinefieldImages = new Image[NumTeams];

            for (var i = 0; i < NumTeams; i++)
            {
                Faction[i] = settings.TeamFactions[i];
                var c = Color.FromArgb(settings.TeamColours[i]);
                TeamBrushes[i] = new SolidBrush(c);
                SelectedPens[i] = new Pen(c, 1) { DashStyle = DashStyle.Dot };
                TextBrushes[i] = new SolidBrush(PerceivedBrightness(c) > 130 ? Color.Black : Color.White);
                
                MinefieldImages[i] = Image.FromFile(MineWeapon.MinefieldImage);

                Utils.ReplaceColour((Bitmap)MinefieldImages[i], c);
            }

            AllUnits.Clear();
            AllBases.Clear();
            AllAsteroids.Clear();
            ResourceAsteroids.Clear();
            BuildableAsteroids.Clear();
            Missiles.Clear();
            Minefields.Clear();
        }

        public void InitialiseGame(bool sound = true)
        {
            for (var t = 0; t < NumTeams; t++)
            {
                Credits[t] = 0;
                DockedPilots[t] = GameSettings.NumPilots;
                AddResources(t+1, (int)(ResourcesInitial * GameSettings.ResourcesStartingMultiplier), sound);
                Map.SetVisibilityToTeam(t+1, GameSettings.WormholesVisible);

                var faction = Faction[t];
                foreach (var tech in TechTree[t].TechItems)
                {
                    tech.Cost = (int)(tech.Cost * GameSettings.ResearchCostMultiplier * faction.Bonuses.ResearchCost);
                    tech.DurationTicks = (int)(tech.DurationTicks * GameSettings.ResearchTimeMultiplier * faction.Bonuses.ResearchTime);
                }
            }
        }

        public void LoadData()
        {
            Ships = ShipSpecs.LoadShipSpecs(this, ShipDataFile);
            Bases = BaseSpecs.LoadBaseSpecs(this, BaseDataFile);
            QuickChat = QuickComms.LoadQuickChat(QuickChatDataFile);

            for (var t = 0; t < NumTeams; t++)
            {
                TechTree[t] = Tech.TechTree.LoadTechTree(this, TechDataFile, t+1);

                var autoCompleted = TechTree[t].TechItems.Where(_ => _.Completed).ToList();
                foreach (var i in autoCompleted)
                {
                    i.Active = false;
                }

                if (GameSettings.GameType == EGameType.Campaign)
                {
                    var allowedIds = GameSettings.RestrictTechToIds[t];
                    if (allowedIds != null)
                    {
                        TechTree[t].TechItems.RemoveAll(_ => !allowedIds.Contains(_.Id));
                    }
                }
            }            
        }

        public bool CanLaunchShip(int team, int pilotsRequired, EShipType type)
        {
            if (type == EShipType.Constructor || type == EShipType.Tower) return false;

            return DockedPilots[team - 1] >= pilotsRequired;
        }

        public void LaunchShip(Ship ship)
        {
            var t = ship.Team - 1;
            if (DockedPilots[t] < ship.NumPilots) return;

            DockedPilots[t] -= ship.NumPilots;
            ship.Health = ship.MaxHealth;

            AddUnit(ship);

            OnGameEvent(ship, EGameEventType.ShipLaunched);
        }

        public void DockPilots(int team, int numPilots)
        {
            DockedPilots[team - 1] += numPilots;
        }

        public void OnGameEvent(object sender, EGameEventType type)
        {
            if (GameEvent != null) GameEvent(sender, type);
        }

        public void UnlockTech(EBaseType type, int team)
        {
            var item = (from t in TechTree[team - 1].TechItems
                        where t.Name == type.ToString()
                        && !t.Completed
                        select t).FirstOrDefault();
            if (item == null) return;

            item.Completed = true;
        }

        public void Tick()
        {
            for (var i = 0; i < AllUnits.Count; i++)
            {
                var u = AllUnits[i];
                u.Update();
            }

            for (var i = 0; i < AllBases.Count; i++)
            {
                var u = AllBases[i];
                u.Update();
            }

            foreach (var m in Missiles)
            {
                if (m.Target == null || !m.Target.Active)
                {
                    m.Target = GetRandomEnemyInRange(m.Team, m.Alliance, m.SectorId, m.Center, 200);
                }

                m.Update();
            }

            // Apply damage to all ships within enemy minefields
            foreach (var m in Minefields)
            {
                m.Update();

                var hits = AllUnits.FindAll(_ => _.Active && _.Type != EShipType.Lifepod && m.SectorId == _.SectorId && _.Alliance != m.Alliance && m.Bounds.Contains(_.Bounds));
                hits.ForEach(_ => _.Damage(m.Damage, m.Team));
            }

            AllUnits.RemoveAll(_ => !_.Active);
            AllBases.RemoveAll(_ => !_.Active);
            Missiles.RemoveAll(_ => !_.Active);
            Minefields.RemoveAll(_ => !_.Active);
        }

        private const int ConstructorCheckDelay = 120;
        private int _constructorCheckNext = ConstructorCheckDelay;
        
        public void SlowTick()
        {
            UpdateVisibility(false);

            ResourceAsteroids.ForEach(_ => _.Regenerate(1));

            for (var t = 0; t < NumTeams; t++)
            {
                var items = (from i in TechTree[t].TechItems
                             where !i.Completed
                             && i.AmountInvested > 0
                             select i).ToList();
                
                items.ForEach(_ => _.Update());

                var completedTech = TechTree[t].TechItems.Where(_ => _.Completed && _.Active).ToList();

                foreach (var c in completedTech)
                {
                    if (c.IsConstructionType())
                    {
                        c.Reset();
                        OnGameEvent(c, EGameEventType.DroneBuilt);
                    }
                    else
                    {
                        OnGameEvent(c, EGameEventType.ResearchComplete);
                        c.Active = false;
                    }
                }

                AddResources(t + 1, (int)(ResourceRegularAmount * GameSettings.ResourcesEachTickMultiplier), false);
                var ai = AICommanders[t];
                if (ai != null) ai.Update();
            }

            _constructorCheckNext--;
            if (_constructorCheckNext <= 0)
            {
                var constructorsWaiting = AllUnits.Where(_ => _.Team == 1 && _.Type == EShipType.Constructor && _.CurrentOrder == null && _.Orders.Count == 0).ToArray();
                if (constructorsWaiting.Length > 0)
                {
                    var con = constructorsWaiting[Random.Next(constructorsWaiting.Length)] as BuilderShip;
                    PlayConstructorRequestSound(con);
                }
            }            

        }

        public static string[] GetExplosionFrames()
        {
            var explosionFrames = new string[10];
            for (var i = 0; i < 10; i++)
            {
                explosionFrames[i] = $".\\Art\\Animations\\Explode\\bubble_explo{i + 1}.png";
            }

            return explosionFrames;
        }

        public void DrawMissiles(Graphics g, int currentSectorId)
        {
            foreach (var m in Missiles)
            {
                if (m.SectorId != currentSectorId) continue;

                m.Draw(g);
            }
        }

        public void DrawMinefields(Graphics g, int currentSectorId)
        {
            foreach (var m in Minefields)
            {
                if (!m.Active || m.SectorId != currentSectorId) continue;

                m.Draw(g, currentSectorId);
            }
        }

        public Ship GetRandomEnemyInRange(int team, int alliance, int sectorId, PointF pos, float range)
        {
            var enemysInRange = AllUnits.Where(_ => _.Active && _.Alliance != alliance && !_.Docked && _.SectorId == sectorId && _.VisibleToTeam[team - 1] && _.Type != EShipType.Lifepod && StrategyGame.WithinDistance(pos.X, pos.Y, _.CenterX, _.CenterY, range)).ToList();

            if (enemysInRange.Count > 1)
            {
                return enemysInRange[Random.Next(enemysInRange.Count)];
            }
            else if (enemysInRange.Count == 1)
            {
                return enemysInRange[0];
            }

            return null;
        }

        public int TotalCampaignPoints(int team)
        {
            var t = team - 1;
            var won = Winners.Contains(Faction[t]);
            var multiplier = won ? 1.5f : 1f;

            return (int)(multiplier * (GameStats.TotalResourcesMined[t] / 1000
                + GameStats.TotalBasesBuilt[t]
                + GameStats.TotalBasesDestroyed[t]
                + GameStats.TotalMinersBuilt[t]
                + GameStats.TotalMinersDestroyed[t]
                + GameStats.TotalConstructorsBuilt[t]
                + GameStats.TotalConstructorsDestroyed[t]));
        }
    }
}