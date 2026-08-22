# Eternal System Intelligence (v3.5.0 Release)

![Eternal Banner](Assets/logo.png)

**Eternal System Intelligence** is a Windows diagnostic, recovery, and optimization suite. Built for power users, system administrators, and forensic experts, it provides deep hardware telemetry, proactive system maintenance tools, and offline PE recovery systems.

## 🚀 Key Features

### 🛡️ Strict Workstation Identity & Multi-Tier Auth (NEW in v3.5.0)
* **Embedded AMOLED Strict Login Overlay:** Full-bleed workstation security gateway with hardware-accelerated entrance animations. Zero bypasses.
* **Multi-Factor / Multi-Tier Authentication:** Native Windows Hello (PIN, Fingerprint, Facial Recognition) via `UserConsentVerifier` and Win32 `LogonUser` credential validation (`advapi32.dll`). Seamless support for blank-password local accounts and domain credentials.
* **3-Tier LocalGroup RBAC System:** Automatic runtime audit (`WindowsPrincipal`) granting full features to Administrators, standard diagnostic controls to Standard Users, and read-only diagnostics to Guests.

### 🕵️ 5-Click User Profile "Net User" Telemetry Inspector (NEW in v3.5.0)
* **Interactive Profile Easter Egg:** Rapidly clicking the upper-right `"Hello, {Username}!"` user profile badge **5 times** opens the custom AMOLED **`NetUserInspectorWindow`** subwindow.
* **SAM Database Telemetry:** Displays Account Active status, Full Name, Password Age, Password Expiration, Password Required, Last Logon Timestamp, and Local Group Membership pills (`*Administrators`, `*Users`).
* **Raw CLI Output & Clipboard Utility:** View raw `net user` terminal stdout with an instant **Copy Raw Telemetry** action button.

### ⚡ Streamlined Calibration & Ergonomics (NEW in v3.5.0)
* **First-Run Hardware Baseline Scan:** Runs an interactive calibration wizard on new installations. Once calibration succeeds, the splash screen exits directly to the dashboard.
* **Fluid Hardware-Accelerated UI Animations:** XAML `Storyboard` entrance animations with `CubicEase` (`EasingMode.EaseOut`) curves for liquid-smooth transitions.
* **Header Ergonomics & Quick-Access:** Dedicated upper-right quick-action buttons for Settings (`Gear`) and Eternal Console (`Terminal`), with auto-saving collapsible sidebar preferences.

### 🛡️ System Hardening & Threat Intelligence
* **Anti-Reverse Engineering & VM Detection (RASP):** Execution timing audits and WMI baseboard/BIOS/GPU sweeps dynamically prevent execution in hypervisor tracing sandboxes.
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
- **Privacy:** Disable OS/Edge Telemetry and Cortana.
- **UI:** Restore Classic Context Menus and Instant Menu response.
- **Safety:** Integrated System Restore Point trigger before any changes.

### 🩺 Eternal Doctor
A problem-oriented repair center mapping common issues to automated fixes.
- Resolve network connectivity and DNS conflicts.
- Repair corrupted OS files (SFC/DISM).
- **DISM /RestoreHealth Source Repairs:** Repair system health using an active or offline WIM/ESD image edition as the local repair source context.

### 💻 Eternal Console
Integrated PowerShell environment with built-in diagnostic macros for rapid system audits.

### 🔍 Boot Architecture
Forensic enumeration of BCD (Boot Configuration Data) records, revealing hardware identifiers and boot paths.

### 🛠️ PE Mode Specialization
Automatically detects **Windows PE** environments to provide a streamlined recovery UI, including **Offline Registry Mounting** and dynamic drive detection for systems where Windows is not on the C: drive.
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
