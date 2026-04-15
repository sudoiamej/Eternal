using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IBootService
    {
        Task<List<BootRecord>> GetBootRecordsAsync();
    }
}
