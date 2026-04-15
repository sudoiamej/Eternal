using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace Eternal.Models
{
    public class PCProblem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Symptom { get; set; }
        public string Category { get; set; } // Network, System, Drivers, etc.
        public IAsyncRelayCommand FixCommand { get; set; }
        public bool IsFixing { get; set; }
        public string LastResult { get; set; }
    }
}
