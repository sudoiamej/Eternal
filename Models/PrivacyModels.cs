using System;
using System.Collections.Generic;

namespace Eternal.Models
{
    public enum PrivacyCategory { Telemetry, Advertising, Permissions, Search }

    public class PrivacyPolicy
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PrivacyCategory Category { get; set; }
        public string RegistryPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public object HardenedValue { get; set; } = 1;
        public object DefaultValue { get; set; } = 0;
        public bool IsHardened { get; set; }
    }

    public record PrivacyAuditResult(int Score, List<PrivacyPolicy> Policies);
}
