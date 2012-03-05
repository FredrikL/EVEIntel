using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using EVEIntel.Model;

namespace EVEIntel.Repository.Parsers
{
        public class KilMailParser
        {
            public Kill ParseKillMail(string killMail)
            {
                StringReader reader = new StringReader(killMail);
                Kill kill = new Kill();

                try
                {
                    kill.Time = GetIncidentTime(reader);
                    MoveReader(reader, 1);
                    kill.Victim = GetVictim(reader, kill);
                    MoveReader(reader, 3);
                    GetInvolvedPlayers(kill, reader);
                }
                catch (Exception)
                {
                    kill.IsComplete = false;
                }
                return kill;
            }

            private void GetInvolvedPlayers(Kill kill, StringReader reader)
            {
                kill.Killers.Add(GetInvolved(reader));
                MoveReader(reader, 1);
                while (HasMoreInvolved(reader))
                {
                    kill.Killers.Add(GetInvolved(reader));
                }
            }

            private Player GetInvolved(StringReader reader)
            {
                Player p = new Player();

                List<string> involved = ReadLines(reader, 8);
                p.Name = involved[0].Replace("Name: ", "").Replace(" (laid the final blow)", "");
                p.Corp = involved[2].Replace("Corp: ", "");
                p.Alliance = involved[3].Replace("Alliance: ", "");
                p.Faction = involved[4].Replace("Faction: ", "");
                p.Ship = new Ship {Name = involved[5].Replace("Ship: ", "")};

                return p;
            }

            private Player GetVictim(StringReader reader, Kill k)
            {
                Player P = new Player();
                List<string> victim = ReadLines(reader, 8);
                P.Name = victim[0].Replace("Victim: ", "");
                P.Corp = victim[1].Replace("Corp: ", "");
                P.Alliance = victim[2].Replace("Alliance: ", "");
                P.Faction = victim[3].Replace("Faction: ", "");
                P.Ship = new Ship {Name = victim[4].Replace("Destroyed: ", "")};
                k.System = victim[5].Replace("System: ", "");

                return P;
            }

            private DateTime GetIncidentTime(StringReader stringReader)
            {
                return DateTime.ParseExact(stringReader.ReadLine(), "yyyy.MM.dd hh:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }

            private List<string> ReadLines(StringReader reader, int count)
            {
                List<string> ret = new List<string>();
                for (int i = 0; i < count; i++)
                    ret.Add(reader.ReadLine());
                return ret;
            }

            private void MoveReader(StringReader reader, int count)
            {
                for (int i = 0; i < count; i++)
                    reader.ReadLine();
            }

            private bool HasMoreInvolved(StringReader reader)
            {
                return ((char)reader.Peek() == 'N');
            }
        }    
}
