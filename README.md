# 🌌 Eternal System Intelligence (v3.5.0 Final Release)

![Eternal Banner](Assets/logo.png)

**Eternal System Intelligence** is an advanced Windows diagnostic, recovery, tuning, and forensic intelligence workstation. Engineered for system administrators, forensic analysts, IT technicians, and power users, Eternal provides deep bare-metal hardware telemetry, automated OS repair routines, firmware security audits, and specialized offline Windows PE (WinPE) recovery capabilities—all wrapped inside a hardware-accelerated AMOLED dark UI.

---

## 🎯 System Architecture & Startup Pipeline

### 🎬 4-Stage Authentication & Transition Pipeline
When launched in standard Windows environments, Eternal executes a multi-stage startup and authentication flow inside [`LegacyMainWindow.xaml`](file:///c:/Users/sudoiamej/Desktop/Eternal/Views/LegacyMainWindow.xaml#L588):

```
┌─────────────────────────┐
│ 1. Strict Identity      │  -> AMOLED Login Overlay (Windows Hello PIN/Biometrics or Password)
└────────────┬────────────┘
             │ Credentials Verified
┌────────────▼────────────┐
│ 2. Login Fade-Out       │  -> Login Overlay Fades Out (400ms)
└────────────┬────────────┘
             │
┌────────────▼────────────┐
│ 3. AMOLED Blackout      │  -> 2.0s Clean Dark Buffer (#030305) Hides Dashboard Load
└────────────┬────────────┘
             │
┌────────────▼────────────┐
│ 4. Loading & Cross-Fade │  -> Post-Auth Loading Overlay (Logo + Hello + Progress 0-100% over 3s)
└────────────┬────────────┘     Cross-Fades directly into Main Workstation Dashboard!
             │
┌────────────▼────────────┐
│ 5. Workstation Entrance │  -> Full Access to Main Workstation Dashboard
└─────────────────────────┘
```

### ⚡ Multi-Threaded Parallel Diagnostic Engine
Rather than running diagnostic checks sequentially, Eternal's PC Intelligence Scanner leverages .NET task parallelism (`Task.WhenAll`). Multi-threaded tasks audit RAM pressure, process DLL maps, pending Windows Update reboot flags, temporary disk usage, Defender real-time protection, and UEFI Secure Boot simultaneously—reducing full diagnostic scan times from **5–8 seconds down to ~0.5s**.

### 🛟 WinPE Recovery Environment Specialization
When booted inside **Windows PE / WinRE**:
1. **PE Detection:** `OsHelper.IsWinPE()` identifies the recovery environment.
2. **Bootloader Setup:** Launches `SplashScreenWindow` on startup to pre-warm basic system services.
3. **Software Render Guard:** Forces `RenderMode.SoftwareOnly` to prevent DirectX/GPU display driver crashes on basic VESA display drivers.
4. **Auto-Authentication Bypass:** Automatically sets `IsAuthenticated = true` so recovery technicians gain immediate access to repair tools without user account prompts.
5. **Drive Mapping:** Maps system drive roots to `X:` (RAM disk) while identifying offline `C:`, `D:`, or target Windows installations.

---

## 🔬 Complete Module-by-Module Technical Reference

### 📊 1. Workstation Dashboard (`DashboardViewModel.cs`)
* **Overall System Health Rating (0–100 Scale):** Computes a composite health index based on CPU load, RAM pressure, disk SMART alerts, Defender protection state, and system file integrity. Assigns grades: `A+ Elite Workstation` (95-100), `A` (85-94), `B` (75-84), `C` (60-74), `D` (<60).
* **Real-Time Vector Telemetry Graphing:** Hardware-accelerated WPF `Polyline` and `Polygon` rendering tracking live CPU, RAM, GPU, and Disk utilization over 60-second sliding windows.
* **Quick Diagnostics Overview:** Displays active uptime (`Environment.TickCount64`), CPU model string, GPU device name, total installed RAM, and OS build version.
* **Contextual Alerts System:** Automatically flags navigation items (e.g. `Processes` or `Storage`) with amber/red alert indicators when thresholds are breached (>80% CPU/RAM).

### ⚡ 2. Parallel PC Intelligence Scanner (`PcIntelligenceViewModel.cs`)
* **Concurrent Health Audit Engine:** Executes parallel diagnostic routines (`Task.WhenAll`):
  * **Reclaimable Temp Capacity:** Recursively calculates purgeable byte volume across `%TEMP%`, `C:\Windows\Temp`, and `C:\Windows\Minidump`.
  * **Pending Windows Reboot Detection:** Audits registry keys `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending` and `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired`.
  * **Unsigned Process Audit:** Verifies Win32 PE digital signatures on running process executables via `WinVerifyTrust`.
  * **Loaded Process DLL Inspector:** Enumerates active `.dll` module handles, memory addresses, and file origins for selected PIDs via `Process.Modules`.

### 💻 3. Bare-Metal Hardware Telemetry (`HardwareViewModel.cs`)
* **CPU Telemetry:** Reads core count, logical thread count, clock frequency (MHz), L1/L2/L3 cache sizes, socket type, and hardware virtualization status (`VT-x` / `AMD-V`) via `Win32_Processor` WMI queries.
* **RAM Telemetry:** Retrieves memory stick topology (Form Factor, Speed in MT/s, Manufacturer, Part Number) via `Win32_PhysicalMemory`. Calculates active page file pressure and physical RAM consumption.
* **Motherboard & BIOS Information:** Reads board manufacturer, model product string, BIOS vendor, version, and release date via `Win32_BaseBoard` and `Win32_BIOS`.

### 🖥️ 4. Displays & GPU Topology (`DisplayViewModel.cs`)
* **GPU Architecture Telemetry:** Queries `Win32_VideoController` for GPU VRAM capacity, driver version, driver date, current resolution, and refresh rate (Hz).
* **Multi-Monitor Topology:** Enumerates connected displays, screen bounds, DPI scaling factors (100%, 125%, 150%, 200%), and primary display designations via `Screen.AllScreens`.

### 🗄️ 5. Storage & Partition Map Studio (`StorageViewModel.cs`)
* **Proportional Partition Layout Visualizer:** Dynamically calculates partition widths bound to partition size percentages on disk layouts.
* **Bare-Metal SMART Telemetry:** Queries `MSStorageDriver_FailurePredictStatus` and `MSStorageDriver_FailurePredictData` WMI classes to read raw SMART attributes (Power-On Hours, Reallocated Sector Count, Wear Level Index, Temperature, Predict Failure boolean).
* **VHD / VHDX Disk Utility:** Native virtual disk management via `virtdisk.dll` API interop (Attach VHD, Detach VHD, Create VHD).

### 🌐 6. Network Diagnostics & Socket Mapper (`NetworkViewModel.cs`)
* **Active Interface Monitor:** Reads link speeds (Gbps/Mbps), MAC addresses, IPv4/IPv6 addresses, subnet masks, default gateways, and DNS servers via `NetworkInterface.GetAllNetworkInterfaces()`.
* **Process Socket Port Mapper:** Uses native `GetExtendedTcpTable` and `GetExtendedUdpTable` Win32 APIs to correlate running Process PIDs directly to local port bindings, remote IP endpoints, and socket states (`LISTENING`, `ESTABLISHED`, `TIME_WAIT`).
* **Ping & Latency Tester:** Asynchronous roundtrip ping tests (`System.Net.NetworkInformation.Ping`) measuring packet loss and latency to DNS endpoints (`1.1.1.1`, `8.8.8.8`).

### ⚙️ 7. Process Intelligence & Module Inspector (`ProcessesViewModel.cs`)
* **Live Process Manager:** Lists running processes with PID, CPU %, RAM (MB), Disk I/O rates, User Session owner, and Command Line parameters.
* **Process Control:** Suspend, Resume, Kill (`Process.Kill()`), or change process CPU Affinity and Priority Class (`Realtime`, `High`, `AboveNormal`, `Normal`, `BelowNormal`, `Idle`).
* **Loaded Module Inspector:** Deep inspection of loaded `.dll` libraries per process, displaying memory base addresses, module sizes, and digital signature verification status.

### 🛠️ 8. Windows Services Manager (`ServicesViewModel.cs`)
* **Service Triage & Control:** Lists Windows Services (`Win32_Service`) with Service Name, Display Name, Current Status (`Running`, `Stopped`, `Paused`), Start Type (`Automatic`, `Manual`, `Disabled`), and Account Path.
* **Service Control Actions:** Start, Stop, Restart, or modify Service Start Type dynamically (`SetStartupType`).

### 📜 9. Windows Event Log Audit (`EventLogsViewModel.cs`)
* **System & Application Event Viewer:** Queries Windows Event Log channels (`System`, `Application`, `Security`) via `EventLogReader`.
* **Event Filtering:** Filters event entries by severity level (`Error`, `Warning`, `Information`, `Critical`), Event ID, Source provider, and timestamp range.

### 🔄 10. Windows Update Telemetry (`OsUpdateViewModel.cs`)
* **Update Service Status:** Audits `wuauserv` service state, last update search timestamp, and pending update list via Windows Update Agent API (`Microsoft.Update.Session`).
* **Update Control:** Trigger update scans, pause Windows Updates (1-35 days), or force soft-reset of Windows Update components.

### 🩺 11. Repair Center — Eternal Doctor (`RepairCenterViewModel.cs`)
Automated issue-to-fix repair routines:
* **Windows Update Soft-Reset:** Stops `wuauserv` and `BITS`, purges `%SystemRoot%\SoftwareDistribution` and `Catroot2` caches, and re-registers update DLLs.
* **Network Stack Repair:** Executes `netsh winsock reset`, `netsh int ip reset`, `ipconfig /flushdns`, and releases/renews DHCP leases.
* **System File Repair:** Executes `sfc /scannow` and `DISM /Online /Cleanup-Image /RestoreHealth` servicing commands asynchronously with real-time output logging.

### 🛡️ 12. Guardian Tuning & System Debloater (`TuningViewModel.cs`)
Centralized registry optimization engine:
* **Low-Latency Gaming Tweaks:** Configures MMCSS (Multimedia Class Scheduler Service) priority to `High` and disables TCP Nagle's algorithm (`TcpAckFrequency = 1`, `TCPNoDelay = 1`) for lower network packet queuing delay.
* **Privacy Hardening:** Disables OS telemetry collection (`AllowTelemetry = 0`), Cortana, and Edge background data collection.
* **UI Responsiveness:** Restores Windows 10 classic context menus (`HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}`) and sets `MenuShowDelay = 0`.
* **System Restore Safeguard:** Automatically creates a System Restore Point (`SystemRestore.CreateRestorePoint`) before applying registry changes.

### 🧰 13. Technician Rescue Belt (`ToolsViewModel.cs`)
Professional diagnostics and system recovery tools:
* **DISM Custom OS Flasher:** Flashes custom Windows installation images (`.wim`, `.esd`, `.swm`) onto target partitions via DISM API / CLI interop, automatically executing `bcdboot` to generate target bootloaders.
* **Offline Registry Hive Mount & Triage:** Mounts offline registry hives (`SYSTEM`, `SAM`, `SOFTWARE`) from secondary/offline partitions to `HKLM\OFFLINE_SYSTEM` to repair broken service start values or unlock user accounts.
* **DISM Driver Injector:** Recursively scans directories for OEM drivers (`.inf`) and injects them into online or offline target Windows images (`dism /Image:X:\ /Add-Driver /Driver:path /Recurse`).
* **BSOD Minidump Crash Parser:** Parses `.dmp` crash dumps using WinDbg symbols / heuristic analysis to isolate BugCheck codes (`0x000000D1`, `0x0000000A`) and identify failing driver `.sys` modules.

### 🛡️ 14. BIOS/UEFI Firmware Integrity Audit (`SecurityViewModel.cs`)
* **Secure Boot State Verification:** Audits `UEFISecureBootEnabled` enforcement for EFI bootloader signatures via `GetFirmwareEnvironmentVariable`.
* **UEFI DBX Revocation List Audit:** Validates DBX revocation database freshness to defend against BlackLotus, Baton Drop, and LogoFAIL bootkit attack vectors.
* **Firmware SetupMode Audit:** Verifies `SetupMode == 0` (User Mode), ensuring no unauthorized Platform Key (PK) or KEK injection occurred.
* **Bootkit Risk Score:** Calculates real-time firmware risk status (`PASS / SECURE`, `WARNING`, `CRITICAL_RISK`).

### ⚙️ 15. Settings & Customization (`SettingsViewModel.cs`)
* **Theme & Appearance:** Toggle between Pure AMOLED Dark mode, Neumorphic Dark mode, and Accent Colors (Cyan, Emerald, Purple, Crimson, Amber).
* **UI Scale Controls:** Adjust Font Scale (80%-130%) and Window Scale dynamically.
* **Startup Preferences:** Configure Startup Authentication enforcement, Auto-Update checks, and Navigation Pinning.

---

## 🎮 Easter Eggs, Secret Shortcuts & Dialog Windows

| Shortcut / Trigger | View / Window | Technical Details & Purpose |
|---|---|---|
| **`F11`** | **Fullscreen Toggle** | Toggles borderless Fullscreen Maximized Mode (`WindowStyle.None`, `WindowState.Maximized`). |
| **`↑ ↑ ↓ ↓ ← → ← → B A`** | **Konami Cyber Matrix HUD** | Activates green Cyber Matrix HUD overlay revealing low-level CPU vector capabilities (AVX2, AVX-512, AES-NI, RDRAND, SHA-NI). |
| **Logo Triple-Click** | **`OrbitalVisualizerWindow`** | Triple-clicking the top-left **ETERNAL** logo opens `OrbitalVisualizerWindow`, rendering CPU cores as spinning neon particle rings scaled dynamically to core load. |
| **`Ctrl + Shift + Alt + B`** | **`BsodSimulatorWindow`** | Launches `BsodSimulatorWindow`, an interactive WinDbg crash dump triage simulation analyzing BugCheck `0x000000D1`. |
| **`Ctrl + Shift + Alt + T`** | **`TestingAuthWindow`** | Opens `TestingAuthWindow` for internal diagnostics bypass. |
| **5x Click Profile Badge** | **`NetUserInspectorWindow`** | Opens `NetUserInspectorWindow` to view local SAM account telemetry, password age, expiration flags, and localgroup memberships. |
| **`Ctrl + K`** | **`CommandPaletteOverlay`** | Opens instant Command Palette overlay for rapid keyboard navigation across all modules and actions. |

---

## 📦 Build & Deployment Guide

### Prerequisites
* **OS:** Windows 10 / 11 (Build 19041+) or Windows PE 10+
* **SDK:** .NET 10.0 Windows SDK
* **Privileges:** Administrator permissions (required for DISM, Registry, and Hardware Telemetry).

### Publishing Single-File Executable
Eternal includes a pre-configured publish profile (`Properties/PublishProfiles/StandaloneRelease.pubxml`) for standalone distribution containing the bundled runtime:

```powershell
dotnet publish Eternal.csproj /p:PublishProfile=StandaloneRelease
```

The output standalone executable will be generated at `dist/Eternal.exe`.

---

© 2026 ETERNAL ANALYTICS. All Rights Reserved.
