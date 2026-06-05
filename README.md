# Eternal System Intelligence (v3.1.0 Release)

![Eternal Banner](Assets/logo.png)

**Eternal System Intelligence** is a Windows diagnostic, recovery, and optimization suite. Built for power users, system administrators, and forensic experts, it provides deep hardware telemetry, proactive system maintenance tools, and offline PE recovery systems.

## 🚀 Key Features

### 🛠️ Professional Technician's Belt (NEW in v3.0.0)
Advanced diagnostics and system rescue tools:
- **DISM Custom OS Flasher (NEW):** Flash custom Windows images (`.wim`, `.esd`, `.swm`) directly onto any target hard drive partition and configure bootloaders (`bcdboot`) automatically.
- **Offline Registry Hive Mount & Triage:** Mount registry hives (`SYSTEM`, `SAM`, `SOFTWARE`) from offline partitions to reset driver startup parameters or unlock accounts.
- **DISM Driver Injector:** Recursively inject storage, network, or controller OEM drivers (`.inf`) into active or offline Windows installations.
- **BSOD Minidump Crash Parser:** Read `.dmp` crash dumps to instantly isolate BugCheck codes and failing third-party `.sys` driver modules.
- **Advanced User & Localgroup Triage:** Manage active status, lockout conditions, password policies, and assign local security group memberships with safety limits.

### 📊 Storage & Partition Map Studio (NEW in v3.0.0)
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
- **UI:** WPF (MVVM Pattern with dual layout: Neumorphic CarPlay or classic Legacy UI, selectable directly from the Startup Lock Screen)
- **Library:** CommunityToolkit.Mvvm, LibreHardwareMonitorLib
- **Security:** Integrated Obfuscar pipeline

## ⚠️ Requirements
- **OS:** Windows 10/11 (Build 19041+) or WinPE 10+
- **Privileges:** Administrative rights are required for most Tuning and Repair features.

---
© 2026 ETERNAL ANALYTICS. All Rights Reserved.
