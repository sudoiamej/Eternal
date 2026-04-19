using System;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IWinSatService
    {
        Task<WinSatScore?> GetCurrentScoresAsync();
        Task<(bool Success, string Message)> RunAssessmentAsync();
    }
}
