using System;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IConsoleService : IDisposable
    {
        event EventHandler<string> OutputReceived;
        event EventHandler Exited;
        bool IsRunning { get; }
        Task StartAsync(string shell, string name);
        Task SendCommandAsync(string command);
        void Stop();
    }
}
