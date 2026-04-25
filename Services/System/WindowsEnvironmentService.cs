using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IEnvironmentService
    {
        bool IsPeMode { get; }
        string SystemDrive { get; }
        Task<List<EnvVar>> GetVariablesAsync();
        Task<bool> SetVariableAsync(string name, string value);
    }

    public record EnvVar(string Name, string Value);

    public class WindowsEnvironmentService : IEnvironmentService
    {
        public bool IsPeMode { get; }
        public string SystemDrive { get; }

        public WindowsEnvironmentService()
        {
            IsPeMode = global::System.IO.Directory.Exists(@"X:\Windows\System32") || 
                       global::System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName.StartsWith("X:", StringComparison.OrdinalIgnoreCase) == true;
            
            SystemDrive = IsPeMode ? "X:" : "C:";
        }

        public Task<List<EnvVar>> GetVariablesAsync()
        {
            return Task.Run(() =>
            {
                var vars = new List<EnvVar>();
                try
                {
                    var userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
                    foreach (global::System.Collections.DictionaryEntry de in userVars)
                    {
                        vars.Add(new EnvVar(de.Key.ToString(), de.Value?.ToString()));
                    }
                } catch { }
                return vars.OrderBy(v => v.Name).ToList();
            });
        }

        public Task<bool> SetVariableAsync(string name, string value)
        {
            return Task.Run(() =>
            {
                try
                {
                    Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
                    return true;
                } catch { return false; }
            });
        }
    }
}