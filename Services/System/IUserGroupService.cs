using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IUserGroupService
    {
        Task<List<UserAccount>> GetUsersAsync();
        Task<List<UserGroup>> GetGroupsAsync();
        
        Task<bool> CreateUserAsync(string username, string password, string fullName, string description, bool mustChangePassword, bool cannotChangePassword, bool passwordNeverExpires, bool disabled);
        Task<bool> DeleteUserAsync(string username);
        Task<bool> SetUserEnabledAsync(string username, bool enabled);
        Task<bool> SetUserPasswordAsync(string username, string newPassword);
        Task<bool> UnlockUserAccountAsync(string username);
        Task<bool> UpdateUserPropertiesAsync(string username, UserAccount properties);
        
        Task<bool> CreateGroupAsync(string groupName, string description);
        Task<bool> DeleteGroupAsync(string groupName);
        Task<bool> AddUserToGroupAsync(string username, string groupName);
        Task<bool> RemoveUserFromGroupAsync(string username, string groupName);
        Task<List<string>> GetGroupMembersAsync(string groupName);
    }
}
