using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json;
using RevitDtools.Core.Models;
using RevitDtools.Utilities;
using RevitDtools.Core.Interfaces;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Service for checking and managing automatic updates
    /// </summary>
    public class UpdateService
    {
        private readonly string _updateCheckUrl;
        private readonly string _currentVersion;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public UpdateService(ILogger logger = null)
        {
            _logger = logger ?? Logger.Instance;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Get current version from assembly
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            
            // Update check URL (would be configured for actual deployment)
            _updateCheckUrl = "https://api.github.com/repos/revitdtools/revitdtools/releases/latest";
        }

        /// <summary>
        /// Check for available updates
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                _logger.LogInfo("Checking for updates...", "UpdateService");

                var response = await _httpClient.GetStringAsync(_updateCheckUrl);
                var releaseInfo = JsonConvert.DeserializeObject<GitHubReleaseInfo>(response);

                var result = new UpdateCheckResult
                {
                    CurrentVersion = _currentVersion,
                    LatestVersion = releaseInfo.TagName?.TrimStart('v'),
                    UpdateAvailable = IsNewerVersion(releaseInfo.TagName?.TrimStart('v'), _currentVersion),
                    ReleaseNotes = releaseInfo.Body,
                    DownloadUrl = GetInstallerDownloadUrl(releaseInfo),
                    ReleaseDate = releaseInfo.PublishedAt,
                    CheckedAt = DateTime.Now
                };

                _logger.LogInfo($"Update check completed. Current: {_currentVersion}, Latest: {result.LatestVersion}, Available: {result.UpdateAvailable}", "UpdateService");

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "UpdateService - Network error during update check");
                return new UpdateCheckResult
                {
                    CurrentVersion = _currentVersion,
                    Error = "Unable to connect to update server. Please check your internet connection.",
                    CheckedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateService - Unexpected error during update check");
                return new UpdateCheckResult
                {
                    CurrentVersion = _currentVersion,
                    Error = $"Update check failed: {ex.Message}",
                    CheckedAt = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Download and install update
        /// </summary>
        public async Task<UpdateInstallResult> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<DownloadProgress> progress = null)
        {
            try
            {
                _logger.LogInfo($"Starting update download from: {downloadUrl}", "UpdateService");

                // Create temporary directory for download
                var tempDir = Path.Combine(Path.GetTempPath(), "RevitDtools_Update");
                Directory.CreateDirectory(tempDir);

                var installerPath = Path.Combine(tempDir, "RevitDtools_Update.msi");

                // Download the installer
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                var progressPercentage = (int)((downloadedBytes * 100) / totalBytes);
                                progress?.Report(new DownloadProgress
                                {
                                    ProgressPercentage = progressPercentage,
                                    DownloadedBytes = downloadedBytes,
                                    TotalBytes = totalBytes
                                });
                            }
                        }
                    }
                }

                _logger.LogInfo($"Update downloaded successfully to: {installerPath}", "UpdateService");

                // Launch the installer
                var installResult = await LaunchInstallerAsync(installerPath);

                return new UpdateInstallResult
                {
                    Success = installResult,
                    InstallerPath = installerPath,
                    Message = installResult ? "Update installer launched successfully" : "Failed to launch update installer"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateService - Error during update download/install");
                return new UpdateInstallResult
                {
                    Success = false,
                    Error = $"Update installation failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Check if automatic updates are enabled
        /// </summary>
        public bool IsAutoUpdateEnabled()
        {
            try
            {
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                return settings?.UpdateSettings?.AutoUpdateEnabled ?? true; // Default to enabled
            }
            catch
            {
                return true; // Default to enabled if settings can't be loaded
            }
        }

        /// <summary>
        /// Get the last update check time
        /// </summary>
        public DateTime GetLastUpdateCheck()
        {
            try
            {
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                return settings?.UpdateSettings?.LastUpdateCheck ?? DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Save the last update check time
        /// </summary>
        public void SaveLastUpdateCheck(DateTime checkTime)
        {
            try
            {
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                
                if (settings.UpdateSettings == null)
                    settings.UpdateSettings = new RevitDtools.Core.Models.UpdateSettings();
                
                settings.UpdateSettings.LastUpdateCheck = checkTime;
                settingsService.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateService - Error saving last update check time");
            }
        }

        /// <summary>
        /// Check if it's time to check for updates based on settings
        /// </summary>
        public bool ShouldCheckForUpdates()
        {
            if (!IsAutoUpdateEnabled())
                return false;

            var lastCheck = GetLastUpdateCheck();
            var checkInterval = GetUpdateCheckInterval();
            
            return DateTime.Now - lastCheck >= checkInterval;
        }

        /// <summary>
        /// Get the update check interval from settings
        /// </summary>
        public TimeSpan GetUpdateCheckInterval()
        {
            try
            {
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                var intervalDays = settings?.UpdateSettings?.CheckIntervalDays ?? 7; // Default to weekly
                return TimeSpan.FromDays(intervalDays);
            }
            catch
            {
                return TimeSpan.FromDays(7); // Default to weekly
            }
        }

        /// <summary>
        /// Create uninstall information for clean removal
        /// </summary>
        public void CreateUninstallInfo()
        {
            try
            {
                var uninstallInfo = new UninstallInfo
                {
                    ProductName = "RevitDtools Enhanced",
                    Version = _currentVersion,
                    InstallDate = DateTime.Now,
                    InstallLocation = Assembly.GetExecutingAssembly().Location,
                    UninstallCommand = "msiexec /x {A1E297A6-13A1-4235-B823-3C22B01D237B}",
                    Publisher = "RevitDtools Development Team",
                    EstimatedSize = GetInstallationSize()
                };

                var uninstallPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevitDtools", "uninstall.json");

                Directory.CreateDirectory(Path.GetDirectoryName(uninstallPath));
                File.WriteAllText(uninstallPath, JsonConvert.SerializeObject(uninstallInfo, Formatting.Indented));

                _logger.LogInfo("Uninstall information created", "UpdateService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateService - Error creating uninstall information");
            }
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            if (string.IsNullOrEmpty(latestVersion) || string.IsNullOrEmpty(currentVersion))
                return false;

            try
            {
                var latest = new Version(latestVersion);
                var current = new Version(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }

        private string GetInstallerDownloadUrl(GitHubReleaseInfo releaseInfo)
        {
            // Look for MSI installer in assets
            foreach (var asset in releaseInfo.Assets ?? new GitHubAsset[0])
            {
                if (asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    return asset.BrowserDownloadUrl;
                }
            }

            return null;
        }

        private async Task<bool> LaunchInstallerAsync(string installerPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = $"/i \"{installerPath}\" /passive",
                    UseShellExecute = true,
                    Verb = "runas" // Request administrator privileges
                };

                var process = Process.Start(startInfo);
                return process != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateService - Error launching installer");
                return false;
            }
        }

        private long GetInstallationSize()
        {
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var directory = Path.GetDirectoryName(assemblyPath);
                
                long totalSize = 0;
                foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    totalSize += new FileInfo(file).Length;
                }

                return totalSize / 1024; // Return size in KB
            }
            catch
            {
                return 5000; // Default estimate: 5MB
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    #region Data Models

    public class UpdateCheckResult
    {
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public bool UpdateAvailable { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public DateTime CheckedAt { get; set; }
        public string Error { get; set; }
    }

    public class UpdateInstallResult
    {
        public bool Success { get; set; }
        public string InstallerPath { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }

    public class DownloadProgress
    {
        public int ProgressPercentage { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
    }



    public class UninstallInfo
    {
        public string ProductName { get; set; }
        public string Version { get; set; }
        public DateTime InstallDate { get; set; }
        public string InstallLocation { get; set; }
        public string UninstallCommand { get; set; }
        public string Publisher { get; set; }
        public long EstimatedSize { get; set; }
    }

    public class GitHubReleaseInfo
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("assets")]
        public GitHubAsset[] Assets { get; set; }
    }

    public class GitHubAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    #endregion
}