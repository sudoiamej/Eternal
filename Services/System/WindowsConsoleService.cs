using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public class WindowsConsoleService : IConsoleService
    {
        private Process _process;
        public event EventHandler<string> OutputReceived;

        public Task StartAsync(string shell = "powershell.exe")
        {
            return Task.Run(() =>
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = "-NoExit -NoProfile",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                _process.OutputDataReceived += (s, e) => { if (e.Data != null) OutputReceived?.Invoke(this, e.Data); };
                _process.ErrorDataReceived += (s, e) => { if (e.Data != null) OutputReceived?.Invoke(this, "[ERROR] " + e.Data); };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
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
                    _process.Dispose();
                }
            }
            catch { }
        }
    }
}
