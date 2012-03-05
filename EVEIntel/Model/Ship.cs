using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVEIntel.Model
{
    public class Ship : IData
    {
        public string Name { get; set; }
        public DateTime LastUsed { get; set; }
        public int TimesUsed { get; set; }
        public int TimesLost { get; set; }

        public bool Validate()
        {
            return !String.IsNullOrEmpty(Name);
        }
    }
}
