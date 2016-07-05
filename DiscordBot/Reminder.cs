using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Reminder
    {
        public string User { get; set; }
        public string BossName { get; set; }
        public DateTime ReminderTime { get; set; }
    }
}
