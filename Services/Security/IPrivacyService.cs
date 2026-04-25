using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Security
{
    public interface IPrivacyService
    {
        Task<PrivacyAuditResult> RunAuditAsync();
        Task<bool> ApplyPolicyAsync(string policyId);
        Task<bool> UndoPolicyAsync(string policyId);
        Task<bool> ApplyAllHardeningAsync();
    }
}
