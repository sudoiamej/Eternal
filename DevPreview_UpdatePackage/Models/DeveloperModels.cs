namespace Eternal.Models
{
    public static class DeveloperEnvironment
    {
        public static bool IsTestingModeActive { get; set; } = false;
        public static string DevAccessPin { get; } = "7734"; 
    }
}
