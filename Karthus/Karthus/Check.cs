using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Karthus
{
    internal class TI
    {
        public Obj_AI_Hero Player;
        public int timeCheck;

        public TI(Obj_AI_Hero player)
        {
            Player = player;
        }
    }

    internal class Check
    {
        public IEnumerable<Obj_AI_Hero> ETeam;
        public IEnumerable<Obj_AI_Hero> ATeam;
        public List<TI> TI = new List<TI>();

        public Check()
        {
            var champs = ObjectManager.Get<Obj_AI_Hero>().ToList();

            ATeam = champs.Where(x => x.IsAlly);
            ETeam = champs.Where(x => x.IsEnemy);

            TI = ETeam.Select(x => new TI(x)).ToList();

            Game.OnUpdate += Game_OnUpdate;
        }

        void Game_OnUpdate(EventArgs args)
        {
            var time = Utils.TickCount;

            foreach (TI ti in TI.Where(x => x.Player.IsVisible && !x.Player.IsRecalling()))
                ti.timeCheck = time;
        }

        public TI GetEI(Obj_AI_Hero E)
        {
            return Program.Check.TI.Find(x => x.Player.NetworkId == E.NetworkId);
        }

        public float GetTargetHealth(TI ti, int addTime)
        {
            if (ti.Player.IsVisible)
                return ti.Player.Health;

            var predhealth = ti.Player.Health + ti.Player.HPRegenRate * ((Utils.TickCount - ti.timeCheck + addTime) / 1000f);

            return predhealth > ti.Player.MaxHealth ? ti.Player.MaxHealth : predhealth;
        }

        public bool recalltc(TI ti)
        {
            if (ti.Player.HasBuff("exaltedwithbaronnashor"))
            {
                if (((Utils.TickCount - ti.timeCheck + 3000f) / 1000f) < 4)
                {
                    return true;
                }
                else
                    return false;
            }
            else if (((Utils.TickCount - ti.timeCheck + 3000f) / 1000f) < 10)
            {
                return true;
            }
            else
                return false;
        }

        public static T GetSafeMenuItem<T>(MenuItem item)
        {
            if (item != null)
                return item.GetValue<T>();

            return default(T);
        }
    }
}
