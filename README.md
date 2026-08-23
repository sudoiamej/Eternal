# 🌌 Eternal System Intelligence (v3.5.5 BETA)

<p align="center">
  <img src="Assets/logo.png" alt="Eternal Logo" width="100"/>
</p>

<p align="center">
  <b>Advanced Windows Diagnostic, Recovery, Tuning & Forensic Intelligence Workstation</b><br/>
  <sub>⚠️ <b>CURRENT STATUS: BETA RELEASE — Active Development Phase</b></sub>
</p>

> [!WARNING]
> **BETA NOTICE**: Eternal System Intelligence is currently in **v3.5.5 BETA**. Features are undergoing active development, hardware telemetry calibration, and recovery testing. Expect bugs, edge-case behavior, and ongoing architectural hardening as development continues.

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
  * **Defender Real-Time Status:** Checks Windows Defender Service (`WinDefend`) status via `ServiceController`.
  * **RAM Memory Pressure:** Queries `GlobalMemoryStatusEx` via P/Invoke.
  * **System Integrity & Secure Boot:** Checks `GetFirmwareType` and Secure Boot state via WMI / Registry.

---

## 🛠️ Build & Run

### Clone Repository
```bash
git clone https://github.com/your-org/Eternal.git
cd Eternal
```

### Build Project
```bash
dotnet build Eternal.csproj
```

### Run Application
```bash
dotnet run --project Eternal.csproj
```

---

## 📜 License

Personal & Enterprise License — Eternal Workstation Ecosystem.
