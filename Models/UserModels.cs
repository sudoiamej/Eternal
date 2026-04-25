using System;
using System.Collections.Generic;

namespace Eternal.Models
{
    public class UserAccount
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsLockedOut { get; set; }
        public bool PasswordNeverExpires { get; set; }
        public bool UserCannotChangePassword { get; set; }
        public bool PasswordRequired { get; set; }
        public bool MustChangePasswordAtNextLogon { get; set; }
        public DateTime? LastLogon { get; set; }
        public string Sid { get; set; } = string.Empty;
        public List<string> Groups { get; set; } = new List<string>();
    }

    public class UserGroup
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sid { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new List<string>();
    }
}
