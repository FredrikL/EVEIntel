using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EVEIntel.Repository.Parsers
{
    public class EveDevParser : Parser
    {
        /*
         * ?a=feed returns rss with killmails in cdata fields, cannot be paged
         * 
         * parse urls from ?a=kills & ?a=losses
         *  kb-table-row-.+?\"\s[\w\W]+?location\.href=\'(?<MATCH>.+?)\'\;\">
         *  
         * Victim data:
         *  Victim:[\W\w]+?_id=\d+?\">(?<MATCH>.+?)</a>
         *  Corp:[\W\w]+?_id=\d+?\">(?<MATCH>.+?)</a>
         *  Alliance:[\W\w]+?_id=\d+?\">(?<MATCH>.+?)</a>
         *  Ship:[\W\w]+?id=\d+?\">(?<MATCH>.+?)</a>
         *  Location:[\W\w]+?id=\d+?\">(?<MATCH>.+?)</a> || Location:[\W\w]+?">(?<MATCH>.+?)</a>
         *  Date:[\W\w]+?cell>(?<MATCH>.+?)</td>
         *  
         * Killers: (includes victims data too first _or_ last,
         *           match everything with one regex?)
         *  <a href=\"\?a\=pilot_detail\&plt_id=\d+?\">(.+?)</a>
         *  <a href=\"\?a\=corp_detail\&crp_id=\d+?\">(.+?)</a>
         *  <a href=\"\?a\=alliance_detail\&all_id=\d+?\">(.+?)</a>  // Alliance might fail.. (match all in one regex?)
         *  <a href=\"\?a\=invtype\&id=\d+?\">(.+?)</a></b> 
         *  
         * Kills mk2: (seems to work on most kbs..)
         *  \"><a href=\"\?a\=pilot_detail\&plt_id=\d+?\">(.+?)</a>[\W\w]+?<a href=.+?\">(.+?)</a>[\W\w]+?<a href=.+?\">(.+?)</a>[\W\w]+?<a href=\"\?a\=invtype\&id=\d+?\">(.+?)</a></b> 
         */

        public EveDevParser(BackgroundWorker worker, DoWorkEventArgs Args) : base(worker, Args)
        {
        }

        public override List<EVEIntel.Model.Kill> GetKillMails(string Query, EVEIntel.Repository.Misc.QueryTypeEnum Type, DateTime MaxAge)
        {
            throw new NotImplementedException();
        }
    }
}
