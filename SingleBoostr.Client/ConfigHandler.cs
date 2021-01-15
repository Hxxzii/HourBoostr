using System;
using System.IO;
using IniParser;
using IniParser.Model;

namespace SingleBoostr.Client
{
    public class ConfigHandler
    {
        internal static string ConfigPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
        private FileIniDataParser Parser { get; set; }
        private IniData Data { get; set; }

        public ConfigHandler()
        {
            if (File.Exists(ConfigPath) && new FileInfo(ConfigPath).Length != 0) return;
            
            Program.SetConsoleTextColor(ConsoleColor.Red);
            File.CreateText(ConfigPath).Dispose();
            Console.WriteLine("ERROR: Config doesn't exist (config.ini), or it is empty");
            Console.WriteLine("Config file has been created/regenerated, please adjust accordingly");

            FreshConfig();

            Console.WriteLine("(exit this program, then edit the config file)");
            while (true) Console.ReadKey();
        }

        public void FreshConfig()
        {
            Parser = new FileIniDataParser();
            Data = new IniData();
            Data.Sections.AddSection("Config");
            Data["Config"].AddKey("SecondsUntilRestart", "3600");
            Data["Config"].AddKey("InputAppIdsDuringRuntime", "false");
            Parser.WriteFile(ConfigPath, Data);
        }

        public void InitializeData()
        {
            Parser = new FileIniDataParser();
            Data = Parser.ReadFile(ConfigPath);
        }

        public string GetValue(string name)
        {
            CheckForUninitializedConfig();
            return Data["Config"][name];
        }

        public void SetValue(string name, string value)
        {
            CheckForUninitializedConfig();
            Data["Config"][name] = value;
        }
        
        public void SaveData()
        {
            CheckForUninitializedConfig();
            Parser.WriteFile(ConfigPath, Data);
        }

        // Will override and replace currently loaded INI data
        public void ReadData()
        {
            CheckForUninitializedConfig();
            Data = Parser.ReadFile(ConfigPath);
        }

        private void CheckForUninitializedConfig()
        {
            if (Data is null || Parser is null)
            {
                Program.SetConsoleTextColor(ConsoleColor.Red);
                Console.WriteLine("INI config was attempted to be accessed whilst the config was uninitialized");
                Console.WriteLine("You shouldn't be getting this error, please create a GitHub issue if you do");
                Console.WriteLine("(please exit now)");
                while (true)
                {
                    Console.ReadKey();
                }
            }
        }
    }
}
