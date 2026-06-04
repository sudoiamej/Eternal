using System;
using System.IO;
using Newtonsoft.Json;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface ISettingsService
    {
        AppSettings Current { get; }
        void Save();
        void Load();
        event EventHandler<AppSettings> SettingsChanged;
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _filePath;
        public AppSettings Current { get; private set; }
        public event EventHandler<AppSettings> SettingsChanged;

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "EternalAnalytics");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "settings.json");
            
            Current = new AppSettings();
            Load();
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
                File.WriteAllText(_filePath, json);
                SettingsChanged?.Invoke(this, Current);
            }
            catch { }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    var tempSettings = JsonConvert.DeserializeObject<AppSettings>(json, new JsonSerializerSettings
                    {
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                    
                    if (tempSettings == null || string.IsNullOrEmpty(tempSettings.AppVersion) || tempSettings.AppVersion != "3.0.0")
                    {
                        // Clean app data directory contents to avoid schema conflict
                        string folder = Path.GetDirectoryName(_filePath);
                        if (Directory.Exists(folder))
                        {
                            foreach (var file in Directory.GetFiles(folder))
                            {
                                try { File.Delete(file); } catch { }
                            }
                            foreach (var dir in Directory.GetDirectories(folder))
                            {
                                try { Directory.Delete(dir, true); } catch { }
                            }
                        }
                        
                        Current = new AppSettings();
                        Current.AppVersion = "3.0.0";
                        Save();
                        return;
                    }
                    
                    Current = tempSettings;
                    
                    // Seamless transition: Upgrade old default pin '1234' to the new user pin '072906'
                    if (Current.StartupLockPin == "1234")
                    {
                        Current.StartupLockPin = "072906";
                        Save();
                    }
                }
                else
                {
                    Current = new AppSettings();
                    Current.AppVersion = "3.0.0";
                    Save();
                }
            }
            catch 
            {
                Current = new AppSettings();
                Current.AppVersion = "3.0.0";
            }
        }
    }
}