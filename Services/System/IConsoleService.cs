using System;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IConsoleService
    {
        event EventHandler<string> OutputReceived;
        Task StartAsync(string shell = "powershell.exe");
        Task SendCommandAsync(string command);
        void Stop();
    }
}
