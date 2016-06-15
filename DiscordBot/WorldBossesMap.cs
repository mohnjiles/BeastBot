using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beast.Common.Models;
using CsvHelper.Configuration;

namespace DiscordBot
{
    public sealed class WorldBossesMap : CsvClassMap<WorldBoss>
    {
        public WorldBossesMap()
        {
            Map(m => m.Start).Index(0);
            Map(n => n.End).Index(1);
            Map(n => n.EventName).Index(2);
            Map(n => n.Link).Index(3);
        }
    }
}
