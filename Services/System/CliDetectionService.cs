using System;
using System.Collections.Generic;
using System.IO;

namespace Eternal.Services.System
{
    public class ShellDescriptor
    {
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string Icon { get; set; } = "Terminal";
        public string ColorHex { get; set; } = "#00E5FF";
    }

    public interface ICliDetectionService
    {
        List<ShellDescriptor> DetectInstalledShells();
    }

    public class CliDetectionService : ICliDetectionService
    {
        public List<ShellDescriptor> DetectInstalledShells()
        {
            var list = new List<ShellDescriptor>();

            // 1. Command Prompt (cmd.exe)
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string cmdPath = Path.Combine(sys32, "cmd.exe");
            if (File.Exists(cmdPath))
            {
                list.Add(new ShellDescriptor
                {
                    Name = "Command Prompt",
                    ExecutablePath = cmdPath,
                    Arguments = "",
                    Icon = "Terminal",
                    ColorHex = "#CCCCCC"
                });
            }

            // 2. Windows PowerShell
            string winPsPath = Path.Combine(sys32, @"WindowsPowerShell\v1.0\powershell.exe");
            if (File.Exists(winPsPath))
            {
                list.Add(new ShellDescriptor
                {
                    Name = "Windows PowerShell",
                    ExecutablePath = winPsPath,
                    Arguments = "-NoExit",
                    Icon = "Terminal",
                    ColorHex = "#0078D4"
                });
            }

            // 3. PowerShell 7 (pwsh.exe)
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pwshPath = Path.Combine(programFiles, @"PowerShell\7\pwsh.exe");
            if (File.Exists(pwshPath))
            {
                list.Add(new ShellDescriptor
                {
                    Name = "PowerShell 7",
                    ExecutablePath = pwshPath,
                    Arguments = "-NoExit",
                    Icon = "Bolt",
                    ColorHex = "#5391FE"
                });
            }
            else
            {
                string? pathPwsh = FindInPath("pwsh.exe");
                if (!string.IsNullOrEmpty(pathPwsh))
                {
                    list.Add(new ShellDescriptor
                    {
                        Name = "PowerShell 7",
                        ExecutablePath = pathPwsh,
                        Arguments = "-NoExit",
                        Icon = "Bolt",
                        ColorHex = "#5391FE"
                    });
                }
            }

            // 4. Git Bash (bash.exe)
            string gitBashPath = Path.Combine(programFiles, @"Git\bin\bash.exe");
            if (File.Exists(gitBashPath))
            {
                list.Add(new ShellDescriptor
                {
                    Name = "Git Bash",
                    ExecutablePath = gitBashPath,
                    Arguments = "--login -i",
                    Icon = "CodeFork",
                    ColorHex = "#F05032"
                });
            }
            else
            {
                string? pathBash = FindInPath("bash.exe");
                if (!string.IsNullOrEmpty(pathBash))
                {
                    list.Add(new ShellDescriptor
                    {
                        Name = "Git Bash",
                        ExecutablePath = pathBash,
                        Arguments = "--login -i",
                        Icon = "CodeFork",
                        ColorHex = "#F05032"
                    });
                }
            }

            // 5. WSL (wsl.exe)
            string wslPath = Path.Combine(sys32, "wsl.exe");
            if (File.Exists(wslPath))
            {
                list.Add(new ShellDescriptor
                {
                    Name = "WSL / Linux",
                    ExecutablePath = wslPath,
                    Arguments = "",
                    Icon = "Linux",
                    ColorHex = "#E95420"
                });
            }

            // 6. Python REPL
            string? pythonPath = FindInPath("python.exe");
            if (!string.IsNullOrEmpty(pythonPath))
            {
                list.Add(new ShellDescriptor
                {
                    Name = "Python REPL",
                    ExecutablePath = pythonPath,
                    Arguments = "",
                    Icon = "Code",
                    ColorHex = "#3776AB"
                });
            }

            // 7. Node.js REPL
            string? nodePath = FindInPath("node.exe");
            if (!string.IsNullOrEmpty(nodePath))
            {
                list.Add(new ShellDescriptor
                {
                    Name = "Node.js REPL",
                    ExecutablePath = nodePath,
                    Arguments = "",
                    Icon = "Jsfiddle",
                    ColorHex = "#68A063"
                });
            }

            return list;
        }

        private string? FindInPath(string exeName)
        {
            try
            {
                string? pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (string.IsNullOrEmpty(pathEnv)) return null;

                foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    string fullPath = Path.Combine(dir.Trim('"'), exeName);
                    if (File.Exists(fullPath)) return fullPath;
                }
            }
            catch { }
            return null;
        }
    }
}
