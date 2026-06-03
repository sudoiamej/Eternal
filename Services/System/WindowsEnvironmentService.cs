using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Eternal.Services.System
{
    public interface IEnvironmentService
    {
        bool IsPeMode { get; }
        string SystemDrive { get; }
        Task<List<EnvVar>> GetVariablesAsync();
        Task<bool> SetVariableAsync(string name, string value, bool isSystem);
    }

    public record EnvVar(string Name, string Value, bool IsSystem);

    public class WindowsEnvironmentService : IEnvironmentService
    {
        public bool IsPeMode { get; }
        public string SystemDrive { get; }

        public WindowsEnvironmentService()
        {
            IsPeMode = Eternal.Helpers.OsHelper.IsWinPE();
            
            SystemDrive = IsPeMode ? "X:" : "C:";
        }

        public Task<List<EnvVar>> GetVariablesAsync()
        {
            return Task.Run(() =>
            {
                var vars = new List<EnvVar>();
                try
                {
                    // User variables
                    var userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
                    foreach (global::System.Collections.DictionaryEntry de in userVars)
                    {
                        vars.Add(new EnvVar(de.Key?.ToString() ?? "Unknown", de.Value?.ToString() ?? "", false));
                    }

                    // System variables
                    var systemVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
                    foreach (global::System.Collections.DictionaryEntry de in systemVars)
                    {
                        vars.Add(new EnvVar(de.Key?.ToString() ?? "Unknown", de.Value?.ToString() ?? "", true));
                    }
                } catch { }
                return vars.OrderBy(v => v.Name).ToList();
            });
        }

        public async Task<bool> SetVariableAsync(string name, string value, bool isSystem)
        {
            if (isSystem)
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        // Use PowerShell with runas for elevation. 
                        string escapedValue = (value ?? "").Replace("'", "''");
                        string script = $"[Environment]::SetEnvironmentVariable('{name}', '{escapedValue}', 'Machine')";
                        
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-Command \"{script}\"",
                            Verb = "runas",
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };

                        using (var process = Process.Start(psi))
                        {
                            process?.WaitForExit();
                            return process?.ExitCode == 0;
                        }
                    }
                    catch { return false; }
                });
            }
            else
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
                        return true;
                    }
                    catch { return false; }
                });
            }
        }
    }
}