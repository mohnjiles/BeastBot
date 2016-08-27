using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBotService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                new DiscordBot.Program().Start();
            }
            catch (Exception ex)
            {
                File.WriteAllText("F:\\boterror.txt", ex.Message);
            }
        }

        protected override void OnStop()
        {
        }
    }
}
