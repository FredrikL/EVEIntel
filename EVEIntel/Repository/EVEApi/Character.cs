using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using System.Windows.Media.Imaging;
using EVEIntel.Model;
using EVEIntel.Repository.Misc;
using System.IO;

namespace EVEIntel.Repository.EVEApi
{
    internal class Character
    {
        private const string CharacterIDURL = "http://api.eve-online.com/eve/CharacterID.xml.aspx?names=";
        public static Int64 GetCharacterID(string Name)
        {
            var xd = new XmlDocument();
            xd.Load(CharacterIDURL + HttpUtility.UrlEncode(Name));

            XmlNode node = xd.SelectSingleNode("/eveapi/result/rowset/row");

            if (node == null)
                return 0;

            return Int64.Parse(node.Attributes.GetNamedItem("characterID").Value);
        }

        public static BitmapImage GetCharacterPortrait(Player player)
        {
            if (player == null || player.CharacterID == 0)
                return null;

            int Size = 64;
            string filePath = string.Format("{0}\\{1}.jpg", Utils.PortraitDir, player.CharacterID);

            BitmapImage image = null;
            try
            {
                if (!File.Exists(filePath))
                {

                    WebClient wc = new WebClient();
                    wc.DownloadFile(
                        string.Format("http://img.eve.is/serv.asp?s={0}&c={1}", Size, player.CharacterID),
                        filePath);
                }

                image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(filePath,
                                          UriKind.Absolute);
                image.EndInit();

            }
            catch (Exception)
            {
            }

            return image;
        }
    }
}
