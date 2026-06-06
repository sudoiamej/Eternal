using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Eternal.Services.System
{
    public class WindowsRegistryLexiconService : IRegistryLexiconService
    {
        public Task<List<LexiconItemDelta>> AnalyzeSystemDriftAsync()
        {
            var deltas = new List<LexiconItemDelta>();
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RegistryLexicon.json");
                if (!File.Exists(jsonPath))
                {
                    // Fallback to current directory if not in base folder
                    jsonPath = "RegistryLexicon.json";
                }

                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var obj = JObject.Parse(json);

                    foreach (var property in obj.Properties())
                    {
                        string theme = property.Name;
                        if (property.Value is JArray array)
                        {
                            foreach (var item in array)
                            {
                                string path = item["Path"]?.ToString() ?? "";
                                string valueName = item["ValueName"]?.ToString() ?? "";
                                string expected = item["ExpectedValue"]?.ToString() ?? "";
                                string description = item["Description"]?.ToString() ?? "";

                                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(valueName)) continue;

                                object? actualObj = GetRegistryValue(path, valueName);
                                string actual = actualObj?.ToString() ?? "MISSING";
                                bool drifted = !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

                                deltas.Add(new LexiconItemDelta
                                {
                                    Theme = theme,
                                    Description = description,
                                    Path = path,
                                    ValueName = valueName,
                                    ExpectedValue = expected,
                                    ActualValue = actual,
                                    IsDrifted = drifted
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silence or log locally
                global::System.Diagnostics.Debug.WriteLine($"Lexicon error: {ex.Message}");
            }
            return Task.FromResult(deltas);
        }

        public Task RealignSystemAsync(List<LexiconItemDelta> deltas)
        {
            return Task.Run(() =>
            {
                foreach (var delta in deltas)
                {
                    if (delta.IsDrifted)
                    {
                        try
                        {
                            object targetValue;
                            if (int.TryParse(delta.ExpectedValue, out int intVal))
                            {
                                targetValue = intVal;
                            }
                            else
                            {
                                targetValue = delta.ExpectedValue;
                            }

                            SetRegistryValue(delta.Path, delta.ValueName, targetValue);
                            delta.ActualValue = delta.ExpectedValue;
                            delta.IsDrifted = false;
                        }
                        catch (Exception ex)
                        {
                            global::System.Diagnostics.Debug.WriteLine($"Realign failed: {ex.Message}");
                        }
                    }
                }
            });
        }

        private static object? GetRegistryValue(string keyPath, string valueName)
        {
            try
            {
                RegistryKey? rootKey = null;
                string subKeyPath = "";

                if (keyPath.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
                {
                    rootKey = Registry.LocalMachine;
                    subKeyPath = keyPath.Substring("HKEY_LOCAL_MACHINE\\".Length);
                }
                else if (keyPath.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
                {
                    rootKey = Registry.CurrentUser;
                    subKeyPath = keyPath.Substring("HKEY_CURRENT_USER\\".Length);
                }

                if (rootKey != null)
                {
                    using var key = rootKey.OpenSubKey(subKeyPath, false);
                    return key?.GetValue(valueName);
                }
            }
            catch { }
            return null;
        }

        private static void SetRegistryValue(string keyPath, string valueName, object value)
        {
            RegistryKey? rootKey = null;
            string subKeyPath = "";

            if (keyPath.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
            {
                rootKey = Registry.LocalMachine;
                subKeyPath = keyPath.Substring("HKEY_LOCAL_MACHINE\\".Length);
            }
            else if (keyPath.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
            {
                rootKey = Registry.CurrentUser;
                subKeyPath = keyPath.Substring("HKEY_CURRENT_USER\\".Length);
            }

            if (rootKey != null)
            {
                using var key = rootKey.OpenSubKey(subKeyPath, true) ?? rootKey.CreateSubKey(subKeyPath);
                if (value is int intVal)
                {
                    key?.SetValue(valueName, intVal, RegistryValueKind.DWord);
                }
                else
                {
                    key?.SetValue(valueName, value.ToString() ?? "", RegistryValueKind.String);
                }
            }
        }
    }
}
