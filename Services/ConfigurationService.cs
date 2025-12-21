using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MacroApp.Models;

namespace MacroApp.Services
{
    public class AppSettings
    {
        public SupabaseSettings Supabase { get; set; } = new();
        public AuthenticationSettings Authentication { get; set; } = new();
        public MacroSettings Macro { get; set; } = new();
        public ApplicationSettings Application { get; set; } = new();
    }

    public class SupabaseSettings
    {
        public string Url { get; set; } = "";
        public string AnonKey { get; set; } = "";
    }

    public class AuthenticationSettings
    {
        public int OfflineGraceDays { get; set; } = 30;
        public string AuthFileName { get; set; } = "auth.dat";
    }

    public class MacroSettings
    {
        public int DefaultKeySequenceDelay { get; set; } = 10;
        public int DefaultActionDuration { get; set; } = 1;
        public int DefaultActionDelay { get; set; } = 0;
        public int MaxActionsPerMacro { get; set; } = 50;
    }

    public class ApplicationSettings
    {
        public string Version { get; set; } = "1.0.0";
        public bool MinimizeToTray { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
    }

    public class ConfigurationService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MacroApp", "config.json");

        private static readonly string DefaultConfigPath = "appsettings.json";
        
        private static ConfigurationService _instance;
        public static ConfigurationService Instance => _instance ??= new ConfigurationService();
        
        private AppSettings _settings;

        private ConfigurationService()
        {
            LoadSettings();
        }

        public AppSettings Settings => _settings;

        private void LoadSettings()
        {
            try
            {
                // Try to load user settings first
                if (File.Exists(ConfigPath))
                {
                    var userConfigJson = File.ReadAllText(ConfigPath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(userConfigJson) ?? new AppSettings();
                }
                else
                {
                    // Load default settings
                    if (File.Exists(DefaultConfigPath))
                    {
                        var defaultConfigJson = File.ReadAllText(DefaultConfigPath);
                        _settings = JsonConvert.DeserializeObject<AppSettings>(defaultConfigJson) ?? new AppSettings();
                    }
                    else
                    {
                        _settings = new AppSettings();
                    }

                    // Save default settings to user config
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                // If loading fails, use default settings
                _settings = new AppSettings();
                
                // Log error (you might want to implement proper logging)
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save settings
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        public void UpdateSupabaseSettings(string url, string anonKey)
        {
            _settings.Supabase.Url = url;
            _settings.Supabase.AnonKey = anonKey;
            SaveSettings();
        }

        public void UpdateAuthenticationSettings(int offlineGraceDays)
        {
            _settings.Authentication.OfflineGraceDays = offlineGraceDays;
            SaveSettings();
        }

        public void UpdateApplicationSettings(bool minimizeToTray, bool startWithWindows, bool checkForUpdates)
        {
            _settings.Application.MinimizeToTray = minimizeToTray;
            _settings.Application.StartWithWindows = startWithWindows;
            _settings.Application.CheckForUpdates = checkForUpdates;
            SaveSettings();
        }

        public MacroConfiguration LoadMacroConfiguration()
        {
            try
            {
                var macroConfigPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MacroApp", "macro_config.json");

                if (File.Exists(macroConfigPath))
                {
                    var json = File.ReadAllText(macroConfigPath);
                    return JsonConvert.DeserializeObject<MacroConfiguration>(json) ?? new MacroConfiguration();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load macro configuration: {ex.Message}");
            }

            return new MacroConfiguration();
        }

        public void SaveMacroConfiguration(MacroConfiguration configuration)
        {
            try
            {
                var macroConfigPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MacroApp", "macro_config.json");

                var directory = Path.GetDirectoryName(macroConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllText(macroConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save macro configuration: {ex.Message}", ex);
            }
        }

        public void ResetToDefaults()
        {
            _settings = new AppSettings();
            SaveSettings();
        }

        public string GetAuthFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MacroApp", _settings.Authentication.AuthFileName);
        }

        public string GetLogFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MacroApp", "logs", "macro-app.log");
        }

        // وظائف جديدة لحفظ وتحميل تسلسل الماكرو
        public void SaveMacroSequence(List<MacroActionItem> sequence)
        {
            try
            {
                var sequencePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MacroApp", "macro_sequence.json");

                var directory = Path.GetDirectoryName(sequencePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(sequence, Formatting.Indented);
                File.WriteAllText(sequencePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save macro sequence: {ex.Message}", ex);
            }
        }

        public List<MacroActionItem> LoadMacroSequence()
        {
            try
            {
                var sequencePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MacroApp", "macro_sequence.json");

                if (File.Exists(sequencePath))
                {
                    var json = File.ReadAllText(sequencePath);
                    var sequence = JsonConvert.DeserializeObject<List<MacroActionItem>>(json);
                    
                    // تحديث خصائص العرض لكل عنصر
                    if (sequence != null)
                    {
                        foreach (var item in sequence)
                        {
                            item.UpdateDisplayProperties();
                        }
                        return sequence;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load macro sequence: {ex.Message}");
            }

            return new List<MacroActionItem>();
        }

        public MacroConfiguration LoadConfiguration()
        {
            return LoadMacroConfiguration();
        }

        public void SaveConfiguration(MacroConfiguration configuration)
        {
            SaveMacroConfiguration(configuration);
        }
    }
}