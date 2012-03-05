using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using EVEIntel.Model;
using EVEIntel.Repository.Misc;

namespace EVEIntel.Repository.Parsers
{
    /// <summary>
    /// Parser for GriefWatch killboards
    /// *.greifwatch.net
    /// 
    /// Has differing version of web code on different hosts.
    /// </summary>
    public class GriefWatchParser : Parser
    {
        /* kills on domain/?p=kills
         * losses on domain/?p=losses
         * 
         * link to killmails from kills/losses page
         *  \?p=details&amp;kill=(?<id>\d*?)\"
         *  match ID=kill id
         * Date
         */

        public GriefWatchParser(BackgroundWorker worker, DoWorkEventArgs Args) : base(worker, Args)
        {

        }

        public override List<Kill> GetKillMails(string Query, QueryTypeEnum Type, DateTime MaxAge)
        {
            throw new NotImplementedException();
        }
    }
}
