using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Net;
using EVEIntel.Model;
using EVEIntel.Repository.Misc;
using EVEIntel.Util;

namespace EVEIntel.Repository.Parsers
{
    public abstract class Parser
    {
        protected BackgroundWorker _Worker;
        protected DoWorkEventArgs _WorkEventArgs;

        private static Dictionary<string, CookieContainer> CookieYar = new Dictionary<string, CookieContainer>();        

        public Parser(BackgroundWorker worker, DoWorkEventArgs Args)
        {
            _Worker = worker;
            _WorkEventArgs = Args;
        }        

        protected BackgroundWorker Worker
        {
            get { return _Worker; }
        }

        protected DoWorkEventArgs WorkEventArgs
        {
            get { return _WorkEventArgs; }
        }

        protected void ReportProgress(string Progress)
        {
            if (Worker != null)
                Worker.ReportProgress(0, Progress);
        }

        public abstract List<Kill> GetKillMails(string Query, QueryTypeEnum Type, DateTime MaxAge);

        protected static string DownloadDocument(string killBoardUrl)
        {
            try
            {
                return DownLoadData(killBoardUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }
            return "";
        }

        protected static long ConvertToNumber(string data)
        {
            string d = data.Replace(",", "").Replace(".", "").Replace(" ", "");
            long retVal;

            return long.TryParse(d, out retVal) ? retVal : 0;
        }

        protected static string GetUrlPartFromEnum(QueryTypeEnum Enum)
        {
            switch (Enum)
            {
                case QueryTypeEnum.Character:
                    return "player";
                case QueryTypeEnum.Alliance:
                    return "alliance";
                case QueryTypeEnum.Corporation:
                    return "corp";
                default:
                    return "player";
            }
        }

        private static string DownLoadData(string urlToDownload)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(urlToDownload);
            req.CookieContainer = GetCookieConatinerForHost(urlToDownload);
            var response = (HttpWebResponse)req.GetResponse();
            StreamReader strm = new StreamReader(response.GetResponseStream());
            return strm.ReadToEnd();
        }

        private static CookieContainer GetCookieConatinerForHost(string url)
        {
            Uri uri = new Uri(url);
            string host = uri.Host.Replace("www.", "");

            if (CookieYar.ContainsKey(host))
                return CookieYar[host];

            var cc = new CookieContainer();
            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + GetFireFoxCookiePath()))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "select * from moz_cookies where host like '%" + host + "%';";
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cc.Add(new Cookie(reader["name"].ToString(), reader["value"].ToString(),
                                                  reader["path"].ToString(), reader["host"].ToString()));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            CookieYar.Add(host, cc);

            return cc;
        }

        private static string GetFireFoxCookiePath()
        {
            string x = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            x += @"\Mozilla\Firefox\Profiles\";
            DirectoryInfo di = new DirectoryInfo(x);
            DirectoryInfo[] dir = di.GetDirectories("*.default");
            if (dir.Length != 1)
                return string.Empty;

            x += dir[0].Name + @"\" + "cookies.sqlite";

            if (!File.Exists(x))
                return string.Empty;

            return x;
        }
    }
}