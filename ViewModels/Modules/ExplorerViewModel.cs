using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class ExplorerViewModel : BaseViewModel
    {
        [ObservableProperty] private string _currentPath = @"C:\";
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private ObservableCollection<FileItemModel> _items = new();
        [ObservableProperty] private ObservableCollection<FileItemModel> _filteredItems = new();
        [ObservableProperty] private FileItemModel? _selectedItem;
        [ObservableProperty] private ObservableCollection<string> _drives = new();
        [ObservableProperty] private string _selectedDrive = @"C:\";
        [ObservableProperty] private string _statusText = "Ready";

        // Properties Inspector Modal State
        [ObservableProperty] private bool _isPropertiesModalVisible = false;
        [ObservableProperty] private string _inspectName = string.Empty;
        [ObservableProperty] private string _inspectPath = string.Empty;
        [ObservableProperty] private string _inspectSize = string.Empty;
        [ObservableProperty] private string _inspectType = string.Empty;
        [ObservableProperty] private string _inspectCreated = string.Empty;
        [ObservableProperty] private string _inspectModified = string.Empty;
        [ObservableProperty] private string _inspectAttributes = string.Empty;
        [ObservableProperty] private string _inspectSha256 = "Computing...";
        [ObservableProperty] private string _inspectMd5 = "Computing...";
        [ObservableProperty] private string _inspectPublisher = "N/A";
        [ObservableProperty] private string _inspectVersion = "N/A";
        [ObservableProperty] private string _inspectOwner = "Unknown";
        [ObservableProperty] private string _inspectAclPermissions = "Loading...";
        // Hex Inspector Modal State
        [ObservableProperty] private bool _isHexModalVisible = false;
        [ObservableProperty] private ObservableCollection<HexRowModel> _hexRows = new();
        [ObservableProperty] private string _hexFileHeaderInfo = string.Empty;
        [ObservableProperty] private bool _isContentSearchMode = false;

        private readonly Stack<string> _backStack = new();
        private readonly Stack<string> _forwardStack = new();

        public ExplorerViewModel()
        {
            LoadDrives();
            _ = NavigateToPathInternalAsync(@"C:\", recordHistory: false);
        }

        public void LoadDrives()
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => d.Name)
                    .ToList();
                Drives = new ObservableCollection<string>(drives);
                if (Drives.Contains(@"C:\")) SelectedDrive = @"C:\";
                else if (Drives.Count > 0) SelectedDrive = Drives[0];
            }
            catch { }
        }

        partial void OnSelectedDriveChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && value != CurrentPath)
            {
                _ = NavigateToPathAsync(value);
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilter();
        }

        [RelayCommand]
        public Task NavigateToPathAsync(string targetPath)
        {
            return NavigateToPathInternalAsync(targetPath, recordHistory: true);
        }

        public async Task NavigateToPathInternalAsync(string targetPath, bool recordHistory = true)
        {
            if (string.IsNullOrWhiteSpace(targetPath) || !Directory.Exists(targetPath))
            {
                StatusText = $"Directory not found: {targetPath}";
                return;
            }

            try
            {
                if (recordHistory && CurrentPath != targetPath)
                {
                    _backStack.Push(CurrentPath);
                    _forwardStack.Clear();
                }

                CurrentPath = targetPath;
                StatusText = $"Scanning {CurrentPath}...";

                var itemList = await Task.Run(() => GetDirectoryItems(CurrentPath));

                Items = new ObservableCollection<FileItemModel>(itemList);
                ApplyFilter();
                StatusText = $"{FilteredItems.Count} item(s) in {CurrentPath}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error accessing directory: {ex.Message}";
            }
        }

        [RelayCommand]
        public void NavigateBack()
        {
            if (_backStack.Count > 0)
            {
                _forwardStack.Push(CurrentPath);
                string prev = _backStack.Pop();
                _ = NavigateToPathInternalAsync(prev, recordHistory: false);
            }
        }

        [RelayCommand]
        public void NavigateForward()
        {
            if (_forwardStack.Count > 0)
            {
                _backStack.Push(CurrentPath);
                string next = _forwardStack.Pop();
                _ = NavigateToPathInternalAsync(next, recordHistory: false);
            }
        }

        [RelayCommand]
        public void NavigateUp()
        {
            try
            {
                var parent = Directory.GetParent(CurrentPath);
                if (parent != null)
                {
                    _ = NavigateToPathAsync(parent.FullName);
                }
            }
            catch { }
        }

        [RelayCommand]
        public void Refresh()
        {
            _ = NavigateToPathInternalAsync(CurrentPath, recordHistory: false);
        }

        [RelayCommand]
        public void ExecuteOrOpenItem(FileItemModel? item)
        {
            if (item == null) return;

            if (item.IsDirectory)
            {
                _ = NavigateToPathInternalAsync(item.FullPath);
            }
            else
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = item.FullPath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    StatusText = $"Launched: {item.Name}";
                }
                catch (Exception ex)
                {
                    StatusText = $"Could not open {item.Name}: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void OpenTerminalHere()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k cd /d \"{CurrentPath}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch { }
        }

        [RelayCommand]
        public void CopyPath(FileItemModel? item)
        {
            string target = item != null ? item.FullPath : CurrentPath;
            try
            {
                System.Windows.Clipboard.SetText(target);
                StatusText = $"Copied to clipboard: {target}";
            }
            catch { }
        }

        [RelayCommand]
        public async Task InspectPropertiesAsync(FileItemModel? item)
        {
            if (item == null) return;

            InspectName = item.Name;
            InspectPath = item.FullPath;
            InspectSize = item.SizeFormatted;
            InspectType = item.ItemType;
            InspectCreated = "Loading...";
            InspectModified = item.DateModified.ToString("g");
            InspectAttributes = $"{(item.IsReadOnly ? "Read-Only " : "")}{(item.IsHidden ? "Hidden " : "")}{(item.IsSystem ? "System" : "")}".Trim();
            if (string.IsNullOrWhiteSpace(InspectAttributes)) InspectAttributes = "Normal";

            InspectSha256 = "Computing...";
            InspectMd5 = "Computing...";
            InspectPublisher = "N/A";
            InspectVersion = "N/A";
            InspectOwner = "Unknown";
            InspectAclPermissions = "Loading...";

            IsPropertiesModalVisible = true;

            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(item.FullPath))
                    {
                        var fi = new FileInfo(item.FullPath);
                        InspectCreated = fi.CreationTime.ToString("g");

                        // Compute Hashes if size < 100MB
                        if (fi.Length < 100 * 1024 * 1024)
                        {
                            InspectSha256 = ComputeSha256(item.FullPath);
                            InspectMd5 = ComputeMd5(item.FullPath);
                        }
                        else
                        {
                            InspectSha256 = "Skipped (File > 100MB)";
                            InspectMd5 = "Skipped (File > 100MB)";
                        }

                        // Version Info
                        var vi = FileVersionInfo.GetVersionInfo(item.FullPath);
                        InspectPublisher = string.IsNullOrWhiteSpace(vi.CompanyName) ? "N/A" : vi.CompanyName;
                        InspectVersion = string.IsNullOrWhiteSpace(vi.FileVersion) ? "N/A" : vi.FileVersion;

                        // ACL Owner & Permissions
                        var sec = fi.GetAccessControl();
                        InspectOwner = sec.GetOwner(typeof(NTAccount))?.Value ?? "Unknown";

                        var rules = sec.GetAccessRules(true, true, typeof(NTAccount));
                        var summary = new List<string>();
                        foreach (FileSystemAccessRule rule in rules)
                        {
                            summary.Add($"{rule.IdentityReference.Value}: {rule.FileSystemRights} ({rule.AccessControlType})");
                        }
                        InspectAclPermissions = string.Join("\n", summary.Take(5));
                    }
                    else if (Directory.Exists(item.FullPath))
                    {
                        var di = new DirectoryInfo(item.FullPath);
                        InspectCreated = di.CreationTime.ToString("g");
                        InspectSha256 = "N/A (Directory)";
                        InspectMd5 = "N/A (Directory)";

                        var sec = di.GetAccessControl();
                        InspectOwner = sec.GetOwner(typeof(NTAccount))?.Value ?? "Unknown";

                        var rules = sec.GetAccessRules(true, true, typeof(NTAccount));
                        var summary = new List<string>();
                        foreach (FileSystemAccessRule rule in rules)
                        {
                            summary.Add($"{rule.IdentityReference.Value}: {rule.FileSystemRights} ({rule.AccessControlType})");
                        }
                        InspectAclPermissions = string.Join("\n", summary.Take(5));
                    }
                }
                catch (Exception ex)
                {
                    InspectAclPermissions = $"Permission Audit Error: {ex.Message}";
                }
            });
        }

        [RelayCommand]
        public void ClosePropertiesModal()
        {
            IsPropertiesModalVisible = false;
        }

        [RelayCommand]
        public async Task InspectHexAsync(FileItemModel? item)
        {
            if (item == null || item.IsDirectory || !File.Exists(item.FullPath)) return;

            HexFileHeaderInfo = $"{item.Name} ({item.SizeFormatted})";
            HexRows.Clear();
            IsHexModalVisible = true;

            await Task.Run(() =>
            {
                try
                {
                    byte[] buffer = new byte[2048]; // Read first 2KB for hex view
                    int bytesRead = 0;
                    using (var fs = File.OpenRead(item.FullPath))
                    {
                        bytesRead = fs.Read(buffer, 0, buffer.Length);
                    }

                    var rows = new List<HexRowModel>();
                    for (int i = 0; i < bytesRead; i += 16)
                    {
                        int length = Math.Min(16, bytesRead - i);
                        string offsetStr = i.ToString("X8");
                        var hexParts = new List<string>();
                        var charParts = new List<char>();

                        for (int j = 0; j < length; j++)
                        {
                            byte b = buffer[i + j];
                            hexParts.Add(b.ToString("X2"));
                            charParts.Add(b >= 32 && b <= 126 ? (char)b : '.');
                        }

                        rows.Add(new HexRowModel
                        {
                            Offset = offsetStr,
                            HexBytes = string.Join(" ", hexParts).PadRight(47),
                            AsciiString = new string(charParts.ToArray())
                        });
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        HexRows = new ObservableCollection<HexRowModel>(rows);
                    });
                }
                catch (Exception ex)
                {
                    StatusText = $"Hex Read Error: {ex.Message}";
                }
            });
        }

        [RelayCommand]
        public void CloseHexModal()
        {
            IsHexModalVisible = false;
        }

        [RelayCommand]
        public async Task CompressToZipAsync(FileItemModel? item)
        {
            if (item == null) return;
            string targetZip = item.FullPath + ".zip";
            StatusText = $"Compressing {item.Name} to Zip...";

            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(targetZip)) File.Delete(targetZip);

                    if (item.IsDirectory)
                    {
                        System.IO.Compression.ZipFile.CreateFromDirectory(item.FullPath, targetZip);
                    }
                    else
                    {
                        using var zip = System.IO.Compression.ZipFile.Open(targetZip, System.IO.Compression.ZipArchiveMode.Create);
                        zip.CreateEntryFromFile(item.FullPath, item.Name);
                    }

                    StatusText = $"Created archive: {Path.GetFileName(targetZip)}";
                    _ = NavigateToPathInternalAsync(CurrentPath, recordHistory: false);
                }
                catch (Exception ex)
                {
                    StatusText = $"Zip Error: {ex.Message}";
                }
            });
        }

        [RelayCommand]
        public async Task ExtractZipAsync(FileItemModel? item)
        {
            if (item == null || item.IsDirectory || !item.FullPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return;

            string extractDir = Path.Combine(CurrentPath, Path.GetFileNameWithoutExtension(item.Name));
            StatusText = $"Extracting {item.Name}...";

            await Task.Run(() =>
            {
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(item.FullPath, extractDir, overwriteFiles: true);
                    StatusText = $"Extracted to: {Path.GetFileName(extractDir)}";
                    _ = NavigateToPathInternalAsync(CurrentPath, recordHistory: false);
                }
                catch (Exception ex)
                {
                    StatusText = $"Extraction Error: {ex.Message}";
                }
            });
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredItems = new ObservableCollection<FileItemModel>(Items);
            }
            else
            {
                var query = SearchQuery.ToLower();
                var filtered = Items.Where(i => i.Name.ToLower().Contains(query) || i.Extension.ToLower().Contains(query)).ToList();
                FilteredItems = new ObservableCollection<FileItemModel>(filtered);
            }
        }

        private List<FileItemModel> GetDirectoryItems(string path)
        {
            var result = new List<FileItemModel>();
            try
            {
                var dirInfo = new DirectoryInfo(path);

                // Add Subdirectories
                foreach (var dir in dirInfo.GetDirectories())
                {
                    try
                    {
                        result.Add(new FileItemModel
                        {
                            Name = dir.Name,
                            FullPath = dir.FullName,
                            IsDirectory = true,
                            SizeBytes = 0,
                            SizeFormatted = "<DIR>",
                            DateModified = dir.LastWriteTime,
                            Extension = "Folder",
                            IconName = "Folder",
                            IconColor = "#00E5FF",
                            IsHidden = (dir.Attributes & FileAttributes.Hidden) != 0,
                            IsSystem = (dir.Attributes & FileAttributes.System) != 0,
                            IsReadOnly = (dir.Attributes & FileAttributes.ReadOnly) != 0,
                            ItemType = "File folder"
                        });
                    }
                    catch { }
                }

                // Add Files
                foreach (var file in dirInfo.GetFiles())
                {
                    try
                    {
                        string ext = file.Extension.ToLower();
                        string icon = GetIconForExtension(ext);
                        string iconColor = GetColorForExtension(ext);

                        result.Add(new FileItemModel
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            IsDirectory = false,
                            SizeBytes = file.Length,
                            SizeFormatted = FormatBytes(file.Length),
                            DateModified = file.LastWriteTime,
                            Extension = ext,
                            IconName = icon,
                            IconColor = iconColor,
                            IsHidden = (file.Attributes & FileAttributes.Hidden) != 0,
                            IsSystem = (file.Attributes & FileAttributes.System) != 0,
                            IsReadOnly = (file.Attributes & FileAttributes.ReadOnly) != 0,
                            ItemType = string.IsNullOrWhiteSpace(ext) ? "File" : $"{ext.TrimStart('.').ToUpper()} File"
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return result;
        }

        private string GetIconForExtension(string ext)
        {
            return ext switch
            {
                ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1" => "Cog",
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "FileArchiveOutline",
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".svg" => "FileImageOutline",
                ".pdf" or ".doc" or ".docx" or ".txt" or ".md" => "FileTextOutline",
                ".mp3" or ".wav" or ".flac" or ".aac" => "FileAudioOutline",
                ".mp4" or ".mkv" or ".avi" or ".mov" => "FileVideoOutline",
                ".cs" or ".js" or ".ts" or ".html" or ".css" or ".json" or ".xml" => "FileCodeOutline",
                _ => "FileOutline"
            };
        }

        private string GetColorForExtension(string ext)
        {
            return ext switch
            {
                ".exe" or ".msi" or ".bat" or ".ps1" => "#10B981",
                ".zip" or ".rar" or ".7z" => "#F59E0B",
                ".png" or ".jpg" or ".jpeg" => "#00E5FF",
                ".pdf" or ".doc" or ".txt" => "#3B82F6",
                ".cs" or ".js" or ".json" => "#EC4899",
                _ => "#888896"
            };
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F2} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        private string ComputeSha256(string filePath)
        {
            try
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
            catch (Exception ex)
            {
                return $"Hash Error: {ex.Message}";
            }
        }

        private string ComputeMd5(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
            catch (Exception ex)
            {
                return $"Hash Error: {ex.Message}";
            }
        }
    }
}
