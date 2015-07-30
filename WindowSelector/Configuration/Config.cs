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
            Settings["Positions"]["N1"].Value = "[" +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":50.0,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":66.67,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N2"].Value = "[" +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":100.0,\"Height\":50.0}," +
                                    "{\"X\":25.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":50.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":66.67,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N3"].Value = "[" +
                                    "{\"X\":50.0,\"Y\":0.0,\"Width\":50.0,\"Height\":50.0}," +
                                    "{\"X\":75.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":66.67,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}," +
                                    "{\"X\":33.33,\"Y\":0.0,\"Width\":66.67,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N4"].Value = "[" +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":50.0,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":66.67,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N5"].Value = "[" +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":100.0,\"Height\":50.0}," +
                                    "{\"X\":25.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":50.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":66.67,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N6"].Value = "[" +
                                    "{\"X\":50.0,\"Y\":0.0,\"Width\":50.0,\"Height\":50.0}," +
                                    "{\"X\":75.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":66.67,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}," +
                                    "{\"X\":33.33,\"Y\":0.0,\"Width\":66.67,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N7"].Value = "[" +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":50.0,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}," +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":66.67,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N8"].Value = "[" +
                                    "{\"X\":0.0,\"Y\":0.0,\"Width\":100.0,\"Height\":50.0}," +
                                    "{\"X\":25.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":50.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":66.67,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}" +
                                    "]";
            Settings["Positions"]["N9"].Value = "[" +
                                    "{\"X\":50.0,\"Y\":0.0,\"Width\":50.0,\"Height\":50.0}," +
                                    "{\"X\":75.0,\"Y\":0.0,\"Width\":25.0,\"Height\":50.0}," +
                                    "{\"X\":66.67,\"Y\":0.0,\"Width\":33.33,\"Height\":50.0}," +
                                    "{\"X\":33.33,\"Y\":0.0,\"Width\":66.67,\"Height\":50.0}" +
                                    "]";
            config.Commit();

            #endregion
        }
    }
}
