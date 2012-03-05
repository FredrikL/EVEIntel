using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Threading;
using EVEIntel.Repository.Misc;
using EVEIntel.Model;
using EVEIntel.Repository.Parsers;
using System.ComponentModel;
using EVEIntel.Repository.EVEApi;
using EVEIntel.Util;

namespace EVEIntel.Repository
{
    public class DataRepository
    {
        private readonly string ConnectionString;
        private DoWorkEventArgs _WorkEventArgs;

        public DataRepository()
        {
            ConnectionString = string.Format("Data Source={0}\\{1}",
                                             Environment.CurrentDirectory,
                                             "EVEIntel.db");
        }

        private BackgroundWorker Worker { get; set; }

        private void ReportProgress(string Progress)
        {
            if (Worker != null)
                Worker.ReportProgress(0, Progress);
        }

        /// <summary>
        /// Updates db with new killmails from battleclinic
        /// </summary>
        /// <param name="Query"></param>
        /// <param name="Type"></param>
        /// <param name="MaxAge"></param>
        /// <param name="worker"></param>
        /// <param name="workEventArgs"></param>
        /// <returns></returns>
        public bool UpdateData(string Query, QueryTypeEnum Type, DateTime MaxAge, BackgroundWorker worker,
                               DoWorkEventArgs workEventArgs)
        {
            //TODO: Add support for other parsers
            try
            {
                Worker = worker;
                _WorkEventArgs = workEventArgs;
                var bcp = new BattleClinicParser(worker, workEventArgs);
                List<Kill> Kills = bcp.GetKillMails(Query, Type, MaxAge);

                StoreKills(Kills);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);                
                return false;
            }
            return true;
        }

        private void StoreKills(List<Kill> kills)
        {
            // Update all players
            // Add new ships
            // Save new kills
            using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                ReportProgress("Storing Players");
                List<string> ships = new List<string>();
                foreach (var kill in kills)
                {
                    if (Worker.CancellationPending)
                    {
                        _WorkEventArgs.Cancel = true;
                        return;
                    }

                    HandlePlayer(kill.Victim, conn, kill.Time);
                    if (!ships.Contains(kill.Victim.Ship.Name))
                        ships.Add(kill.Victim.Ship.Name);

                    foreach (var killer in kill.Killers)
                    {
                        HandlePlayer(killer, conn, kill.Time);
                        if (!ships.Contains(killer.Ship.Name))
                            ships.Add(killer.Ship.Name);
                    }
                }

                List<Ship> dbShips = GetShips(conn);
                List<string> dShips = (from s in dbShips
                                       select s.Name).ToList();
                List<string> newShips = new List<string>();
                foreach (var s in ships)
                    if (!dShips.Contains(s) &&
                        !newShips.Contains(s))
                        newShips.Add(s);

                if (newShips.Count > 0)
                {
                    ReportProgress("Storing Ships");
                    InsertShips(newShips, conn);
                }

                // save kills
                ReportProgress("Storing Kills");
                int Count = 0;
                foreach (var kill in kills)
                {
                    if (Worker.CancellationPending)
                    {
                        _WorkEventArgs.Cancel = true;
                        return;
                    }

                    Count++;
                    if (Count%50 == 0)
                        ReportProgress(String.Format("Storing Kill {0} of {1}", Count, kills.Count));

                    InsertKill(kill, conn);
                }

            }
        }

        private void InsertKill(Kill kill, SQLiteConnection conn)
        {
            // Make sure that we haven't stored this
            // kill already
            bool found = false;
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * " +
                                  "FROM Seen LEFT OUTER JOIN " +
                                  "Kill ON Seen.KillID = Kill.ID " +
                                  "WHERE (Seen.Player = @NAME) AND (Seen.Ship = @SHIP) AND " +
                                  "(Seen.isKiller = 0) AND (Kill.System = @SYSTEM) AND (Kill.[Time] = @TIME)";
                cmd.Parameters.AddWithValue("@NAME", kill.Victim.Name);
                cmd.Parameters.AddWithValue("@SHIP", kill.Victim.Ship.Name);
                cmd.Parameters.AddWithValue("@SYSTEM", kill.System);
                cmd.Parameters.AddWithValue("@TIME", kill.Time);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    found = reader.HasRows;
                }
            }

            if (found)
                return;

            Int64 id;
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "insert into Kill(System, Time) values(@SYSTEM, @TIME);";
                cmd.Parameters.AddWithValue("@SYSTEM", kill.System);
                cmd.Parameters.AddWithValue("@TIME", kill.Time);
                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();

                cmd.CommandText = "SELECT last_insert_rowid()";
                object ret = cmd.ExecuteScalar();
                id = (Int64) ret;
            }

            InsertSeen(kill.Victim, false, id, conn);
            foreach (var killer in kill.Killers)
                InsertSeen(killer, true, id, conn);
        }

        private void InsertSeen(Player player, bool isKiller, Int64 KillID, SQLiteConnection conn)
        {
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "insert into Seen(Player, Ship, KillID, isKiller) values(@PLAYER, @SHIP, @KILLID, @ISKILLER)";
                cmd.Parameters.AddWithValue("@PLAYER", player.Name);
                cmd.Parameters.AddWithValue("@SHIP", player.Ship.Name);
                cmd.Parameters.AddWithValue("@KILLID", KillID);
                cmd.Parameters.AddWithValue("@ISKILLER", isKiller);
                cmd.ExecuteNonQuery();
            }
        }

        private void HandlePlayer(Player player, SQLiteConnection Conn, DateTime TimeSeen)
        {
            Player P = GetPlayer(player.Name, Conn);
            if (P == null)
                InsertPlayer(player, Conn);
            else if (!P.Equals(player))
                UpdatePlayer(player, Conn, TimeSeen);
        }

        private Player GetPlayer(string Name, SQLiteConnection Conn)
        {
            Player P = null;

            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "select Corp, Alliance from Player where Name = @NAME";
                cmd.Parameters.AddWithValue("@NAME", Name);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        P = new Player
                                {
                                    Name = Name,
                                    Corp = reader.GetString(0),
                                    Alliance = reader.GetString(1)
                                };
                    }
                }
            }

            return P;
        }

        private void UpdatePlayer(Player player, SQLiteConnection Conn, DateTime TimeSeen)
        {
            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                // Get Last seen for player first!
                cmd.CommandText = "select * from Seen "+
                                  "inner join kill on Seen.KillId = Kill.Id "+
                                  "where Seen.Player = @NAME and Kill.Time > @TIME";
                cmd.Parameters.AddWithValue("@NAME", player.Name);
                cmd.Parameters.AddWithValue("@TIME", TimeSeen);

                bool newData = true;
                using(SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if(reader.HasRows)
                        newData = false;
                }

                if (newData)
                {
                    cmd.CommandText = "update Player set Corp = @CORP, Alliance = @ALLIANCE where Name = @NAME";
                    cmd.Parameters.AddWithValue("@CORP", player.Corp);
                    cmd.Parameters.AddWithValue("@ALLIANCE", player.Alliance);
                    cmd.Parameters.AddWithValue("@NAME", player.Name);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertPlayer(Player player, SQLiteConnection Conn)
        {
            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "insert into Player(Name, Corp, Alliance) values(@NAME, @CORP, @ALLIANCE)";
                cmd.Parameters.AddWithValue("@NAME", player.Name);
                cmd.Parameters.AddWithValue("@CORP", player.Corp);
                cmd.Parameters.AddWithValue("@ALLIANCE", player.Alliance);

                cmd.ExecuteNonQuery();
            }
        }

        private List<Ship> GetShips(SQLiteConnection Conn)
        {
            var ret = new List<Ship>();
            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "select Name from ship";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        ret.Add(new Ship {Name = reader.GetString(0)});
                }
            }
            return ret;
        }

        private void InsertShips(List<string> Ships, SQLiteConnection Conn)
        {
            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "insert into Ship(Name) values(@NAME)";

                foreach (var s in Ships)
                {
                    cmd.Parameters.AddWithValue("@NAME", s);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }
            }
        }


        /// <summary>
        /// Returns a list of all players in db matching Query/Type
        /// </summary>
        /// <param name="Query"></param>
        /// <param name="Type"></param>
        /// <param name="FromDate"></param>
        /// <returns></returns>
        public List<Player> GetPilots(string Query, QueryTypeEnum Type, DateTime FromDate)
        {
            switch (Type)
            {
                case QueryTypeEnum.Alliance:
                    return GetAlliancePilots(Query, FromDate);

                case QueryTypeEnum.Corporation:
                    return GetCorporationPilots(Query, FromDate);

                case QueryTypeEnum.Character:
                    return GetCharacter(Query, FromDate);

                default:
                    return new List<Player>();
            }
        }

        /// <summary>
        /// Gets all data about a player
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Player GetPlayer(string Name)
        {
            Player player;
            using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                player = GetFullPlayer(conn, Name);
            }

            return player;
        }
        private Player GetFullPlayer(SQLiteConnection Conn, string Name )
        {
            var player = new Player
                             {
                                 Name = Name,
                                 KnownShips = new List<Ship>(),
                                 Seen = new List<Seen>(),
                                 CharacterID = 0
                             };

            bool updateCharID = false;

            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "select Corp, Alliance, CharacterID from Player where Name = @NAME";
                cmd.Parameters.AddWithValue("@NAME", Name);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        player.Corp = reader.GetString(0);
                        player.Alliance = reader.GetString(1);
                        if(!reader.IsDBNull(2))
                            player.CharacterID = reader.GetInt64(2);
                        if (player.CharacterID == 0)
                            updateCharID = true;
                    }
                }
            }

            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "select Seen.Ship, Seen.isKiller, Kill.System, Kill.Time from Seen" +
                                  " left outer join Kill on seen.KillID = Kill.ID" +
                                  " where Seen.Player = @NAME";
                cmd.Parameters.AddWithValue("@NAME", Name);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ShipName = reader.GetString(0);
                        Ship ship = player.KnownShips.Find(S => S.Name == ShipName);
                        DateTime lastUsed = reader.GetDateTime(3);
                        bool killer = reader.GetBoolean(1);
                        string System = reader.GetString(2);

                        if (ship == null)
                        {
                            ship = new Ship
                                       {
                                           Name = ShipName,
                                           LastUsed = lastUsed,
                                           TimesUsed = 1,
                                           TimesLost = killer ? 0 : 1
                                       };
                            player.KnownShips.Add(ship);
                        }
                        else
                        {
                            ship.TimesUsed++;
                            if (!killer)
                                ship.TimesLost++;
                            if (ship.LastUsed < lastUsed)
                                ship.LastUsed = lastUsed;
                        }

                        Seen seen = player.Seen.Find(S => S.System == System);
                        if (seen == null)
                        {
                            seen = new Seen
                                       {
                                           Count = 1,
                                           System = System,
                                           Time = lastUsed
                                       };                                                       

                            player.Seen.Add(seen);
                        }
                        else
                        {
                            seen.Count++;
                            if (seen.Time < lastUsed)
                                seen.Time = lastUsed;
                        }

                        if (lastUsed > player.LastSeen.Time)
                        {
                            player.LastSeen.Time = lastUsed;
                            player.LastSeen.System = System;
                        }
                    }
                }
            }

            //TODO: Call update charid + update db
            if(updateCharID)
            {
                player.CharacterID= EVEIntel.Repository.EVEApi.Character.GetCharacterID(player.Name);
                SetPlayerCharacterID(player.Name, player.CharacterID, Conn);
            }

            return player;
        }

        private void SetPlayerCharacterID(string Name, Int64 Id, SQLiteConnection Conn)
        {
            using(SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "update player set CharacterID = @CID where Name = @NAME";
                cmd.Parameters.AddWithValue("@CID", Id);
                cmd.Parameters.AddWithValue("@NAME", Name);
                cmd.ExecuteNonQuery();
            }
        }

        private const string SelectStatement =
            @"select Player.Name, Player.Corp, Player.Alliance From Player
                inner join seen on Player.Name = Seen.Player
                inner join kill on Seen.KillID = Kill.ID
                where UPPER({0}) = UPPER(@QUERY) and Kill.[Time] > @FROMDATE
                Group By Player.Name";

        private List<Player> GetCharacter(string Query, DateTime FromDate)
        {
            string sql = string.Format(SelectStatement, "Player.Name");
            //sql += EndPart;

            return GetPilots(sql,
                             Query, FromDate);
        }

        private List<Player> GetCorporationPilots(string Query, DateTime FromDate)
        {
            string sql = string.Format(SelectStatement, "Player.Corp");
            //sql += GroupBy;
            //sql += EndPart;

            return GetPilots(sql,
                             Query, FromDate);
        }

        private List<Player> GetAlliancePilots(string Query, DateTime FromDate)
        {
            string sql = string.Format(SelectStatement, "Player.Alliance");
            //sql += GroupBy;
            //sql += EndPart;

            return GetPilots(sql,
                             Query, FromDate);
        }

        private List<Player> GetPilots(string SQL, string Query, DateTime FromDate)
        {
            var ret = new List<Player>();
            using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@QUERY", Query);
                    cmd.Parameters.AddWithValue("@FROMDATE", FromDate);

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            var P = new Player
                                        {
                                            Name = name,
                                            Corp = reader.GetString(1),
                                            Alliance = reader.GetString(2),
                                        };
                            ret.Add(P);
                        }
                    }
                }

                foreach (var player in ret)
                {
                    UpdateLastSeen(player, conn);
                }
            }
            return ret;
        }

        private void UpdateLastSeen(Player Player, SQLiteConnection Conn)
        {
            using (SQLiteCommand cmd = Conn.CreateCommand())
            {
                cmd.CommandText = "Select Seen.Ship, Kill.System, Kill.[Time] from Seen" +
                                  " left outer join Kill on Seen.KillID = Kill.ID" +
                                  " Where Seen.Player = @NAME" +
                                  " Order by Kill.[Time] Desc limit 1";
                cmd.Parameters.AddWithValue("@NAME", Player.Name);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Player.Ship = new Ship {Name = reader.GetString(0)};
                        Player.LastSeen = new Seen
                                              {
                                                  System = reader.GetString(1),
                                                  Time = reader.GetDateTime(2)
                                              };
                    }
                }
            }
        }

        private List<Player> GetFullCorporationPilots(string Query)
        {
            var names = new List<string>();
            var ret = new List<Player>();
            
            using(SQLiteConnection conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using(SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select Name from Player where Corp = @QUERY";
                    cmd.Parameters.AddWithValue("@QUERY", Query);

                    using(SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            names.Add(reader.GetString(0));
                        }
                    }
                }

                // get full data
                foreach(string n in names)
                    ret.Add(GetFullPlayer(conn, n));
            }

            return ret;
        }

        private List<Player> GetFullAlliancePilots(string Query)
        {
            var names = new List<string>();
            var ret = new List<Player>();

            using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select Name from Player where Alliance = @QUERY";
                    cmd.Parameters.AddWithValue("@QUERY", Query);

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names.Add(reader.GetString(0));
                        }
                    }
                }

                // get full data
                foreach (string n in names)
                    ret.Add(GetFullPlayer(conn, n));
            }

            return ret;
        }

        public Corporation GetCorporation(string Query)
        {
            List<Player> corpPilots = GetFullCorporationPilots(Query);
            if (corpPilots.Count == 0)
                return new Corporation
                           {
                               Alliance = "NONE",
                               Name = Query
                           };

            // Sort list based on last seen
            // to be closer to getting the right alliance
            corpPilots.Sort((P1, P2) => P1.LastSeen.Time.CompareTo(P2.LastSeen.Time));

            var tmp = corpPilots[corpPilots.Count - 1];
            return new Corporation
                                   {
                                       Name = tmp.Corp,
                                       Alliance = tmp.Alliance,
                                       KnownMembers = corpPilots,
                                       KnownShips = MergeShips(corpPilots),
                                       Seen = MergeLastSeen(corpPilots)
                                   };
        }

        public Alliance GetAlliance(string Query)
        {
            List<Player> corpPilots = GetFullAlliancePilots(Query);
            if (corpPilots.Count == 0)
                return new Alliance
                {
                    Name = Query
                };            
            
            var tmp = corpPilots[corpPilots.Count - 1];
            return new Alliance
            {
                Name = tmp.Alliance,
                KnownMembers = corpPilots,
                KnownShips = MergeShips(corpPilots),
                Seen = MergeLastSeen(corpPilots)
            };
        }

        private List<Ship> MergeShips(List<Player> players)
        {
            List<Ship> ret= new List<Ship>();
            foreach (var player in players)
            {
                if(ret.Count == 0)
                {
                    ret.AddRange(player.KnownShips);
                    continue;
                }

                foreach(Ship s in player.KnownShips)
                {
                    Ship ship = ret.Find(S => S.Name == s.Name);
                    if(ship == null)
                    {
                        ret.Add(s);
                    }
                    else
                    {
                        ship.TimesLost += s.TimesLost;
                        ship.TimesUsed += s.TimesUsed;
                        if (ship.LastUsed < s.LastUsed)
                            ship.LastUsed = s.LastUsed;
                    }
                }
            }

            return ret;
        }

        private List<Seen> MergeLastSeen(List<Player> players)
        {
            List<Seen> ret = new List<Seen>();
            foreach (var player in players)
            {
                if(ret.Count == 0)
                {
                    ret.AddRange(player.Seen);
                    continue;
                }

                foreach (var s in player.Seen)
                {
                    Seen seen = ret.Find(S => S.System == s.System);
                    if(seen == null)
                    {
                        ret.Add(s);
                    }
                    else
                    {
                        seen.Count += s.Count;
                        if (seen.Time < s.Time)
                            seen.Time = s.Time;
                    }
                }
            }

            return ret;
        }
    }
}
    ;