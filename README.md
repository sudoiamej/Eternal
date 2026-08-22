# Eternal System Intelligence (v3.5.0 Final Release)

![Eternal Banner](Assets/logo.png)

**Eternal System Intelligence** is an advanced Windows diagnostic, recovery, and optimization suite. Built for power users, system administrators, and forensic experts, it provides deep hardware telemetry, proactive system maintenance tools, and offline PE recovery systems.

---

## 🌟 What's New in v3.5.0 Release

### 🎬 5-Second Post-Auth Transition & Cross-Fade Flow
* **Stage 1 — AMOLED Strict Identity Screen:** Application opens with the sleek dark AMOLED login screen prefilled with active profile (`sudoiamej`). Supports native **Windows Hello PIN / Biometrics** and Windows Password. Zero backdoors.
* **Stage 2 — Login Overlay Fade-Out & 2-Second Buffer:** Upon authentication, the login overlay smoothly fades out (`400ms`), followed by a 2.0-second AMOLED dark buffer pause that hides dashboard initialization.
* **Stage 3 — 3-Second Loading Overlay & Direct Dashboard Cross-Fade:** Smoothly fades in the Post-Auth Loading screen featuring the official **`/Assets/logo.png`**, **`ETERNAL SYSTEM INTELLIGENCE`** title, **`Hello, {Username}!`** greeting, live telemetry steps, and dynamic workstation tip cards over a 3.0-second progress bar ($0\% \rightarrow 100\%$). The loading overlay then dissolves directly and cross-fades into the **Main Workstation Dashboard**!

### 🖥️ Fullscreen Support (`F11`)
* **F11 Toggle:** Press **`F11`** anywhere in the application to toggle between borderless **Fullscreen Maximized Mode** (`WindowStyle.None`, `WindowState.Maximized`) and standard windowed mode.

### 🛡️ Windows Hello PIN / Biometrics Integration
* **Native Windows 10/11 Security Prompt:** Integrates Win32 `UserConsentVerifier` to invoke the real OS biometric/PIN dialog modal. Once verified, routes seamlessly through the 2.0s buffer + 3.0s transition flow.

### 🛟 WinPE Recovery Environment Specialization
* **Auto-Authentication Bypass:** When running under **Windows PE** or WinRE (`IsPeMode == true`), user login is bypassed automatically so technicians get immediate access to the dashboard for emergency system repairs.
* **Software Render Fallback:** Forces software rendering (`RenderMode.SoftwareOnly`) in WinPE to guarantee 100% stability on basic VESA display drivers.
* **Splash Recovery Boot:** Loads `SplashScreenWindow` on startup in WinPE environments to initialize services safely.

---

## 🚀 Core Feature Highlights

### 🕵️ 5-Click User Profile "Net User" Telemetry Inspector
* **Interactive Profile Easter Egg:** Rapidly clicking the upper-right `"Hello, {Username}!"` user profile badge **5 times** opens the custom AMOLED **`NetUserInspectorWindow`** subwindow.
* **SAM Database Telemetry:** Displays Account Active status, Full Name, Password Age, Password Expiration, Password Required, Last Logon Timestamp, and Local Group Membership pills (`*Administrators`, `*Users`).
* **Raw CLI Output & Clipboard Utility:** View raw `net user` terminal stdout with an instant **Copy Raw Telemetry** action button.

### 🎮 Diagnostic Easter Eggs & Secret Shortcuts
* **🕹️ Konami Code (`↑ ↑ ↓ ↓ ← → ← → B A`):** Typing the Konami Code anywhere in the application toggles **Cyber Matrix Vector HUD Mode**, rendering green matrix styling and revealing low-level CPU vector capabilities (AVX2, AVX-512, AES-NI, RDRAND, SHA-NI).
* **🌀 Logo Triple-Click:** Triple-clicking the top-left **ETERNAL** logo opens the **`OrbitalVisualizerWindow`** subwindow, rendering real-time CPU cores as spinning neon particle rings scaled dynamically to core load.
* **🚨 Safe BSOD Crash Simulator Drill (`Ctrl + Shift + Alt + B`):** Hotkey shortcut launching **`BsodSimulatorWindow`**, an interactive WinDbg crash dump triage drill analyzing BugCheck `0x000000D1` and isolating failing driver `.sys` modules safely.

### ⚡ Parallel PC Intelligence Scanner & Health Index
* **10x Faster Multi-Threaded Engine:** Replaced sequential scan loops with concurrent background tasks (`Task.WhenAll`). Reduces full system diagnostic scan time from **5–8s down to ~0.5–0.9s**.
* **Windows Overall Health Rating Engine (0–100 Scale):** Real-time diagnostic workstation index assigning grades (`A+ Elite Workstation`, `A`, `B`, `C`, `D`).
* **Process Loaded DLL Module Inspector:** Enumerate active `.dll` binaries, memory maps, and disk origins per process PID in Process Intelligence.
* **Pending Windows Reboot Audit:** Detects pending OS reboot flags (`RebootPending`, `RebootRequired`) caused by Windows Updates or component installations.
* **Exact Reclaimable Capacity Calculation:** Recursively calculates exact MB available for immediate cleanup across `%TEMP%`, `C:\Windows\Temp`, and crash minidumps (`C:\Windows\Minidump`).

### 🛡️ BIOS/UEFI Integrity Audit (Firmware Security)
* **Secure Boot State Verification:** Audits `UEFISecureBootEnabled` enforcement for EFI bootloader signatures.
* **UEFI DBX Revocation List Audit:** Validates DBX revocation database freshness to defend against BlackLotus, Baton Drop, and LogoFAIL bootkit attack vectors.
* **Firmware SetupMode Audit:** Verifies `SetupMode == 0` (User Mode) preventing unauthorized Platform Key (PK) or KEK injection.
* **Bootkit Risk Score:** Calculates real-time firmware risk status (`PASS / SECURE`, `WARNING`, `CRITICAL_RISK`).

---

## 🛠️ Deployment & Technical Specs

- **Framework:** .NET 10.0-windows
- **UI:** Pure AMOLED Dark Mode with hardware-accelerated WPF Storyboard animations (MVVM pattern)
- **Security:** Win32 `UserConsentVerifier` Windows Hello + `advapi32.dll` LogonUser security engine
- **Publish Profile:** Pre-configured `Properties/PublishProfiles/StandaloneRelease.pubxml` for single-file self-contained deployment.

---
© 2026 ETERNAL ANALYTICS. All Rights Reserved.
