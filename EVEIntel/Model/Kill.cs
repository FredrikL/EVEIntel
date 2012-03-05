using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVEIntel.Model
{
    /// <summary>
    /// Class representing a KillMail (http://wiki.eveonline.com/wiki/Killmail)
    /// </summary>
    public class Kill
    {
        public int ID { get; private set; }
        public string System { get; set; }
        public DateTime Time { get; set; }
        public Player Victim { get; set; }
        public List<Player> Killers { get; set; }
        public bool IsComplete { get; set; }

        public Kill()
        {
            Killers = new List<Player>();
        }

        public Kill(int ID)
        {
            this.ID = ID;
        }
    }
}
