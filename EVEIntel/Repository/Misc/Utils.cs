using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EVEIntel.Repository.Misc
{
    internal class Utils
    {
        public static string ApplicationDir
        {
            get
            {
                return Environment.CurrentDirectory;
            }
        }

        public static string PortraitDir
        {
            get
            {
                string dir = ApplicationDir + "\\Portraits";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return dir;
            }
        }
    }
}
