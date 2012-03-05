using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using EVEIntel.Repository.Misc;
using EVEIntel.Model;
using System.ComponentModel;
using EVEIntel.Util;

namespace EVEIntel.Repository.Parsers
{
    public class BattleClinicParser : Parser
    {
        //TODO: Include cookie with authenticated info to be able to read killmails?

        public BattleClinicParser(BackgroundWorker worker, DoWorkEventArgs args): base(worker, args) {}

        private const string KillBoardUrlBase = "http://www.battleclinic.com/eve_online/pk/view.php?type={0}&name={1}&filter={2}&page={3}";
        private string Document = String.Empty;

        private static string KillBoardUrl(string Query, QueryTypeEnum Type, bool Kills, int Page)
        {
            return String.Format(KillBoardUrlBase, GetUrlPartFromEnum(Type),
                Query, (Kills ? "kills" : "losses"), Page);
        }

        private static string KillBoardUrl(string Query, QueryTypeEnum Type)
        {
            return KillBoardUrl(Query, Type, true, 1);
        }

        public override List<Kill> GetKillMails(string Query, QueryTypeEnum Type, DateTime MaxAge)
        {
            var kills = new List<Kill>();
            
            try
            {
                kills = ParseAllData(Query, Type, MaxAge);
            }
            catch(Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }

            return kills;
        }

        private int Pages
        {
            get
            {
                if (Document == String.Empty)
                    return 0;

                string count = GetCustomRegexContent("Pages:.*?(?<MATCH>\\d{1,5})</a>\\s+\\[");
                if (count == String.Empty)
                    return 1;

                int c;
                return Int32.TryParse(count, out c) ? c : 0;
            }
        }

        private List<Kill> ParseAllData(string Query, QueryTypeEnum Type, DateTime MaxAge)
        {
            ReportProgress("Downloading killmail pages");
            Document = DownloadDocument(KillBoardUrl(Query, Type, true, 1));

            var KillTime = GetKillTimes();

            for (int i = 2; i <= Pages; i++ )
            {
                if(Worker.CancellationPending)
                {
                    WorkEventArgs.Cancel = true;
                    return new List<Kill>();
                }

                if (KillTime[KillTime.Count - 1].Time < MaxAge)
                    break;
                Document = DownloadDocument(KillBoardUrl(Query, Type, true, i));
                
                KillTime.AddRange(GetKillTimes());
            }

            ReportProgress("Downloading lossmail pages");
            Document = DownloadDocument(KillBoardUrl(Query, Type, false, 1));
            KillTime.AddRange(GetKillTimes());

            for (int i = 2; i <= Pages; i++)
            {
                if (Worker.CancellationPending)
                {
                    WorkEventArgs.Cancel = true;
                    return new List<Kill>();
                }

                if (KillTime[KillTime.Count - 1].Time < MaxAge)
                    break;
                Document = DownloadDocument(KillBoardUrl(Query, Type, false, i));
                KillTime.AddRange(GetKillTimes());
            }

            var result = ParseKillMails(KillTime);

            return result;
        }        

        private List<KillTime> GetKillTimes()
        {
            var Kills = new List<string>();
            var Times = new List<string>();

            Kills.AddRange(GetMatchList("\\[<a\\ href=\"(?<MATCH>.+?)#mail\">View</a>\\]"));
            Times.AddRange(GetMatchList("[(<tr>)|(<td>)][\\W]+<td>Time:</td>[\\W]+<td>(.*?\">)?(?<MATCH>.+?)<"));

            var Ret = new List<KillTime>();

            for(int i = 0; i < Kills.Count; i++)
            {
                var LastUsed = DateTime.ParseExact(Times[i], "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture);

                Ret.Add(new KillTime{Time = LastUsed, Url = Kills[i]});
            }

            return Ret;
        }

        private List<Kill> ParseKillMails(List<KillTime> Kills)
        {
            var ret = new List<Kill>();

            ReportProgress("Downloading killmails");
            Log.Info(String.Format("Got {0} killmails to parse", Kills.Count));

            foreach (KillTime Kill in Kills)
            {
                if(Worker.CancellationPending)
                {
                    WorkEventArgs.Cancel = true;
                    return ret;
                }

                var kill = new Kill();

                if(ret.Count % 20 == 0)
                {
                    ReportProgress(String.Format("{0} of {1} killmails downloaded", ret.Count, Kills.Count));
                }

                Document = DownloadDocument("http://www.battleclinic.com/eve_online/pk/" + Kill.Url);

                #region Victim Regex
                // Killmail Details[\W\w]+Victim:</td>[\W]+<td>(.*?">)?(?<MATCH>.+?)<
                // Killmail Details[\W\w]+Corporation:</td>[\W]+<td>(.*?">)?(?<MATCH>.+?)<
                // Killmail Details[\W\w]+Alliance</td>[\W]+<td>(.*?">)?(?<MATCH>.+?)<
                // Killmail Details[\W\w]*?Ship:</td>[\W]+<td>(?<MATCH>.+?)[\W]
                // Killmail Details[\W\w]*?Location:</td>\W+(.+?)">(?<MATCH>.+?)<
                #endregion
                string Name = GetCustomRegexContent(@"Killmail Details[\W\w]+Victim:</td>[\W]+<td>(.*?"">)?(?<MATCH>.+?)<");
                string Corp =
                    GetCustomRegexContent("Killmail Details[\\W\\w]+Corporation:</td>[\\W]+<td>(.*?\">)?(?<MATCH>.+?)<");
                string Alliance =
                    GetCustomRegexContent("Killmail Details[\\W\\w]+Alliance</td>[\\W]+<td>(.*?\">)?(?<MATCH>.+?)<");
                string Ship = GetCustomRegexContent("Killmail Details[\\W\\w]*?Ship:</td>[\\W]+<td>(?<MATCH>.+?)[\\W]");
                string Loc = GetCustomRegexContent("Killmail Details[\\W\\w]*?Location:</td>\\W+(.+?)\">(?<MATCH>.+?)<");


                var victim = new Player {Alliance = Alliance, Corp = Corp, Name = Name, Ship = new Ship{ Name = Ship}};

                kill.Victim = victim;
                kill.System = Loc;
                kill.Time = Kill.Time;                

                #region Killer Regex
                // >Name:</td>[\W]+<td\sc(.*">)(?<MATCH>.+?)</a>
                // >Corp:</td>[\W]+<td\sc(.*">)(?<MATCH>.+?)</a>
                // >Alliance</td>[\W]+<td\sc(.*">)(?<MATCH>.+?)</a>
                // >Ship:</td>[\W]+<td\scolspan="2">[\W]+(.+?)[\W]+[(</td>)|(Damage)]
                #endregion
                var Names = GetMatchList(">Name:</td>[\\W]+<td\\sc(.*\">)(?<MATCH>.+?)</a>");
                var Corps = GetMatchList(">Corp:</td>[\\W]+<td\\sc(.*\">)(?<MATCH>.+?)</a>");
                var Alliances = GetMatchList(">Alliance</td>[\\W]+<td\\sc(.*\">)(?<MATCH>.+?)</a>");
                var Ships = GetMatchList(">Ship:</td>[\\W]+<td\\ colspan=\"2\">[\\W]+(?<MATCH>.+?)[\\W]+[(</td>)|(Damage)]");
                for(int i = 0; i < Names.Count; i++)
                {
                    var Killer = new Player
                                     {
                                         Name = Names[i],
                                         Alliance = Alliances[i],
                                         Ship = new Ship {Name = Ships[i]},
                                         Corp = Corps[i]
                                     };
                    
                    kill.Killers.Add(Killer);
                }

                ret.Add(kill);
            }

            return ret;
        }      

        /// <summary>
        /// For basic statistics
        /// </summary>
        /// <param name="Header"></param>
        /// <returns></returns>
        private string GetMatchContent(string Header)
        {
            var r = new Regex(Header + @":</td>[\W]+<td>(?<MATCH>.+?)</td>", RegexOptions.None);
            Match m = r.Match(Document);
            try
            {
                Group g = m.Groups["MATCH"];
                return g.Captures[0].ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);            
            }
            return "";
        }

        /// <summary>
        /// match group needs to be named MATCH!!
        /// </summary>
        /// <param name="Reg"></param>
        /// <returns></returns>
        private string GetCustomRegexContent(string Reg)
        {
            var r = new Regex(Reg);
            Match m = r.Match(Document);
            try
            {
                Group g = m.Groups["MATCH"];
                return g.Captures[0].ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }
            return "";
        }

        private List<string> GetMatchList(string RegEx)
        {
            var retVal = new List<string>();

            var r = new Regex(RegEx);
            MatchCollection MC = r.Matches(Document);

            try
            {
                foreach(Match M in MC)
                {
                    Group G = M.Groups["MATCH"];
                    retVal.Add(G.Captures[0].ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }

            return retVal;
        }      
    }
}
