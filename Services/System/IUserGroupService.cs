using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IUserGroupService
    {
        Task<List<UserAccount>> GetUsersAsync();
        Task<List<UserGroup>> GetGroupsAsync();
        
        Task<bool> CreateUserAsync(string username, string password, string fullName, string description);
        Task<bool> DeleteUserAsync(string username);
        Task<bool> SetUserEnabledAsync(string username, bool enabled);
        Task<bool> SetUserPasswordAsync(string username, string newPassword);
        
        Task<bool> CreateGroupAsync(string groupName, string description);
        Task<bool> DeleteGroupAsync(string groupName);
        Task<bool> AddUserToGroupAsync(string username, string groupName);
        Task<bool> RemoveUserFromGroupAsync(string username, string groupName);
    }
}
