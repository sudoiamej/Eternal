using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IBiosService
    {
        Task<BiosInfo> GetBiosInfoAsync();
        Task<UefiStatus> GetUefiStatusAsync();
        Task<UefiIntegrityAudit> AuditUefiIntegrityAsync();
    }

    public record BiosInfo(string Vendor, string Version, string ReleaseDate);
    public record TpmInfo(bool IsPresent, string Version, string Manufacturer, string Status, string ManufacturerId);
    public record UefiStatus(bool IsUefi, bool SecureBootEnabled, TpmInfo Tpm);
    public record UefiIntegrityAudit(
        bool IsUefi,
        bool SecureBootEnabled,
        bool IsSetupMode,
        bool DbxUpToDate,
        string BootkitRiskLevel,
        string Summary,
        List<string> SecurityChecks
    );
}
