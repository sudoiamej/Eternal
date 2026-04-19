// --- ADD THESE PROPERTIES TO YOUR MainViewModel ---
[ObservableProperty] private bool _isTestingModeActive = false;
public ObservableCollection<NavigationItem> DevToolkitItems { get; } = new ObservableCollection<NavigationItem>();

// --- ADD THESE METHODS TO YOUR MainViewModel ---
public async void ActivateTestingMode()
{
    DeveloperEnvironment.IsTestingModeActive = true;
    IsTestingModeActive = true;
    
    // Populate Dev Toolkit
    DevToolkitItems.Clear();
    DevToolkitItems.Add(new NavigationItem("Splash Test", "Image", "TestSplash"));
    DevToolkitItems.Add(new NavigationItem("OS Guard Test", "ExclamationTriangle", "TestIncompatible"));
    DevToolkitItems.Add(new NavigationItem("Exit Testing", "SignOut", "ExitTestMode"));

    _loggingService.Log("!!! DEV TESTING MODE ACTIVATED !!!");
    await RunSelfIntegrityCheckAsync();
}

private void ExitTestingMode()
{
    DeveloperEnvironment.IsTestingModeActive = false;
    IsTestingModeActive = false;
    DevToolkitItems.Clear();
    Navigate("Dashboard");
    _loggingService.Log("Dev Testing Mode deactivated.");
}

private void TestSplashScreen()
{
    var testSplash = new Eternal.Views.SplashScreenWindow(true);
    testSplash.Show();
}

private void TestIncompatibleOS()
{
    var testIncompatible = new Eternal.Views.IncompatibilityWindow(true);
    testIncompatible.Show();
}

private async Task RunSelfIntegrityCheckAsync()
{
    _loggingService.Log("[Integrity] Starting application self-diagnostic...");
    // ... logic for WMI and Registry checks here ...
    System.Windows.MessageBox.Show("Testing Mode Active.", "Developer Environment", MessageBoxButton.OK, MessageBoxImage.Warning);
}

// --- UPDATE YOUR Navigate() SWITCH CASE ---
/*
case "TestSplash": TestSplashScreen(); break;
case "TestIncompatible": TestIncompatibleOS(); break;
case "ExitTestMode": ExitTestingMode(); break;
*/
