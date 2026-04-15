using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Services.Security
{
    public interface ISecurityService
    {
        Task<List<StartupProgram>> GetStartupProgramsAsync();
        Task<DefenderStatus> GetDefenderStatusAsync();
        Task<List<ServiceInfo>> GetRunningServicesAsync();
        Task<List<SoftwareInfo>> GetInstalledSoftwareAsync();
        Task<List<DriverSignatureInfo>> GetDriverSignaturesAsync();
        Task<REAgentStatus> GetREAgentStatusAsync();
        Task<List<BitLockerStatus>> GetBitLockerStatusAsync();
    }

    public record StartupProgram(string Name, string Path, string Location);
    public record DefenderStatus(bool RealTimeProtection, bool AntivirusEnabled);
    public record ServiceInfo(string Name, string DisplayName, string Status, string StartMode);
    public record SoftwareInfo(string Name, string Version, string Vendor);
    public record DriverSignatureInfo(string DeviceName, bool IsSigned, string Provider);
    public record REAgentStatus(bool IsEnabled, string WindowsLocation, string Identifier, string RecoveryImageLocation);
    public record BitLockerStatus(string DriveLetter, string ProtectionStatus, string EncryptionMethod, string LockStatus, string KeyProtectors);
}
