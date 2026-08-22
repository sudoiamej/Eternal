# Eternal System Intelligence (v3.5.0 Release)

![Eternal Banner](Assets/logo.png)

**Eternal System Intelligence** is a Windows diagnostic, recovery, and optimization suite. Built for power users, system administrators, and forensic experts, it provides deep hardware telemetry, proactive system maintenance tools, and offline PE recovery systems.

## 🚀 Key Features

### 🔒 Strict Authentication & 3-Stage Cinematic Boot Sequence
* **Stage 1 - Strict Identity Verification:** On application startup, `LegacyMainWindow` opens immediately displaying the AMOLED Strict Login screen. Authenticates identity via native Windows Hello (PIN, Fingerprint, Facial Recognition) or Windows Password. Zero emergency bypasses or backdoors.
* **Stage 2 - Integrated Telemetry Initializer Splash:** Upon successful login, the screen transitions smoothly inside the *same window* to the post-auth telemetry splash initializer featuring a glowing pulsing logo, `"Hello, {Username}!"` greeting, step-by-step diagnostic text, and a smooth 0% $\rightarrow$ 100% progress bar.
* **Stage 3 - Workstation Dashboard Entrance:** Once progress reaches 100%, the initialization overlay smoothly fades out as the main workstation dashboard emerges over a 1s smooth fade-in transition.

### 🕵️ 5-Click User Profile "Net User" Telemetry Inspector (NEW in v3.5.0)
* **Interactive Profile Easter Egg:** Rapidly clicking the upper-right `"Hello, {Username}!"` user profile badge **5 times** opens the custom AMOLED **`NetUserInspectorWindow`** subwindow.
* **SAM Database Telemetry:** Displays Account Active status, Full Name, Password Age, Password Expiration, Password Required, Last Logon Timestamp, and Local Group Membership pills (`*Administrators`, `*Users`).
* **Raw CLI Output & Clipboard Utility:** View raw `net user` terminal stdout with an instant **Copy Raw Telemetry** action button.

### 🎮 Diagnostic Easter Eggs & Secret Shortcuts (NEW in v3.5.0)
* **🕹️ Konami Code (`↑ ↑ ↓ ↓ ← → ← → B A`):** Typing the Konami Code anywhere in the application toggles **Cyber Matrix Vector HUD Mode**, rendering green matrix styling and revealing low-level CPU vector capabilities (AVX2, AVX-512, AES-NI, RDRAND, SHA-NI).
* **🌀 Logo Triple-Click:** Triple-clicking the top-left **ETERNAL** logo opens the **`OrbitalVisualizerWindow`** subwindow, rendering real-time CPU cores as spinning neon particle rings scaled dynamically to core load.
* **🚨 Safe BSOD Crash Simulator Drill (`Ctrl + Shift + Alt + B`):** Hotkey shortcut launching **`BsodSimulatorWindow`**, an interactive WinDbg crash dump triage drill analyzing BugCheck `0x000000D1` and isolating failing driver `.sys` modules safely.

### ⚡ Next-Gen Parallel PC Intelligence Scanner & Health Index (NEW in v3.5.0)
* **10x Faster Multi-Threaded Engine:** Replaced sequential scan loops with concurrent background tasks (`Task.WhenAll`). Reduces full system diagnostic scan time from **5–8s down to ~0.5–0.9s**.
* **Windows Overall Health Rating Engine (0–100 Scale):** Real-time diagnostic workstation index assigning grades (`A+ Elite Workstation`, `A`, `B`, `C`, `D`).
* **Process Loaded DLL Module Inspector:** Enumerate active `.dll` binaries, memory maps, and disk origins per process PID in Process Intelligence.
* **Pending Windows Reboot Audit:** Detects pending OS reboot flags (`RebootPending`, `RebootRequired`) caused by Windows Updates or component installations.
* **Exact Reclaimable Capacity Calculation:** Recursively calculates exact MB available for immediate cleanup across `%TEMP%`, `C:\Windows\Temp`, and crash minidumps (`C:\Windows\Minidump`).
* **Dynamic Multi-Subsystem Audits:** Simultaneously evaluates partition health, Defender real-time status, unsigned running processes, BIOS firmware age, memory pressure, ping roundtrip latency, and generic driver providers.

### 🛡️ BIOS/UEFI Integrity Audit (Firmware Security)
Firmware-level security audit protecting against bootkits and unauthorized key injections.
- **Secure Boot State Verification:** Audits `UEFISecureBootEnabled` enforcement for EFI bootloader signatures.
- **UEFI DBX Revocation List Audit:** Validates DBX revocation database freshness to defend against BlackLotus, Baton Drop, and LogoFAIL bootkit attack vectors.
- **Firmware SetupMode Audit:** Verifies `SetupMode == 0` (User Mode) preventing unauthorized Platform Key (PK) or KEK injection.
- **Bootkit Risk Score:** Calculates real-time firmware risk status (`PASS / SECURE`, `WARNING`, `CRITICAL_RISK`).
* **Process Sockets Port Mapper:** Instantly correlates running PIDs with active TCP/UDP port states, local bindings, and remote connection endpoints.
* **Vector Telemetry Graphing:** Hardware-accelerated line contours and area fills (`Polyline`/`Polygon`) visualize CPU, RAM, and Disk active history over time.

### 🛠️ Professional Technician's Belt (v3.0.0)
Advanced diagnostics and system rescue tools:
- **DISM Custom OS Flasher:** Flash custom Windows images (`.wim`, `.esd`, `.swm`) directly onto any target hard drive partition and configure bootloaders (`bcdboot`) automatically.
- **Offline Registry Hive Mount & Triage:** Mount registry hives (`SYSTEM`, `SAM`, `SOFTWARE`) from offline partitions to reset driver startup parameters or unlock accounts.
- **DISM Driver Injector:** Recursively inject storage, network, or controller OEM drivers (`.inf`) into active or offline Windows installations.
- **BSOD Minidump Crash Parser:** Read `.dmp` crash dumps to instantly isolate BugCheck codes and failing third-party `.sys` driver modules.
- **Advanced User & Localgroup Triage:** Manage active status, lockout conditions, password policies, and assign local security group memberships with safety limits.

### 📊 Storage & Partition Map Studio (v3.0.0)
- **Proportional Partition Layout:** Visualizes partitions on each drive with proportional widths bound to their size percentage.
- **SMART Telemetry Diagnostics:** Retrieve bare-metal statistics (Power-On Hours, Reallocated Sector Counts, and Predict Failure alerts) directly from WMI.
- **VHD/VHDX Mount Utility:** Attach or detach virtual disks natively.

### 🛡️ Guardian Tuning (Debloater)
Optimize Windows performance and privacy with a centralized registry tweak engine.
- **Low Latency Gaming:** MMCSS High-Priority Multimedia Class Scheduler & TCP Low Latency (Disables Nagle's packet queuing algorithm).
- **Privacy:** Disable OS/Edge Telemetry and Cortana.
- **UI:** Restore Classic Context Menus and Instant Menu response.
- **Safety:** Integrated System Restore Point trigger before any changes.

### 🩺 Eternal Doctor
A problem-oriented repair center mapping common issues to automated fixes.
- **Windows Update Soft-Reset:** Stops WUAUSERV/BITS, purges `%SystemRoot%\SoftwareDistribution` & `Catroot2` caches, and resets Windows Update components safely.
- **Network Stack Auto-Repair:** Flushes DNS, resets Winsock catalog (`netsh winsock reset`), and resets IP stack settings.
- Resolve network connectivity, DNS conflicts, and corrupted OS files (SFC/DISM).

### 🛠️ PE Mode Specialization & Bootloader Repair
Automatically detects **Windows PE** environments to provide a streamlined recovery UI, including **Offline Registry Mounting** and dynamic drive detection.
- **Automated Offline BCD Repair:** Executes `bcdboot` macros to recreate damaged boot files directly onto target offline drives (`/f ALL`).
- **Safe Rendering Fallback:** Automatically forces software-only rendering (`RenderMode.SoftwareOnly`) to prevent UI startup crashes on barebones display drivers inside WinPE.
- **Self-Contained Single-File Build:** Pre-configured to build as a standalone executable containing the .NET runtime so it can run immediately in any custom WinPE image.
- **How to Publish for WinPE:** Run `dotnet publish -c Release -r win-x64 --self-contained true` to produce the ready-to-run binary.

## 🛠️ Technical Specs
- **Framework:** .NET 10.0-windows
- **UI:** Pure AMOLED Dark Mode with hardware-accelerated WPF Storyboard animations (MVVM pattern)
- **Library:** CommunityToolkit.Mvvm, LibreHardwareMonitorLib
- **Security:** Integrated Obfuscar pipeline & Win32 `advapi32.dll` LogonUser security engine

## ⚠️ Requirements
- **OS:** Windows 10/11 (Build 19041+) or WinPE 10+
- **Privileges:** Administrative rights are required for most Tuning and Repair features.

---
© 2026 ETERNAL ANALYTICS. All Rights Reserved.
