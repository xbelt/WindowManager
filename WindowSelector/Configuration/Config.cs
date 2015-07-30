using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VAkos;

namespace WindowSelector.Configuration
{
    class Config
    {
        public static ConfigSetting Settings;
        public static Xmlconfig Xmlconfig;

        public static void Init()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var userFilePath = Path.Combine(localAppData, "LukasHaefliger");

            if (!Directory.Exists(userFilePath))
                Directory.CreateDirectory(userFilePath);
            var configFilePath = Path.Combine(userFilePath, "windowSelector.xml");
#if DEBUG
            configFilePath += "d";
#endif
            var alreadyExists = File.Exists(configFilePath);
            var config = new Xmlconfig(configFilePath, true);
            Xmlconfig = config;
            Settings = config.Settings;
            config.CommitOnUnload = true;
            if (alreadyExists)
                return;
            #region initDefault
            Settings["Exclude"]["WinSplit"].Value = "WinSplit";
            Settings["Exclude"]["Hangouts"].Value = "Hangouts";
            Settings["Settings"]["UpdateInterval"].intValue = 2;
            config.Commit();

            #endregion
        }
    }
}
