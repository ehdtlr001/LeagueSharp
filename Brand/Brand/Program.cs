using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Brand
{
    internal class Program
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static Spell Q = new Spell(SpellSlot.Q, 1050f, TargetSelector.DamageType.Magical);
        private static Spell W = new Spell(SpellSlot.W, 900f, TargetSelector.DamageType.Magical);
        private static Spell E = new Spell(SpellSlot.E, 625f, TargetSelector.DamageType.Magical);
        private static Spell R = new Spell(SpellSlot.R, 750f, TargetSelector.DamageType.Magical);
        private static Spell _Ignite = new Spell(SpellSlot.Unknown, 600);

        private static Menu MenuIni;
        private static Orbwalking.Orbwalker orbwalker;
        private static Obj_AI_Hero Target;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Brand") return;

            Q.SetSkillshot(0.625f, 50f, 1600f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(1f, 240f, int.MaxValue, false, SkillshotType.SkillshotCircle);

            var Ignite = Player.Spellbook.Spells.FirstOrDefault(spell => spell.Name == "summonerdot");
            if (Ignite != null) _Ignite.Slot = Ignite.Slot;

            MenuIni = new Menu("SN Brand", "SN Brand", true);
            MenuIni.AddToMainMenu();

            Menu orbwalkerMenu = new Menu("OrbWalker", "OrbWalker");
            orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            MenuIni.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            MenuIni.AddSubMenu(targetSelectorMenu);

            var Combo = new Menu("Combo", "Combo");
            Combo.AddItem(new MenuItem("Use_Q", "Use_Q").SetValue(true));
            Combo.AddItem(new MenuItem("Use_W", "Use_W").SetValue(true));
            Combo.AddItem(new MenuItem("Use_E", "Use_E").SetValue(true));
            Combo.AddItem(new MenuItem("Use_R", "Use_R").SetValue(true));
            Combo.AddItem(new MenuItem("Q_HitChance", "Q_HitChance").SetValue(new Slider(6, 1, 6)));
            Combo.AddItem(new MenuItem("W_HitChance", "W_HitChance").SetValue(new Slider(6, 1, 6)));
            MenuIni.AddSubMenu(Combo);

            var Harass = new Menu("Harass", "Harass");
            Harass.AddItem(new MenuItem("Use_Q", "Use_Q").SetValue(true));
            Harass.AddItem(new MenuItem("Use_W", "Use_W").SetValue(true));
            Harass.AddItem(new MenuItem("Use_E", "Use_E").SetValue(false));
            Harass.AddItem(new MenuItem("Q_HitChance", "Q_HitChance").SetValue(new Slider(6, 1, 6)));
            Harass.AddItem(new MenuItem("W_HitChance", "W_HitChance").SetValue(new Slider(6, 1, 6)));
            MenuIni.AddSubMenu(Harass);

            var Farm = new Menu("Farm", "Farm");
            Farm.AddItem(new MenuItem("Use_W", "Use_W").SetValue(true));
            MenuIni.AddSubMenu(Farm);

            var Misc = new Menu("Misc", "Misc");
            Misc.AddItem(new MenuItem("Gapcloser", "Gapcloser").SetValue(true));
            Misc.AddItem(new MenuItem("Interrupt", "Interrupt").SetValue(true));
            Misc.AddItem(new MenuItem("Ignite", "Ignite").SetValue(true));
            MenuIni.AddSubMenu(Misc);

            var Draw = new Menu("Draw", "Draw");
            Draw.AddItem(new MenuItem("Enabled", "Enabled").SetValue(true));
            Draw.AddItem(new MenuItem("Draw_Q", "Draw_Q").SetValue(new Circle(true, Color.Green)));
            Draw.AddItem(new MenuItem("Draw_W", "Draw_W").SetValue(new Circle(true, Color.Green)));
            Draw.AddItem(new MenuItem("Draw_E", "Draw_E").SetValue(new Circle(true, Color.Green)));
            Draw.AddItem(new MenuItem("Draw_R", "Draw_R").SetValue(new Circle(true, Color.Green)));
            Draw.AddItem(new MenuItem("Draw_Damage", "Draw_Damage").SetValue(true));
            MenuIni.AddSubMenu(Draw);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            Utility.HpBarDamageIndicator.Enabled = MenuIni.SubMenu("Draw").Item("Draw_Damage").GetValue<bool>();

            Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var activeOrbwalker = orbwalker.ActiveMode;
            switch (activeOrbwalker)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (Target.IsValid && Target.IsValidTarget())
                    {
                        Combo(Target);
                    }
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    if (MenuIni.SubMenu("Farm").Item("Use_W").GetValue<bool>())
                        Farm();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if (Target.IsValid && Target.IsValidTarget())
                    {
                        Harass(Target);
                    }
                    break;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (MenuIni.SubMenu("Draw").Item("Enabled").GetValue<bool>() == true)
            {
                if (MenuIni.SubMenu("Draw").Item("Draw_Q").GetValue<Circle>().Active)
                {
                    Render.Circle.DrawCircle(Player.Position, Q.Range, MenuIni.SubMenu("Draw").Item("Draw_Q").GetValue<Circle>().Color);
                }
                if (MenuIni.SubMenu("Draw").Item("Draw_W").GetValue<Circle>().Active)
                {
                    Render.Circle.DrawCircle(Player.Position, W.Range, MenuIni.SubMenu("Draw").Item("Draw_W").GetValue<Circle>().Color);
                }
                if (MenuIni.SubMenu("Draw").Item("Draw_E").GetValue<Circle>().Active)
                {
                    Render.Circle.DrawCircle(Player.Position, E.Range, MenuIni.SubMenu("Draw").Item("Draw_E").GetValue<Circle>().Color);
                }
                if (MenuIni.SubMenu("Draw").Item("Draw_R").GetValue<Circle>().Active)
                {
                    Render.Circle.DrawCircle(Player.Position, R.Range, MenuIni.SubMenu("Draw").Item("Draw_R").GetValue<Circle>().Color);
                }
            }
        }

        private static void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!MenuIni.SubMenu("Misc").Item("Interrupt").GetValue<bool>())
                return;

            if (sender.HasBuff("brandablaze") && Q.IsReady())
            {
                Q.Cast(sender);
            }
            else
            {
                if (E.IsReady() && Q.IsReady())
                {
                    E.CastOnUnit(sender);
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!MenuIni.SubMenu("Misc").Item("Gapcloser").GetValue<bool>())
                return;

            if (gapcloser.Sender.HasBuff("brandablaze") && Q.IsReady())
            {
                Q.Cast(gapcloser.Sender);
            }
            else
            {
                if (E.IsReady() && Q.IsReady())
                {
                    E.CastOnUnit(gapcloser.Sender);
                }
            }
        }

        private static void Harass(Obj_AI_Hero target)
        {
            if (MenuIni.SubMenu("Harass").Item("Use_E").GetValue<bool>())
                if (E.IsReady() && target.IsValidTarget(E.Range))
                    E.CastOnUnit(target);
            if (MenuIni.SubMenu("Harass").Item("Use_Q").GetValue<bool>())
                if (Q.IsReady() && target.IsValidTarget(Q.Range))
                    Q.CastIfHitchanceEquals(target, Hitchance("Q_HitChance"), true);
            if (MenuIni.SubMenu("Harass").Item("Use_W").GetValue<bool>())
                if (W.IsReady() && target.IsValidTarget(W.Range))
                    W.CastIfHitchanceEquals(target, Hitchance("W_HitChance"), true);
        }

        private static void Combo(Obj_AI_Hero target)
        {
            if (target == null) return;

            var Qm = MenuIni.SubMenu("Combo").Item("Use_Q").GetValue<bool>();
            var Wm = MenuIni.SubMenu("Combo").Item("Use_W").GetValue<bool>();
            var Em = MenuIni.SubMenu("Combo").Item("Use_E").GetValue<bool>();
            var Rm = MenuIni.SubMenu("Combo").Item("Use_R").GetValue<bool>();
            var Qcoll = Q.GetPrediction(target).CollisionObjects.OrderBy(unit => unit.Distance(Player.ServerPosition)).FirstOrDefault();
            var QcollC = (Qcoll.Distance(target) > 55) ? true : false;
            var Qd = Damage.GetSpellDamage(Player, target, SpellSlot.Q);
            var Rd = Damage.GetSpellDamage(Player, target, SpellSlot.R);
            var Igd = Damage.GetSummonerSpellDamage(Player, target, Damage.SummonerSpell.Ignite);

            if (Player.Distance(target.Position) < E.Range)
            {
                if (E.IsReady() && Em && target.IsValidTarget(E.Range) && Q.IsReady() && Qm && W.IsReady() && Wm)
                {
                    if (GetDamage(target) > target.Health)
                        _Ignite.CastOnUnit(target);
                    if (Player.Distance(target) > 300 && QcollC)
                    {
                        Q.CastIfHitchanceEquals(target, Hitchance("Use_Q"), true);
                        E.CastOnUnit(target);
                        W.CastIfHitchanceEquals(target, Hitchance("Use_W"), true);
                    }
                    else if (QcollC)
                    {                        
                        E.CastOnUnit(target);
                        Q.CastIfHitchanceEquals(target, Hitchance("Use_Q"), true);
                        W.CastIfHitchanceEquals(target, Hitchance("Use_W"), true);                        
                    }
                    if (Rd >= target.Health && R.IsReady() && Rm)
                        R.CastOnUnit(target);
                    else if (Rd+Rd >= target.Health && target.CountEnemiesInRange(R.Range) >= 2 && R.IsReady() && Rm)
                        R.CastOnUnit(target);
                }
                else
                {
                    if (E.IsReady() && Em)
                        E.CastOnUnit(target);
                    if (W.IsReady() && Wm)
                        W.CastIfHitchanceEquals(target, Hitchance("Use_W"), true);
                    if (Qd > target.Health || target.HasBuff("brandablaze"))
                        Q.CastIfHitchanceEquals(target, Hitchance("Use_Q"), true);                    
                    if (Rd >= target.Health && R.IsReady() && Rm)
                        R.CastOnUnit(target);
                    else if (Rd + Rd >= target.Health && target.CountEnemiesInRange(R.Range) >= 2 && R.IsReady() && Rm)
                        R.CastOnUnit(target);
                }
            }
            else if (Player.Distance(target.Position) > E.Range && Player.Distance(target.Position) < W.Range)
            {
                if (W.IsReady() && Wm)
                    W.CastIfHitchanceEquals(target, Hitchance("Use_W"), true);
                if (Qd > target.Health || target.HasBuff("brandablaze"))
                    Q.CastIfHitchanceEquals(target, Hitchance("Use_Q"), true);
                if (Rd >= target.Health && R.IsReady() && Rm)
                    R.CastOnUnit(target);
                else if (Rd + Rd >= target.Health && target.CountEnemiesInRange(R.Range) >= 2 && R.IsReady() && Rm)
                    R.CastOnUnit(target);
            }
            else if (target.IsValidTarget(Q.Range))
            {
                if (Qd > target.Health || target.HasBuff("brandablaze"))
                    Q.CastIfHitchanceEquals(target, Hitchance("Use_Q"), true);
            }

            if(Igd > target.Health)
                _Ignite.CastOnUnit(target);
        }

        private static void Farm()
        {
            if (!W.IsReady())
                return;

            var minions = MinionManager.GetMinions(W.Range);
            var positions = new List<Vector2>();
            foreach(var minion in minions)
            {
                positions.Add(minion.ServerPosition.To2D());
            }

            var location = MinionManager.GetBestCircularFarmLocation(positions, 240f, W.Range);
            if (location.MinionsHit >= 3)
            {
                W.Cast(location.Position);
            }
        }

        private static HitChance Hitchance(string Type)
        {
            HitChance hit = HitChance.Low;
            switch (MenuIni.Item(Type).GetValue<Slider>().Value)
            {
                case 1:
                    hit = HitChance.OutOfRange;
                    break;
                case 2:
                    hit = HitChance.Impossible;
                    break;
                case 3:
                    hit = HitChance.Low;
                    break;
                case 4:
                    hit = HitChance.Medium;
                    break;
                case 5:
                    hit = HitChance.High;
                    break;
                case 6:
                    hit = HitChance.VeryHigh;
                    break;
            }
            return hit;
        }

        private static double GetDamage(Obj_AI_Hero target)
        {
            var pDamage = Damage.CalcDamage(Player, target, Damage.DamageType.Magical, (.08) * target.MaxHealth);
            var qDamage = Damage.GetSpellDamage(Player, target, SpellSlot.Q);
            var wDamage = Damage.GetSpellDamage(Player, target, SpellSlot.W);
            var eDamage = Damage.GetSpellDamage(Player, target, SpellSlot.E);
            var rDamage = Damage.GetSpellDamage(Player, target, SpellSlot.R);
            var iDamage = Damage.GetSummonerSpellDamage(Player, target, Damage.SummonerSpell.Ignite);
            var totalDamage = 0.0;

            var myMana = Player.Mana;
            var qMana = Q.Instance.ManaCost;
            var wMana = W.Instance.ManaCost;
            var eMana = E.Instance.ManaCost;
            var rMana = R.Instance.ManaCost;
            var totalMana = 0.0;

            if (!Q.IsReady())
                qDamage = 0.0;
            if (!W.IsReady())
                wDamage = 0.0;
            if (!E.IsReady())
                eDamage = 0.0;
            if (!R.IsReady())
                rDamage = 0.0;
            if (_Ignite.Slot == SpellSlot.Unknown)
                iDamage = 0.0;

            if (myMana >= eMana && myMana >= totalMana)
            {
                totalMana += eMana;
                totalDamage += eDamage;
            }

            if (myMana >= qMana && myMana >= totalMana)
            {
                totalMana += qMana;
                totalDamage += qDamage;
            }

            if (myMana >= wMana && myMana >= totalMana)
            {
                totalMana += wMana;
                totalDamage += wDamage;
            }

            if (myMana >= rMana && myMana >= totalMana)
            {
                totalMana += rMana;
                totalDamage += rDamage;
            }

            totalDamage += pDamage;
            totalDamage += iDamage;
            return totalDamage;
        }
    }
}
