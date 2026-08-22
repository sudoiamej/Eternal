using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsKnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly List<HelpArticle> _articles;

        public WindowsKnowledgeBaseService()
        {
            _articles = new List<HelpArticle>
            {
                new HelpArticle {
                    Id = "GettingStarted",
                    Category = "System",
                    Title = "Getting Started",
                    Description = "Overview of the Eternal ecosystem.",
                    Content = "Eternal System Intelligence is a professional-grade diagnostic platform designed for high-performance system auditing. Use the sidebar to navigate between modules. Each tool is designed to work both in standard Windows and recovery environments.",
                    Icon = "Rocket"
                },
                new HelpArticle {
                    Id = "PcScanner",
                    Category = "Diagnostics",
                    Title = "PC Intelligence Scanner",
                    Description = "Deep system health audits.",
                    Content = "The PC Scanner performs a multi-tiered audit of your system. Issues are categorized into Required (Critical), Recommended (High), and Optional (Maintenance). Use 'FIX NOW' for automated resolution or 'REVIEW' for manual guidance.",
                    Icon = "Search"
                },
                new HelpArticle {
                    Id = "Storage",
                    Category = "Diagnostics",
                    Title = "Storage Management",
                    Description = "Volume and partition control.",
                    Content = "The Storage module provides advanced volume management. You can rename partitions, perform quick formats, and resize volumes. GPT/MBR conversion and Surface Sector Scans are also available for physical disk maintenance.",
                    Icon = "Database"
                },
                new HelpArticle {
                    Id = "WindowsUpdate",
                    Category = "System",
                    Title = "Windows Update",
                    Description = "Managing OS patches.",
                    Content = "Directly interface with the Windows Update Agent. Search for available updates, install specific KB articles, or pause updates for 7 days. The module also detects pending reboots and provides a 'REBOOT NOW' action.",
                    Icon = "Refresh"
                },
                new HelpArticle {
                    Id = "DismImaging",
                    Category = "Recovery",
                    Title = "DISM Image Studio",
                    Description = "Reading .wim and .esd files.",
                    Content = "Image Studio utilizes the Deployment Image Servicing and Management (DISM) tool to analyze Windows installers. It can read indices from .wim, .esd, and .swm files, displaying architecture and version metadata.",
                    Icon = "Archive"
                },
                new HelpArticle {
                    Id = "PeMode",
                    Category = "Recovery",
                    Title = "PE / Recovery Mode",
                    Description = "Emergency recovery tools.",
                    Content = "PE Mode tools are functional only when a native WinRE or PE environment is detected. It provides access to BCD Rebuild, SFC Offline, and Disk Check (Chkdsk) for repairing non-bootable systems.",
                    Icon = "Medkit"
                },
                new HelpArticle {
                    Id = "Hardware",
                    Category = "Hardware",
                    Title = "Hardware Intelligence & Sensors",
                    Description = "CPU, GPU, RAM, and Motherboard diagnostics.",
                    Content = "Inspect physical hardware architecture, 64-bit VRAM calculations, thermal sensors, and clock frequencies. Live sensors update dynamically using low-overhead telemetry.",
                    Icon = "Microchip"
                },
                new HelpArticle {
                    Id = "Display",
                    Category = "Hardware",
                    Title = "Display Architecture & Spatial Map",
                    Description = "Multi-monitor topology and resolutions.",
                    Content = "Analyze spatial monitor arrangements, EDID manufacturer data, refresh rates, HDR capabilities, and display scale factors. Drag and re-arrange spatial monitor layouts.",
                    Icon = "Desktop"
                },
                new HelpArticle {
                    Id = "Security",
                    Category = "Security",
                    Title = "System Repair & Security Audit",
                    Description = "SFC, DISM, and Security Hardening.",
                    Content = "Run DISM /RestoreHealth and SFC /Scannow component store repairs with live terminal log output. Inspect anti-tamper state, hardware breakpoint detection, and security posture.",
                    Icon = "Shield"
                },
                new HelpArticle {
                    Id = "Drivers",
                    Category = "Hardware",
                    Title = "Driver Management & Audit",
                    Description = "Device driver verification and export.",
                    Content = "Audit installed kernel and device drivers, verify digital signatures, check provider details, and export driver packages for system backup or recovery deployment.",
                    Icon = "Sliders"
                },
                new HelpArticle {
                    Id = "Services",
                    Category = "System",
                    Title = "Services & Startup Control",
                    Description = "Windows Services and autoruns.",
                    Content = "Manage Windows Background Services and startup autorun entries. Start, stop, enable, or disable background services with real-time status updates.",
                    Icon = "Cog"
                },
                new HelpArticle {
                    Id = "Registry",
                    Category = "System",
                    Title = "Registry Lexicon & System Keys",
                    Description = "Deep Windows registry inspection.",
                    Content = "Navigate system registry hives, analyze key definitions, and inspect key values with integrated safety checks and description tooltips.",
                    Icon = "Book"
                },
                new HelpArticle {
                    Id = "Network",
                    Category = "Telemetry",
                    Title = "Network Architecture & Adapters",
                    Description = "Ethernet, Wi-Fi, and active sockets.",
                    Content = "Monitor active network interfaces, IP addresses, MAC IDs, link speeds, gateway paths, and active socket connections in real-time.",
                    Icon = "Globe"
                },
                new HelpArticle {
                    Id = "Settings",
                    Category = "System",
                    Title = "Settings & Custom Calibration",
                    Description = "App scale, themes, and calibration.",
                    Content = "Adjust application scaling with automatic persistence, toggle accent themes, manage startup lockouts, and reset hardware calibration baselines.",
                    Icon = "Gear"
                }
            };
        }

        public Task<List<HelpArticle>> GetAllArticlesAsync()
        {
            return Task.FromResult(_articles.OrderBy(a => a.Category).ThenBy(a => a.Title).ToList());
        }

        public Task<List<HelpArticle>> SearchArticlesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return GetAllArticlesAsync();
            
            var results = _articles.Where(a => 
                a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                a.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            return Task.FromResult(results);
        }
    }
}
