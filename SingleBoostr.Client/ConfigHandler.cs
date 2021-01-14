using System;
using System.IO;
using IniParser;
using IniParser.Model;

namespace SingleBoostr.Client
{
    public class ConfigHandler
    {
        internal static string ConfigPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
        private FileIniDataParser Parser { get; } = new FileIniDataParser();
        private IniData Data { get; set; } = new IniData();
        public bool ConfigExistsAndValid => File.Exists(ConfigPath) && new FileInfo(ConfigPath).Length != 0;
        public void FreshConfig()
        {
            Data.Sections.AddSection("Config");
            Data["Config"].AddKey("SecondsUntilRestart", "3600");
            Data["Config"].AddKey("InputAppIdsDuringRuntime", "false");
            Parser.WriteFile(ConfigPath, Data);
        }
        
        public void InitializeData()
        {
            Data = Parser.ReadFile(ConfigPath);
        }

        public string GetValue(string name)
        {
            return Data["Config"][name];
        }

        public void SetValue(string name, string value)
        {
            Data["Config"][name] = value;
        }
    }
}
