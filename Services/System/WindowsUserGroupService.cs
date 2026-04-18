using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsUserGroupService : IUserGroupService
    {
        public Task<List<UserAccount>> GetUsersAsync()
        {
            return Task.Run(() =>
            {
                var users = new List<UserAccount>();
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                    
                    foreach (var result in searcher.FindAll())
                    {
                        if (result is UserPrincipal user)
                        {
                            users.Add(new UserAccount
                            {
                                Name = user.SamAccountName,
                                FullName = user.DisplayName ?? string.Empty,
                                Description = user.Description ?? string.Empty,
                                IsEnabled = user.Enabled ?? false,
                                IsLockedOut = user.IsAccountLockedOut(),
                                LastLogon = user.LastLogon,
                                Sid = user.Sid.ToString(),
                                Groups = user.GetGroups().Select(g => g.Name).ToList()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching users: {ex.Message}");
                }
                return users.OrderBy(u => u.Name).ToList();
            });
        }

        public Task<List<UserGroup>> GetGroupsAsync()
        {
            return Task.Run(() =>
            {
                var groups = new List<UserGroup>();
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var searcher = new PrincipalSearcher(new GroupPrincipal(context));
                    
                    foreach (var result in searcher.FindAll())
                    {
                        if (result is GroupPrincipal group)
                        {
                            groups.Add(new UserGroup
                            {
                                Name = group.SamAccountName,
                                Description = group.Description ?? string.Empty,
                                Sid = group.Sid.ToString(),
                                Members = group.GetMembers().Select(m => m.SamAccountName).ToList()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching groups: {ex.Message}");
                }
                return groups.OrderBy(g => g.Name).ToList();
            });
        }

        public Task<bool> CreateUserAsync(string username, string password, string fullName, string description)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var user = new UserPrincipal(context, username, password, true);
                    user.DisplayName = fullName;
                    user.Description = description;
                    user.Save();
                    return true;
                }
                catch { return false; }
            });
        }

        public Task<bool> DeleteUserAsync(string username)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var user = UserPrincipal.FindByIdentity(context, username);
                    if (user != null)
                    {
                        user.Delete();
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public Task<bool> SetUserEnabledAsync(string username, bool enabled)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var user = UserPrincipal.FindByIdentity(context, username);
                    if (user != null)
                    {
                        user.Enabled = enabled;
                        user.Save();
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public Task<bool> SetUserPasswordAsync(string username, string newPassword)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var user = UserPrincipal.FindByIdentity(context, username);
                    if (user != null)
                    {
                        user.SetPassword(newPassword);
                        user.Save();
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public Task<bool> CreateGroupAsync(string groupName, string description)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var group = new GroupPrincipal(context, groupName);
                    group.Description = description;
                    group.Save();
                    return true;
                }
                catch { return false; }
            });
        }

        public Task<bool> DeleteGroupAsync(string groupName)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var group = GroupPrincipal.FindByIdentity(context, groupName);
                    if (group != null)
                    {
                        group.Delete();
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public Task<bool> AddUserToGroupAsync(string username, string groupName)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var group = GroupPrincipal.FindByIdentity(context, groupName);
                    using var user = UserPrincipal.FindByIdentity(context, username);
                    if (group != null && user != null)
                    {
                        group.Members.Add(user);
                        group.Save();
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public Task<bool> RemoveUserFromGroupAsync(string username, string groupName)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Machine);
                    using var group = GroupPrincipal.FindByIdentity(context, groupName);
                    using var user = UserPrincipal.FindByIdentity(context, username);
                    if (group != null && user != null)
                    {
                        group.Members.Remove(user);
                        group.Save();
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }
    }
}
