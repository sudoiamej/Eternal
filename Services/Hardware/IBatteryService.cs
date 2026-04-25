using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Hardware
{
    public interface IBatteryService
    {
        Task<BatteryInfo?> GetBatteryInfoAsync();
    }
}
