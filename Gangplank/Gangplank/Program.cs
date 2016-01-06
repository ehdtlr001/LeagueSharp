using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

namespace Gangplank
{
    class Program
    {
        public static String Version = "1.0.0.0";
        private static String championName = "Gangplank";
        public static Obj_AI_Hero Player;
        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell Q, W, E, R;
        private const float ExplosionRange = 400;
        private const float LinkRange = 650;
        private static List<Bomb> LiveBarrels = new List<Bomb>();
        private static bool _qautoallowed = true;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void MenuIni()
        {
            _menu = new Menu("Gangplank", "gangplank.menu", true);

            var orbwalkerMenu = new Menu("Orbwalker", "gangplank.menu.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "gangplank.menu.targetselector");
            TargetSelector.AddToMenu(targetSelectorMenu);

            var comboMenu = new Menu("Combo", "gangplank.menu.combo");
            comboMenu.AddItem(new MenuItem("gangplank.menu.combo.q", "Use Q = ON"));
            comboMenu.AddItem(new MenuItem("gangplank.menu.combo.e", "Use E = ON"));
            comboMenu.AddItem(new MenuItem("gangplank.menu.combo.r", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("gangplank.menu.combo.rmin", "Minimum enemies to cast R").SetTooltip("Minimum enemies to hit with R in combo").SetValue(new Slider(2, 1, 5)));

            // Harass Menu
            var harassMenu = new Menu("Harass", "gangplank.menu.harass");
            harassMenu.AddItem(new MenuItem("gangplank.menu.harass.info", "Use your mixed key for harass"));
            harassMenu.AddItem(new MenuItem("gangplank.menu.harass.q", "Use Q").SetTooltip("If disabled, it won't block EQ usage").SetValue(true));
            harassMenu.AddItem(new MenuItem("gangplank.menu.harass.separator1", "Extended EQ:"));
            harassMenu.AddItem(new MenuItem("gangplank.menu.harass.extendedeq", "Enabled").SetValue(true));
            harassMenu.AddItem(new MenuItem("gangplank.menu.harass.instructioneq", "Place E near your pos, then wait it will automatically"));
            harassMenu.AddItem(new MenuItem("gangplank.menu.harass.instructionqe2", "place E in range of 1st barrel + Q to harass enemy"));
            harassMenu.AddItem(new MenuItem("gangplank.menu.harass.qmana", "Minimum mana to use Q harass").SetTooltip("Minimum mana for Q harass & Extended EQ").SetValue(new Slider(20)));

            // Farm Menu
            var farmMenu = new Menu("Farm", "gangplank.menu.farm");
            farmMenu.AddItem(new MenuItem("gangplank.menu.farm.qlh", "Use Q to lasthit").SetTooltip("Recommended On for bonus gold").SetValue(true));
            farmMenu.AddItem(new MenuItem("gangplank.menu.farm.qlhmana", "Minimum mana for Q lasthit").SetValue(new Slider(10)));
            farmMenu.AddItem(new MenuItem("gangplank.menu.farm.ewc", "Use E to Laneclear & Jungle").SetValue(true));
            farmMenu.AddItem(new MenuItem("gangplank.menu.farm.eminwc", "Minimum minions to use E").SetTooltip("If jungle mobs, it won't block E usage under value").SetValue(new Slider(5, 1, 15)));
            farmMenu.AddItem(new MenuItem("gangplank.menu.farm.qewc", "Use Q on E to clear").SetTooltip("Recommended On for bonus gold").SetValue(true));
            farmMenu.AddItem(new MenuItem("gangplank.menu.farm.qewcmana", "Minimum mana to use Q on E").SetValue(new Slider(10)));

            // Misc Menu
            var miscMenu = new Menu("Misc", "gangplank.menu.misc");
            // Barrel Manager Options
            var barrelManagerMenu = new Menu("Barrel Manager", "gangplank.menu.misc.barrelmanager");
            barrelManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.barrelmanager.edisabled", "Block E usage").SetTooltip("If on, won't use E").SetValue(false));
            barrelManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.barrelmanager.stacks", "Number of stacks to keep").SetTooltip("If Set to 0, it won't keep any stacks; Stacks are used in combo/harass").SetValue(new Slider(1, 0, 4)));
            barrelManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.barrelmanager.autoboom", "Auto explode when enemy in explosion range").SetTooltip("Will auto Q on barrels that are near enemies").SetValue(true));

            // Cleanser W Manager Menu
            var cleanserManagerMenu = new Menu("W cleanser", "gangplank.menu.misc.cleansermanager");
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.enabled", "Enabled").SetValue(true));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.separation1", ""));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.separation2", "Buff Types: "));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.charm", "Charm").SetValue(true));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.flee", "Flee").SetTooltip("Fear").SetValue(true));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.polymorph", "Polymorph").SetValue(true));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.snare", "Snare").SetValue(true));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.stun", "Stun").SetValue(true));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.taunt", "Taunt").SetValue(true));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.exhaust", "Exhaust").SetTooltip("Will only remove Slow").SetValue(false));
            cleanserManagerMenu.AddItem(new MenuItem("gangplank.menu.misc.cleansermanager.suppression", "Supression").SetValue(true));

            miscMenu.AddItem(new MenuItem("gangplank.menu.misc.wheal", "Use W to heal").SetTooltip("Enable auto W heal(won't cancel recall if low)").SetValue(true));
            miscMenu.AddItem(new MenuItem("gangplank.menu.misc.healmin", "Health %").SetTooltip("If under, will use W").SetValue(new Slider(30)));
            miscMenu.AddItem(new MenuItem("gangplank.menu.misc.healminmana", "Minimum Mana %").SetTooltip("Minimum mana to use W heal").SetValue(new Slider(35)));
            miscMenu.AddItem(new MenuItem("gangplank.menu.misc.ks", "KillSteal").SetTooltip("If off, won't try to KS").SetValue(true));
            miscMenu.AddItem(new MenuItem("gangplank.menu.misc.qks", "Use Q to KillSteal").SetTooltip("If on, will auto Q to KS").SetValue(true));
            miscMenu.AddItem(new MenuItem("gangplank.menu.misc.rks", "Use R to KillSteal").SetTooltip("If on, will try to KS on the whole map").SetValue(false));
            miscMenu.AddItem(new MenuItem("gangplank.menu.misc.rksoffinfo", "Ks Notification").SetTooltip("Use it if you want to manually ks, it will show a notification when killable").SetValue(true));

            // Drawing Menu
            Menu drawingMenu = new Menu("Drawing", "gangplank.menu.drawing");
            drawingMenu.AddItem(new MenuItem("gangplank.menu.drawing.enabled", "Enabled").SetTooltip("If off, will block gangplank drawings").SetValue(true));
            drawingMenu.AddItem(new MenuItem("gangplank.menu.drawing.q", "Draw Q range").SetValue(true));
            drawingMenu.AddItem(new MenuItem("gangplank.menu.drawing.e", "Draw E range").SetValue(true));
            drawingMenu.AddItem(new MenuItem("gangplank.menu.drawing.ehelper", "Draw manual E indicator").SetTooltip("Draw E connection range").SetValue(false));

            _menu.AddSubMenu(orbwalkerMenu);
            _menu.AddSubMenu(targetSelectorMenu);
            _menu.AddSubMenu(comboMenu);
            _menu.AddSubMenu(harassMenu);
            _menu.AddSubMenu(farmMenu);
            _menu.AddSubMenu(miscMenu);
            miscMenu.AddSubMenu(barrelManagerMenu);
            miscMenu.AddSubMenu(cleanserManagerMenu);
            _menu.AddSubMenu(drawingMenu);
            _menu.AddToMainMenu();
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != championName)
            {
                return;
            }
            MenuIni();

            Player = ObjectManager.Player;
            // Spells ranges
            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0.25f, 2150f);
            E.SetSkillshot(0.5f, 40, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.9f, 100, float.MaxValue, false, SkillshotType.SkillshotCircle);
            Game.OnUpdate += Logic;
            Drawing.OnDraw += Draw;
            GameObject.OnCreate += GameObjCreate;
            GameObject.OnDelete += GameObjDelete;
        }

        private static void GameObjCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Barrel")
            {
                LiveBarrels.Add(new Bomb(sender as Obj_AI_Minion));
            }
        }

        private static void GameObjDelete(GameObject sender, EventArgs args)
        {
            for (int i = 0; i < LiveBarrels.Count; i++)
            {
                if (LiveBarrels[i].BombObj.NetworkId == sender.NetworkId)
                {
                    LiveBarrels.RemoveAt(i);
                    return;
                }
            }
        }

        // Draw Manager
        static void Draw(EventArgs args)
        {
            if (GetBool("gangplank.menu.drawing.enabled") == false)
            {
                return;
            }
            if (GetBool("gangplank.menu.drawing.q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range,
                    Q.IsReady() ? Color.FromArgb(38, 126, 188) : Color.Black);
            }
            if (GetBool("gangplank.menu.drawing.e") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range,
                    E.IsReady() ? Color.BlueViolet : Color.Black);
            }
            if (GetBool("gangplank.menu.drawing.ehelper"))
            {
                Render.Circle.DrawCircle(Game.CursorPos, LinkRange / 2 + 10, Color.FromArgb(125, 125, 125));
            }
        }

        // Orbwalker Manager
        static void Logic(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            var activeOrbwalker = _orbwalker.ActiveMode;
            switch (activeOrbwalker)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    _qautoallowed = false;
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    WaveClear();
                    _qautoallowed = true;
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Mixed();
                    _qautoallowed = false;
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    _qautoallowed = true;
                    break;
                case Orbwalking.OrbwalkingMode.None:
                    _qautoallowed = true;
                    // flee maybe inc bogue
                    break;
            }
            if (GetBool("gangplank.menu.misc.cleansermanager.enabled"))
            {
                CleanserManager();
            }
            if (GetBool("gangplank.menu.misc.wheal"))
            {
                HealManager();
            }
            if (GetBool("gangplank.menu.misc.ks"))
            {
                KillSteal();
            }
            if (GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false && GetBool("gangplank.menu.misc.barrelmanager.autoboom") && _qautoallowed)
            {
                BarrelManager();
            }
        }

        // TODO rework the logic of combo, especially if already barrel manually placed
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true, HeroManager.Enemies.Where(e => e.IsInvulnerable));
            var ePrediction = Prediction.GetPrediction(target, 1f).CastPosition;
            var e2Prediction = Prediction.GetPrediction(target, 1f).CastPosition + 350;
            var nbar = NearestBomb(Player.ServerPosition.To2D());

            if (target == null) return;
            if ((E.Instance.Ammo == 0 || E.Level < 1) && Q.IsReady() && Q.IsInRange(target) && (LiveBarrels.Count == 0 || NearestBomb(Player.Position.To2D()).BombObj.Distance(Player) > Q.Range))
            {
                Q.CastOnUnit(target);
            }

            if (GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false && R.Level == 0 && E.IsReady() && (LiveBarrels.Count == 0 || NearestBomb(Player.Position.To2D()).BombObj.Distance(Player) > E.Range)) // 2 Bomb
            {
                BarrelLinkManager();
            }/*
            if (R.Level == 1 && GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false && E.IsReady()) // 3 Bomb
            {
                if ((LiveBarrels.Count == 0 || nbar.BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo >= 3)
                {
                    E.Cast(Player.ServerPosition);
                }
                if ((LiveBarrels.Count == 0 || nbar.BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo < 3)
                {
                    foreach (var k in LiveBarrels)
                    {
                        if (k.BombObj.GetEnemiesInRange(ExplosionRange).Count >= 1 && Player.Distance(k.BombObj) < E.Range)
                        {
                            BarrelManager();
                            return;

                        }

                    }
                    E.Cast(ePrediction);
                }
            }
            if (R.Level == 2 && GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false && E.IsReady()) // 4 Bomb
            {
                if ((LiveBarrels.Count == 0 || nbar.BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo >= 3)
                {
                    E.Cast(Player.ServerPosition);
                }
                if ((LiveBarrels.Count == 0 || nbar.BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo < 3)
                {
                    foreach (var k in LiveBarrels)
                    {
                        if (k.BombObj.GetEnemiesInRange(ExplosionRange).Count >= 1 && Player.Distance(k.BombObj) < E.Range)
                        {
                            BarrelManager();
                            return;
                        }
                    }
                    E.Cast(ePrediction);
                }
            }
            if (R.Level == 3 && GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false && E.IsReady()) // 5 Bomb
            {
                if ((LiveBarrels.Count == 0 || nbar.BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo >= 3 && Player.GetEnemiesInRange(E.Range).Count < 3)
                {
                    E.Cast(Player.ServerPosition);
                }
                if (((LiveBarrels.Count == 0 || nbar.BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo < 3) || Player.GetEnemiesInRange(E.Range).Count >= 3)
                {
                    foreach (var k in LiveBarrels)
                    {
                        if (k.BombObj.GetEnemiesInRange(ExplosionRange).Count >= 1 && Player.Distance(k.BombObj) < E.Range)
                        {
                            BarrelManager();
                            return;
                        }
                    }
                    E.Cast(ePrediction);
                }
            }*/     
            //Extend if possible and if the number of enemies is below 3
            if (Player.GetEnemiesInRange(E.Range).Count < 3 && GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false)
            {
                if (Player.ServerPosition.Distance(nbar.BombObj.Position) < Q.Range && nbar.BombObj.Health < 3)
                {
                    if (target != null)
                    {
                        var prediction = Prediction.GetPrediction(target, 0.8f).CastPosition;
                        if (nbar.BombObj.Distance(prediction) < LinkRange)
                        {
                            E.Cast(prediction);
                            // if (Player.Level < 7 && nbar.BombObj.Health < 2)
                            // {
                            //    Q.Cast(nbar.BombObj);
                            // }
                            if (Player.Level < 13 && Player.Level >= 7 && nbar.BombObj.Health == 2)
                            {
                                Utility.DelayAction.Add(580 - Game.Ping, () =>
                                {
                                    Q.Cast(nbar.BombObj);
                                }
                                   );
                            }

                            if (Player.Level >= 13 && nbar.BombObj.Health == 2)
                            {
                                Utility.DelayAction.Add((int)(80 - Game.Ping), () =>
                                {
                                    Q.Cast(nbar.BombObj);
                                }
                                    );
                            }
                            if (nbar.BombObj.Health == 1)
                            {
                                Q.Cast(nbar.BombObj);

                            }
                        }
                    }
                }
            }

            if (GetBool("gangplank.menu.combo.r") && R.IsReady() && target.GetEnemiesInRange(600).Count + 1 > Getslider("gangplank.menu.combo.rmin") && target.HealthPercent < 30)
            {
                R.Cast(Prediction.GetPrediction(target, R.Delay).CastPosition);
            }
            BarrelManager();
        }

        private static void WaveClear()
        {
            var minions = MinionManager.GetMinions(Q.Range).Where(m => m.Health > 3).ToList();
            var jungleMobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral).Where(j => j.Health > 3).ToList();
            minions.AddRange(jungleMobs);

            if (GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false && GetBool("gangplank.menu.farm.ewc") && E.IsReady())
            {
                var posE = E.GetCircularFarmLocation(minions, ExplosionRange);
                if (posE.MinionsHit >= Getslider("gangplank.menu.farm.eminwc") && (LiveBarrels.Count == 0 || NearestBomb(Player.ServerPosition.To2D()).BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo > Getslider("gangplank.menu.misc.barrelmanager.stacks"))
                {
                    E.Cast(posE.Position);
                }
                // Jungle
                if (jungleMobs.Count >= 1)
                {
                    if (GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false &&
                        GetBool("gangplank.menu.farm.ewc") && E.IsReady() && (LiveBarrels.Count == 0 || NearestBomb(Player.ServerPosition.To2D()).BombObj.Distance(Player) > Q.Range) && E.Instance.Ammo > Getslider("gangplank.menu.misc.barrelmanager.stacks"))
                    {
                        E.Cast(jungleMobs.FirstOrDefault().Position);
                    }
                }
            }
            if (Q.IsReady() && jungleMobs.Any() && Player.ManaPercent > Getslider("gangplank.menu.farm.qlhmana") && GetBool("gangplank.menu.farm.qlh"))
            {
                Q.CastOnUnit(jungleMobs.FirstOrDefault(j => j.Health < Player.GetSpellDamage(j, SpellSlot.Q)));
            }
            if ((GetBool("gangplank.menu.farm.qlh") && minions.Any() && Player.ManaPercent > Getslider("gangplank.menu.farm.qlhmana") && Q.IsReady()) && (E.Instance.Ammo <= Getslider("gangplank.menu.misc.barrelmanager.stacks") || E.Level < 1))
            {
                Q.CastOnUnit(minions.FirstOrDefault(m => m.Health < Player.GetSpellDamage(m, SpellSlot.Q)));
            }
            if (LiveBarrels.Any() || NearestBomb(Player.ServerPosition.To2D()).BombObj.Distance(Player) < Q.Range + 150)
            {
                var lol =
                    MinionManager.GetMinions(NearestBomb(Player.ServerPosition.To2D()).BombObj.Position, ExplosionRange, MinionTypes.All, MinionTeam.All)
                        .Where(m => m.Health < Player.GetSpellDamage(m, SpellSlot.Q))
                        .ToList();

                if (GetBool("gangplank.menu.farm.qewc") &&
                    Player.ManaPercent > Getslider("gangplank.menu.farm.qewcmana") &&
                    Q.IsReady() &&
                    Q.IsInRange(NearestBomb(Player.ServerPosition.To2D()).BombObj) &&
                    NearestBomb(Player.ServerPosition.To2D()).BombObj.Health < 2 &&
                    ((Q.Level >= 3 && minions.Count > 3 && lol.Count > 3) || (Q.Level == 2 && minions.Count > 2 && lol.Count >= 2) || (Q.Level == 1 && minions.Count >= 2 && lol.Any()) || (minions.Count <= 2 && lol.Any())))
                {
                    Q.Cast(NearestBomb(Player.ServerPosition.To2D()).BombObj);
                }
                if (!Q.IsReady() &&
                    Player.ServerPosition.Distance(NearestBomb(Player.ServerPosition.To2D()).BombObj.Position) <
                    Player.AttackRange &&
                    NearestBomb(Player.ServerPosition.To2D()).BombObj.IsTargetable &&
                    NearestBomb(Player.ServerPosition.To2D()).BombObj.Health < 2 &&
                    NearestBomb(Player.ServerPosition.To2D()).BombObj.IsValidTarget())
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, NearestBomb(Player.ServerPosition.To2D()).BombObj);
                }
            }

        }


        private static void Mixed()
        {

            // harass
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            // Q lasthit minions
            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            Bomb nbar = NearestBomb(Player.ServerPosition.To2D());

            if (GetBool("gangplank.menu.farm.qlh") && Q.IsReady() && Player.ManaPercent >= Getslider("gangplank.menu.farm.qlhmana") && target == null)
            {
                if (minions != null)
                {
                    foreach (var m in minions)
                    {
                        if (m != null)
                        {
                            if (m.Health <= Player.GetSpellDamage(m, SpellSlot.Q))
                            {
                                Q.CastOnUnit(m);
                            }
                        }
                    }
                }
            }
            // Q
            if (GetBool("gangplank.menu.harass.q") && Q.IsReady() && Player.ManaPercent >= Getslider("gangplank.menu.harass.qmana") && TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical) != null

                )
            {
                if (LiveBarrels.Count == 0) Q.Cast(TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical));
                if (LiveBarrels.Count >= 1 && nbar.BombObj.Distance(Player) > E.Range) Q.Cast(TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical));
            }


            // Extended EQ, done but still some bugs remaining, going to fix them #TODO
            if (Q.IsReady() && E.IsReady() && GetBool("gangplank.menu.harass.extendedeq") && GetBool("gangplank.menu.misc.barrelmanager.edisabled") == false && Player.ManaPercent >= Getslider("gangplank.menu.harass.qmana"))
            {
                if (!LiveBarrels.Any()) return;


                if (Player.ServerPosition.Distance(nbar.BombObj.Position) < Q.Range && nbar.BombObj.Health < 3)
                {
                    if (target != null)
                    {
                        {
                            var prediction = Prediction.GetPrediction(target, 0.8f).CastPosition;
                            if (nbar.BombObj.Distance(prediction) < LinkRange)
                            {
                                E.Cast(prediction);

                                if (Player.Level < 13 && Player.Level >= 7 && nbar.BombObj.Health == 2)
                                {
                                    Utility.DelayAction.Add((int)(580 - Game.Ping), () =>
                                    {
                                        Q.Cast(nbar.BombObj);
                                    }
                                       );
                                }

                                if (Player.Level >= 13 && nbar.BombObj.Health == 2)
                                {
                                    Utility.DelayAction.Add((int)(80 - Game.Ping), () =>
                                    {
                                        Q.Cast(nbar.BombObj);
                                    }
                                        );
                                }
                                if (nbar.BombObj.Health == 1)
                                {
                                    Q.Cast(nbar.BombObj);
                                }
                            }
                        }
                    }
                }
            }
            BarrelManager();
        }

        private static void LastHit()
        {
            // LH Logic
            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

            // Q Last Hit
            if (GetBool("gangplank.menu.farm.qlh") && Q.IsReady() && Player.ManaPercent >= Getslider("gangplank.menu.farm.qlhmana"))
            {
                if (minions != null)
                {
                    foreach (var m in minions)
                    {
                        if (m != null)
                        {
                            if (m.Health <= Player.GetSpellDamage(m, SpellSlot.Q))
                            {
                                Q.CastOnUnit(m);
                            }
                        }
                    }
                }
            }
        }

        // W heal
        private static void HealManager()
        {
            if (Player.InFountain()) return;
            if (Player.IsRecalling()) return;
            if (Player.InShop()) return;
            if (W.IsReady() && Player.HealthPercent <= Getslider("gangplank.menu.misc.healmin") &&
                Player.ManaPercent >= Getslider("gangplank.menu.misc.healminmana"))
            {
                Utility.DelayAction.Add(100 + Game.Ping, () =>
                {
                    W.Cast();
                }
                );
            }


        }

        private static void CleanserManager()
        {
            // List of disable buffs
            if
                (W.IsReady() && (
                (Player.HasBuffOfType(BuffType.Charm) && GetBool("gangplank.menu.misc.cleansermanager.charm"))
                || (Player.HasBuffOfType(BuffType.Flee) && GetBool("gangplank.menu.misc.cleansermanager.flee"))
                || (Player.HasBuffOfType(BuffType.Polymorph) && GetBool("gangplank.menu.misc.cleansermanager.polymorph"))
                || (Player.HasBuffOfType(BuffType.Snare) && GetBool("gangplank.menu.misc.cleansermanager.snare"))
                || (Player.HasBuffOfType(BuffType.Stun) && GetBool("gangplank.menu.misc.cleansermanager.stun"))
                || (Player.HasBuffOfType(BuffType.Taunt) && GetBool("gangplank.menu.misc.cleansermanager.taunt"))
                || (Player.HasBuff("summonerexhaust") && GetBool("gangplank.menu.misc.cleansermanager.exhaust"))
                || (Player.HasBuffOfType(BuffType.Suppression) && GetBool("gangplank.menu.misc.cleansermanager.suppression"))
                ))
            {
                W.Cast();
            }
        }

        // Ks "logic" kappa
        private static void KillSteal()
        {
            var kstarget = HeroManager.Enemies;
            if (GetBool("gangplank.menu.misc.qks") && Q.IsReady())
            {
                if (kstarget != null)
                {
                    foreach (var ks in kstarget)
                    {
                        if (ks != null)
                        {
                            if (ks.Health <= Player.GetSpellDamage(ks, SpellSlot.Q) && ks.Health > 0 && Q.IsInRange(ks))
                            {

                                Q.CastOnUnit(ks);
                            }
                        }
                    }
                }
            }
            if (GetBool("gangplank.menu.misc.rks") && R.IsReady())
            {
                if (kstarget != null)
                    foreach (var ks in kstarget)
                    {
                        if (ks != null)
                        {
                            // Prevent overkill
                            if (ks.Health <= Player.GetSpellDamage(ks, SpellSlot.Q) && Q.IsInRange(ks)) return;

                            if (ks.Health <= Player.GetSpellDamage(ks, SpellSlot.R) * 7 && ks.Health > 0)
                            {
                                var ksposition = Prediction.GetPrediction(ks, R.Delay).CastPosition;
                                if (ksposition.IsValid())
                                {
                                    R.Cast(ksposition);
                                }
                            }
                        }
                    }
            }

        }

        // auto barrel activator
        private static void BarrelManager()
        {
            if (LiveBarrels.Count == 0) return;
            foreach (var k in LiveBarrels)
            {
                if (Q.IsReady() && Q.IsInRange(k.BombObj) && k.BombObj.GetEnemiesInRange(ExplosionRange).Count > 0 && k.BombObj.Health < 2)
                    Q.Cast(k.BombObj);
                if (Player.Distance(k.BombObj) <= Player.AttackRange &&
                    k.BombObj.GetEnemiesInRange(ExplosionRange).Count > 0 && k.BombObj.Health < 2 &&
                    k.BombObj.IsValidTarget() &&
                    k.BombObj.IsTargetable)
                    Player.IssueOrder(GameObjectOrder.AttackUnit, k.BombObj);
            }
        }

        private static void BarrelLinkManager()
        {
            if (E.Instance.Ammo == 0 || E.Level < 1) return;

            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true, HeroManager.Enemies.Where(e => e.IsInvulnerable));
            var nbar = NearestBomb(Player.ServerPosition.To2D());
                        
            if (E.IsReady() && NearestBomb(target.Position.To2D()).BombObj.Distance(target) > ExplosionRange && (LiveBarrels.Count == 0 || NearestBomb(Player.Position.To2D()).BombObj.Distance(Player) > E.Range))
            {
                E.Cast(Prediction.GetPrediction(target, 1f).CastPosition);
            }
            if (E.IsReady() && NearestBomb(target.Position.To2D()).BombObj.Distance(target) < ExplosionRange && (LiveBarrels.Count == 0 || NearestBomb(Player.Position.To2D()).BombObj.Distance(Player) > E.Range))
            {
                E.Cast(Prediction.GetPrediction(target, 20f).CastPosition);
            }
        }

        private static Bomb NearestBomb(Vector2 pos)
        {
            if (LiveBarrels.Count == 0)
            {
                return null;
            }
            return LiveBarrels.OrderBy(k => k.BombObj.ServerPosition.Distance(pos.To3D())).FirstOrDefault(k => !k.BombObj.IsDead);
        }
        // Get Values code
        private static bool GetBool(string name)
        {
            return _menu.Item(name).GetValue<bool>();
        }
        private static int Getslider(string itemname)
        {
            return _menu.Item(itemname).GetValue<Slider>().Value;
        }
    }

    internal class Bomb
    {
        public Obj_AI_Minion BombObj;
        public Bomb(Obj_AI_Minion Bomb)
        {
            BombObj = Bomb;
        }
    }
}
