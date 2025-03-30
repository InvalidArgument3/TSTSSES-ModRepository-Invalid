using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace CustomHangar
{
    public enum TimerType
    {
        RetrievalCooldown,
        StorageCooldown
    }

    public class FactionTimers
    {
        public List<TimerTypes> timers;

        public static void AddTimer(IMyFaction faction, TimerType type, int timeAmt)
        {
            TimerTypes timer = new TimerTypes()
            {
                type = type,
                time = timeAmt
            };

            FactionTimers timers = new FactionTimers()
            {
                timers = new List<TimerTypes> { timer }
            };

            if (Session.Instance.cooldownTimers.ContainsKey(faction))
                Session.Instance.cooldownTimers[faction].timers.Add(timer);
            else
                Session.Instance.cooldownTimers.Add(faction, timers);
        }
    }

    public class TimerTypes
    {
        public TimerType type;
        public int time;
    }
}
