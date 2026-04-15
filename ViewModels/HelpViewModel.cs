using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Eternal.ViewModels
{
    public partial class HelpViewModel : ObservableObject
    {
        [ObservableProperty] private string _currentTopicTitle = "Getting Started";
        [ObservableProperty] private string _currentTopicDescription = "Basic introduction to Eternal Suite.";
        [ObservableProperty] private string _currentContent = "Eternal System Intelligence is a professional-grade diagnostic platform. Use the sidebar to navigate through various modules. Each module preloads data at startup for instant access.";

        public void ChangeTopic(string topic)
        {
            switch (topic)
            {
                case "Getting Started":
                    CurrentTopicTitle = "Getting Started";
                    CurrentTopicDescription = "Overview of the Eternal diagnostic ecosystem.";
                    CurrentContent = "Eternal provides real-time telemetry for CPU, RAM, and storage. Navigation is instant, and the Dashboard provides a high-level summary of your system's overall health and security.";
                    break;
                case "Hardware Intelligence":
                    CurrentTopicTitle = "Hardware Intelligence";
                    CurrentTopicDescription = "Deep analysis of physical components.";
                    CurrentContent = "The Hardware module retrieves model and manufacturer data for your CPU, Motherboard, and RAM. Thermal Intelligence tracks CPU temperature and power status in real-time, helping detect thermal throttling.";
                    break;
                case "Security Auditing":
                    CurrentTopicTitle = "Security Auditing";
                    CurrentTopicDescription = "System integrity and vulnerability scans.";
                    CurrentContent = "The Security module audits startup programs and Windows Defender status. The Trust Score (0-100) provides a single index of system reliability based on driver signatures and service integrity.";
                    break;
                case "Active Sentry (Network)":
                    CurrentTopicTitle = "Active Sentry (Network)";
                    CurrentTopicDescription = "Mapping live network connections.";
                    CurrentContent = "Network Intelligence displays active TCP/UDP connections and maps them to specific process IDs. This allows you to identify exactly which application is consuming bandwidth.";
                    break;
                case "PE / Recovery Mode":
                    CurrentTopicTitle = "PE / Recovery Mode";
                    CurrentTopicDescription = "Diagnostic tools for offline environments.";
                    CurrentContent = "PE Mode is designed for Windows PE or RE. Destructive recovery tools like BCD Rebuild and SFC Offline are safety-locked and only function when a native recovery environment is detected.";
                    break;
                case "FAQ & Troubleshooting":
                    CurrentTopicTitle = "FAQ & Troubleshooting";
                    CurrentTopicDescription = "Resolving common system issues.";
                    CurrentContent = "If a sensor shows 'Unsupported', your hardware may not expose that specific WMI class. Ensure you are running as Administrator for full access to security and thermal telemetry.";
                    break;
            }
        }
    }
}