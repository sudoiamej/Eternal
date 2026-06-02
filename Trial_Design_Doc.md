# Design Document: Eternal Trial Version Implementation

## 1. Overview
The goal is to create a limited "Trial" version of the Eternal System Intelligence suite. This version will be a separate project/folder that uses compile-time flags to restrict access to advanced features and limit the usage of critical system utilities.

## 2. Technical Strategy
- **Distribution:** A separate project folder `Eternal-Trial`.
- **Enforcement:** Use `#if TRIAL` preprocessor directives.
- **Build Configuration:** Add `TRIAL` to `DefineConstants` in the `.csproj`.

## 3. Feature Gating (Pro vs. Basic)

### 3.1. Basic Features (Unlocked)
- Dashboard
- Hardware Telemetry (CPU, RAM, GPU, etc.)
- Display Information
- Battery Lab
- PC Rating (WINSAT)
- Thermal Monitoring
- Components Diagnostics
- BIOS / UEFI Info
- Storage / Disk Info
- Performance Monitoring
- Network Information
- Reports & System Logs
- Basic Tools (Calculator, etc.)
- Settings

### 3.2. Pro Features (Locked)
- **New Neumorphic UI:** The trial version is forced to use the "Legacy UI". The modern Neumorphic/CarPlay interface is reserved for the Pro version.
- **PC Scanner:** Deep system audit.
- **Eternal Doctor:** Automated system repairs.
- **Registry Editor:** Direct registry manipulation.
- **Guardian Tuning:** Registry-based optimizations/debloating.
- **Hardware Stress Test:** High-load benchmarking.
- **Boot Records:** BCD editing.
- **Process Intelligence:** Advanced process analysis and neutralization.
- **Sentinel Privacy:** Telemetry and log purging.
- **Services Manager:** SCM management.
- **User Accounts:** Local user/group management.
- **Security Audit:** Forensic scans and persistence tracking.
- **Drivers Manager:** Driver archiving.
- **Environment Editor:** PATH and env var editing.
- **Eternal Console:** Integrated PowerShell terminal.
- **Time Machine:** System snapshots.
- **DISM Imaging:** OS image management.
- **Windows Update:** Advanced update control.
- **PE Mode:** Windows PE specialized environment.

## 4. Usage Limits
- **Eternal Doctor:** Maximum of 3 repair actions allowed.
- **Guardian Tuning:** Maximum of 1 optimization category allowed.
- **Stress Test:** Duration capped at 30 seconds.

## 5. UI/UX Changes
- **Trial Banner:** A subtle "TRIAL VERSION" watermark in the sidebar or header.
- **Lock Icons:** Pro features in the navigation will have a lock icon.
- **Upsell Dialog:** Clicking a locked feature will show a professional dialog explaining the benefit of the Pro version.

## 6. Implementation Steps
1. **Duplicate Project:** Copy `Eternal` to `Eternal-Trial`.
2. **Setup Build Flag:** Edit `Eternal.csproj` to include `<DefineConstants>TRIAL</DefineConstants>`.
3. **Service Layer Gating:** Wrap "Pro" service registrations in `App.xaml.cs` with `#if !TRIAL`.
4. **ViewModel Gating:** Update `MainViewModel.InitializeNavigation()` to filter items based on `#if TRIAL`.
5. **Usage Tracking:** Add usage counters to `AppSettings` or a dedicated `TrialService`.
6. **Limit Enforcement:** Inject trial checks into `RepairCenterViewModel` and `TuningViewModel`.
7. **UI Polish:** Add trial indicators and lock visuals.
