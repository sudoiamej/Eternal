using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IBiosService
    {
        Task<BiosInfo> GetBiosInfoAsync();
        Task<UefiStatus> GetUefiStatusAsync();
    }

    public record BiosInfo(string Vendor, string Version, string ReleaseDate);
    public record TpmInfo(bool IsPresent, string Version, string Manufacturer, string Status, string ManufacturerId);
    public record UefiStatus(bool IsUefi, bool SecureBootEnabled, TpmInfo Tpm);
}
