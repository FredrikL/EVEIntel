using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVEIntel.Model
{
    public abstract class PilotGroup : IData
    {
        public string Name { get; set; }
        public List<Player> KnownMembers { get; set; }
        public List<Ship> KnownShips { get; set; }
        public List<Seen> Seen { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}
