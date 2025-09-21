using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicIsland
{
    public static class AppSettings
    {
        public static int Time
        {
            get => Properties.Settings1.Default.Time;
            set
            {
                Properties.Settings1.Default.Time = value;
                Properties.Settings1.Default.Save();
            }
        }
    }
}
