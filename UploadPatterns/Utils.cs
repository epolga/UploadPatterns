using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UploadPatterns
{
    class Utils
    {
        private static ILog mLog4netLogger = null;

        private static ILog GetLogger()
        {
            XmlConfigurator.Configure();
            return LogManager.GetLogger(typeof(MainWindow));
        }
        
        public static ILog Logger
        {
            get
            {
                if (mLog4netLogger == null)
                {
                    mLog4netLogger = GetLogger();
                }

                return mLog4netLogger;
            }
        }
    }
}
