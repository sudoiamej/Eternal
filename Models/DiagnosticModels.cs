using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace Eternal.Models
{
    public class PCProblem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Symptom { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Network, System, Drivers, etc.
        public IAsyncRelayCommand FixCommand { get; set; } = default!;
        public bool IsFixing { get; set; }
        public string LastResult { get; set; } = string.Empty;
    }
}
