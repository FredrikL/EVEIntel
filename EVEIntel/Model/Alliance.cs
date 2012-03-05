using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVEIntel.Model
{
    public class Alliance : PilotGroup
    {
        public List<Corporation> KnownMemberCorps { get; set; }
    }
}
