using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace EVEIntel.Util
{
    internal class Log
    {
        private static readonly ILog log = getLogger();

        private static ILog getLogger()
        {
            var _log = LogManager.GetLogger("EVEIntel");
            log4net.Config.XmlConfigurator.Configure();
            return _log;
        }

        public static void Debug(string msg)
        {
            if (log.IsDebugEnabled)
                log.Debug(msg);
        }

        public static void Error(string msg)
        {
            if(log.IsErrorEnabled)
                log.Error(msg);
        }

        public static void Warn(string msg)
        {
            if (log.IsWarnEnabled)
                log.Warn(msg);
        }

        public static void Info(string msg)
        {
            if (log.IsInfoEnabled)
                log.Info(msg);
        }

        public static void Fatal(string msg)
        {
            if (log.IsFatalEnabled)
                log.Fatal(msg);
        }
    }
}
