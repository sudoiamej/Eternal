using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Services.Hardware
{
    public interface IHardwareService
    {
        Task<CpuInfo> GetCpuInfoAsync();
        Task<GpuInfo> GetGpuInfoAsync();
        Task<RamInfo> GetRamInfoAsync();
        Task<List<DiskInfo>> GetDiskInfoAsync();
        Task<MotherboardInfo> GetMotherboardInfoAsync();
        Task<List<NetworkAdapterInfo>> GetNetworkAdaptersAsync();
        Task<List<SystemSummaryItem>> GetDetailedSystemInfoAsync();

        void StartStressTest(int threads);
        void StopStressTest();
    }

    public record SystemSummaryItem(string Category, string Property, string Value);
    public record CpuInfo(string Name, int Cores, int Threads, string Architecture, string Frequency);
    public record GpuInfo(
        string Name, 
        string DriverVersion, 
        string Vram, 
        string Utilization,
        string Temperature,
        string CoreClock,
        string MemoryClock,
        string Cores
    );
    public record RamInfo(string TotalCapacity, string Used, string Speed, int SlotsUsed, int TotalSlots);
    public record DiskInfo(string Model, string Size, string Health, string InterfaceType);
    public record MotherboardInfo(string Manufacturer, string Model);
    public record NetworkAdapterInfo(string Name, string MacAddress, string IpAddress, string Speed);
}
