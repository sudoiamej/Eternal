using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public class WindowsConsoleService : IConsoleService
    {
        private Process? _process;
        public event EventHandler<string>? OutputReceived;
        public event EventHandler? Exited;

        public bool IsRunning => _process != null && !_process.HasExited;

        public Task StartAsync(string shell, string name, string arguments = "", string? workingDirectory = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    string targetWorkDir = workingDirectory;
                    if (string.IsNullOrEmpty(targetWorkDir) || !Directory.Exists(targetWorkDir))
                    {
                        targetWorkDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    }
                    if (string.IsNullOrEmpty(targetWorkDir) || !Directory.Exists(targetWorkDir))
                    {
                        targetWorkDir = @"C:\";
                    }

                    string defaultArgs = arguments;
                    if (string.IsNullOrEmpty(defaultArgs) && shell.Contains("powershell", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultArgs = "-NoExit";
                    }

                    _process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = shell,
                            Arguments = defaultArgs,
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8,
                            WorkingDirectory = targetWorkDir
                        },
                        EnableRaisingEvents = true
                    };

                    _process.OutputDataReceived += (s, e) => { 
                        if (e.Data != null) {
                            Debug.WriteLine($"Console Output: {e.Data}");
                            OutputReceived?.Invoke(this, e.Data); 
                        }
                    };
                    _process.ErrorDataReceived += (s, e) => { 
                        if (e.Data != null) {
                            Debug.WriteLine($"Console Error: {e.Data}");
                            OutputReceived?.Invoke(this, e.Data); 
                        }
                    };
                    _process.Exited += (s, e) => Exited?.Invoke(this, EventArgs.Empty);

                    if (!_process.Start())
                    {
                        OutputReceived?.Invoke(this, "[FATAL] Process failed to start.");
                        return;
                    }

                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();
                }
                catch (Exception ex)
                {
                    OutputReceived?.Invoke(this, $"[FATAL] Failed to initialize shell: {ex.Message}");
                }
            });
        }

        public async Task SendCommandAsync(string command)
        {
            if (_process != null && !_process.HasExited)
            {
                await _process.StandardInput.WriteLineAsync(command);
                await _process.StandardInput.FlushAsync();
            }
        }

        public void Stop()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(true);
                }
            }
            catch { }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
