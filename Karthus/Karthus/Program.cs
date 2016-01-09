using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Karthus
{
    class Program
    {
        public static Check Check;
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static Spell Q = new Spell(SpellSlot.Q, 875f, TargetSelector.DamageType.Magical);
        private static Spell W = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Magical);
        private static Spell E = new Spell(SpellSlot.E, 425f, TargetSelector.DamageType.Magical);
        private static Spell R = new Spell(SpellSlot.R, float.MaxValue, TargetSelector.DamageType.Magical);
        private static Spell _Ignite = new Spell(SpellSlot.Unknown, 600);

        private static Menu MenuIni;
        private static Orbwalking.Orbwalker orbwalker;
        private static Obj_AI_Hero QTarget;
        private static Obj_AI_Hero WTarget;
        private static Obj_AI_Hero ETarget;
        private static bool NowE = false;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Karthus") return;

            Check = new Check();

            Q.SetSkillshot(1f, 160f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(.5f, 70f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(1f, 505f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(3f, float.MaxValue, float.MaxValue, false, SkillshotType.SkillshotCircle);

            MenuIni = new Menu("SN Karthus", "SN Karthus", true);
            MenuIni.AddToMainMenu();

            Menu orbwalkerMenu = new Menu("OrbWalker", "OrbWalker");
            orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            MenuIni.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            MenuIni.AddSubMenu(targetSelectorMenu);

            var Combo = new Menu("Combo", "Combo");
            Combo.AddItem(new MenuItem("CUse_Q", "CUse_Q").SetValue(true));
            Combo.AddItem(new MenuItem("CUse_W", "CUse_W").SetValue(true));
            Combo.AddItem(new MenuItem("CUse_E", "CUse_E").SetValue(true));
            Combo.AddItem(new MenuItem("CUse_AA", "CUse_AA").SetValue(true));
            Combo.AddItem(new MenuItem("CEPercent", "Use E Mana %").SetValue(new Slider(30)));
            MenuIni.AddSubMenu(Combo);

            var Harass = new Menu("Harass", "Harass");
            Harass.AddItem(new MenuItem("HUse_Q", "HUse_Q").SetValue(true));
            MenuIni.AddSubMenu(Harass);

            var Farm = new Menu("Farm", "Farm");
            Farm.AddItem(new MenuItem("FUse_Q", "FUse_Q").SetValue(true));
            //Farm.AddItem(new MenuItem("Q_to_One", "Q_to_One").SetTooltip("Q use only one minion").SetValue(true));
            Farm.AddItem(new MenuItem("FUse_E", "FUse_E").SetValue(true));
            Farm.AddItem(new MenuItem("FEPercent", "Use E Mana %").SetValue(new Slider(15)));
            MenuIni.AddSubMenu(Farm);

            var Misc = new Menu("Misc", "Misc");
            Misc.AddItem(new MenuItem("NotifyUlt", "Ult_notify_text").SetValue(true));
            Misc.AddItem(new MenuItem("DeadCast", "DeadCast").SetValue(true));
            Misc.AddItem(new MenuItem("Ignite", "Ignite").SetValue(true));
            MenuIni.AddSubMenu(Misc);

            var Draw = new Menu("Draw", "Draw");
            Draw.AddItem(new MenuItem("Enabled", "Enabled").SetValue(true));
            Draw.AddItem(new MenuItem("Draw_Q", "Draw_Q").SetValue(new Circle(true, Color.Green)));
            MenuIni.AddSubMenu(Draw);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            QTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            WTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            ETarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            var Ignite = Player.Spellbook.Spells.FirstOrDefault(spell => spell.Name == "summonerdot");
            if (Ignite != null && MenuIni.SubMenu("Misc").Item("Ignite").GetValue<bool>())
                _Ignite.Slot = Ignite.Slot;
            else
                _Ignite.Slot = SpellSlot.Unknown;

            var activeOrbwalker = orbwalker.ActiveMode;
            switch (activeOrbwalker)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    orbwalker.SetAttack(MenuIni.SubMenu("Combo").Item("CUse_AA").GetValue<bool>() || Player.Mana < Q.Instance.ManaCost * 3);
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    orbwalker.SetAttack(true);
                    Farm();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    orbwalker.SetAttack(true);
                    Harass();
                    break;
                default:
                    orbwalker.SetAttack(true);
                    calcE();

                    if (MenuIni.SubMenu("Misc").Item("DeadCast").GetValue<bool>())
                        if (Player.IsZombie)
                            if (!Combo())
                                Farm(true);
                    break;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if(!Player.IsDead)
            {
                if (MenuIni.SubMenu("Draw").Item("Draw_Q").GetValue<Circle>().Active)
                {
                    Render.Circle.DrawCircle(Player.Position, Q.Range, MenuIni.SubMenu("Draw").Item("Draw_Q").GetValue<Circle>().Color);
                }
            }

            if(Player.Spellbook.GetSpell(SpellSlot.R).Level > 0)
            {
                var killable = "";

                var time = Utils.TickCount;

                foreach (TI target in Program.Check.TI.Where(x => x.Player.IsValid && !x.Player.IsDead && x.Player.IsEnemy && (Program.Check.recalltc(x) || (x.Player.IsVisible && Utility.IsValidTarget(x.Player))) && Player.GetSpellDamage(x.Player, SpellSlot.R) >= Program.Check.GetTargetHealth(x, (int)(R.Delay * 1000f))))
                {
                    killable += target.Player.ChampionName + " ";
                }

                if (killable != "" && MenuIni.SubMenu("Misc").Item("NotifyUlt").GetValue<bool>())
                {
                    Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.7f, System.Drawing.Color.Red, "Killable by ult: " + killable);
                }
            }
        }

        private static Vector3 PredPos(Obj_AI_Hero Hero, float Delay)
        {
            float value = 0f;
            if (Hero.IsFacing(Player))
            {
                value = (50f - Hero.BoundingRadius);
            }
            else
            {
                value = -(100f - Hero.BoundingRadius);
            }
            var distance = Delay * Hero.MoveSpeed + value;
            var path = Hero.GetWaypoints();

            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var d = a.Distance(b);

                if (d < distance)
                {
                    distance -= d;
                }
                else
                {
                    return (a + distance * (b - a).Normalized()).To3D();
                }
            }
            return (path[path.Count - 1]).To3D();
        }

        private static void calcE(bool TC = false)
        {
            if (!E.IsReady() || Player.IsZombie || Player.Spellbook.GetSpell(SpellSlot.E).ToggleState != 2) return;

            var minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (!TC && (ETarget != null || (!NowE && minions.Count != 0)))
                return;

            E.Cast();
            NowE = false;
        }

        private static void Harass()
        {
            if (MenuIni.SubMenu("Harass").Item("HUse_Q").GetValue<bool>())
                if (Q.IsReady() && QTarget.IsValidTarget(Q.Range))
                    Q.Cast(PredPos(QTarget, 0.6f));
        }

        private static bool Combo()
        {
            bool Qtarget = false;

            var Qm = MenuIni.SubMenu("Combo").Item("CUse_Q").GetValue<bool>();
            var Wm = MenuIni.SubMenu("Combo").Item("CUse_W").GetValue<bool>();
            var Em = MenuIni.SubMenu("Combo").Item("CUse_E").GetValue<bool>();

            if (QTarget == null && WTarget == null && ETarget == null)
                return false;

            if (Player.Distance(QTarget.Position) < _Ignite.Range)
            {
                var Igd = Damage.GetSummonerSpellDamage(Player, QTarget, Damage.SummonerSpell.Ignite);
                if (Igd > QTarget.Health)
                    _Ignite.CastOnUnit(QTarget);
            }

            if (Wm && W.IsReady() && WTarget.IsValid)
                W.Cast(PredPos(WTarget, 0.2f));

            if (Em && E.IsReady() && !Player.IsZombie)
            {
                if (ETarget != null)
                {
                    if (Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 1)
                    {
                        if (Player.Distance(ETarget.ServerPosition) <= E.Range && (((Player.Mana / Player.MaxMana) * 100f) >= MenuIni.SubMenu("Combo").Item("CEPercent").GetValue<Slider>().Value))
                        {
                            NowE = true;
                            E.Cast();
                        }
                    }
                    else if (Player.Distance(ETarget.ServerPosition) >= E.Range || (((Player.Mana / Player.MaxMana) * 100f) <= MenuIni.SubMenu("Combo").Item("CEPercent").GetValue<Slider>().Value))
                    {
                        calcE(true);
                    }
                }
                else
                    calcE();
            }

            if (Qm && Q.IsReady() && QTarget.IsValid)
            {
                if (QTarget != null)
                {
                    Qtarget = true;
                    Q.Cast(PredPos(QTarget, 0.6f));
                }
            }

            return Qtarget;
        }

        private static void Farm(bool Can = false)
        {
            var canQ = Can || MenuIni.SubMenu("Farm").Item("FUse_Q").GetValue<bool>();
            var canE = Can || MenuIni.SubMenu("Farm").Item("FUse_E").GetValue<bool>();
            //bool QtoOne = MenuIni.SubMenu("Farm").Item("Q_to_One").GetValue<bool>();
            bool jgm;
            List<Obj_AI_Base> minions;

            if (canQ && Q.IsReady())
            {
                //if (!QtoOne)
                //{
                    minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                    minions.RemoveAll(x => x.MaxHealth <= 5);
                    var positions = new List<Vector2>();

                    foreach (var minion in minions)
                    {
                        positions.Add(minion.ServerPosition.To2D());
                    }

                    var location = MinionManager.GetBestCircularFarmLocation(positions, 160f, Q.Range);

                    if (location.MinionsHit >= 1)
                        Q.Cast(location.Position);
                /*}
                else
                {
                    minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                    minions.RemoveAll(x => x.MaxHealth <= 5);
                    minions.RemoveAll(x => x.Health > Damage.GetSpellDamage(Player, x, SpellSlot.Q)*2);
                    var positions = new List<Vector2>();

                    foreach (var minion in minions)
                    {
                        positions.Add(minion.ServerPosition.To2D());
                    }

                    var location = MinionManager.GetBestCircularFarmLocation(positions, 200f, Q.Range);

                    if (location.MinionsHit == 1)
                        Q.Cast(location.Position);
                }*/
            }

            if (!canE || !E.IsReady() || Player.IsZombie)
                return;
            NowE = false;

            minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);
            minions.RemoveAll(x => x.MaxHealth <= 5);
            jgm = minions.Any(x => x.Team == GameObjectTeam.Neutral);

            if ((Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 1 && (minions.Count >= 3 || jgm)) && (((Player.Mana / Player.MaxMana) * 100f) >= MenuIni.SubMenu("Farm").Item("FEPercent").GetValue<Slider>().Value))
                E.Cast();
            else if ((Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 2 && (minions.Count <= 2 && !jgm)) || !(((Player.Mana / Player.MaxMana) * 100f) >= MenuIni.SubMenu("Farm").Item("FEPercent").GetValue<Slider>().Value))
                calcE();
        }
    }
}
