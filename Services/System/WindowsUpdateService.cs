using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eternal.Services.System
{
    public class WindowsUpdateService : IUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingsService _settingsService;
        private readonly string _repoOwner = "eternal-intelligence";
        private readonly string _repoName = "eternal";
        private string? _downloadPath;

        public WindowsUpdateService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Eternal-App-Updater");
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
                var response = await _httpClient.GetFromJsonAsync<GitHubRelease>(url);

                if (response == null || string.IsNullOrEmpty(response.TagName))
                {
                    return new UpdateInfo(false, "", "", "");
                }

                var remoteVersionString = response.TagName.TrimStart('v');
                if (Version.TryParse(remoteVersionString, out var remoteVersion))
                {
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
                    
                    if (remoteVersion > currentVersion)
                    {
                        var asset = response.Assets?.Find(a => a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);
                        return new UpdateInfo(
                            true, 
                            remoteVersionString, 
                            response.Body ?? "No changelog provided.", 
                            asset?.BrowserDownloadUrl ?? ""
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            return new UpdateInfo(false, "", "", "");
        }

        public async Task<bool> DownloadUpdateAsync(string downloadUrl, IProgress<double> progress)
        {
            try
            {
                if (string.IsNullOrEmpty(downloadUrl)) return false;

                _downloadPath = Path.Combine(Path.GetTempPath(), "Eternal_Update.exe");
                
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var fileStream = new FileStream(_downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                using var downloadStream = await response.Content.ReadAsStreamAsync();

                var buffer = new byte[8192];
                var totalRead = 0L;
                int bytesRead;

                while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes != -1)
                    {
                        progress.Report((double)totalRead / totalBytes * 100);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ApplyUpdateAndRestart()
        {
            if (string.IsNullOrEmpty(_downloadPath) || !File.Exists(_downloadPath)) return;

            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExe)) return;

            var batchPath = Path.Combine(Path.GetTempPath(), "eternal_update.bat");
            var batchContent = $@"
@echo off
taskkill /f /pid {Process.GetCurrentProcess().Id} >nul 2>&1
timeout /t 1 /nobreak >nul
move /y ""{_downloadPath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""
";

            File.WriteAllText(batchPath, batchContent);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{batchPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo);
            global::System.Windows.Application.Current.Shutdown();
        }

        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }
            [JsonPropertyName("body")]
            public string? Body { get; set; }
            [JsonPropertyName("assets")]
            public List<GitHubAsset>? Assets { get; set; }
        }

        private class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            [JsonPropertyName("browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }
        }
    }
}