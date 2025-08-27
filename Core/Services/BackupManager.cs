using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Manages backup and rollback functionality for namespace conflict resolution
    /// </summary>
    public class BackupManager
    {
        private readonly string _backupDirectory;
        private readonly string _metadataFile;
        
        public BackupManager(string backupDirectory = null)
        {
            _backupDirectory = backupDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), ".kiro", "backups");
            _metadataFile = Path.Combine(_backupDirectory, "backup_metadata.json");
            
            // Ensure backup directory exists
            Directory.CreateDirectory(_backupDirectory);
        }
        
        /// <summary>
        /// Creates a backup of specified files before making changes
        /// </summary>
        /// <param name="filePaths">Files to backup</param>
        /// <param name="backupName">Name for this backup session</param>
        /// <returns>Backup session information</returns>
        public async Task<BackupSession> CreateBackupAsync(IEnumerable<string> filePaths, string backupName = null)
        {
            var session = new BackupSession
            {
                Id = Guid.NewGuid().ToString(),
                Name = backupName ?? $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}",
                CreatedAt = DateTime.Now,
                BackupDirectory = Path.Combine(_backupDirectory, $"session_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}")
            };
            
            Directory.CreateDirectory(session.BackupDirectory);
            
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    var backupInfo = await BackupFileAsync(filePath, session.BackupDirectory);
                    session.BackedUpFiles.Add(backupInfo);
                }
            }
            
            // Save session metadata
            await SaveBackupMetadataAsync(session);
            
            return session;
        }
        
        /// <summary>
        /// Creates a backup of a single file
        /// </summary>
        private async Task<BackupFileInfo> BackupFileAsync(string originalPath, string backupDirectory)
        {
            var fileInfo = new FileInfo(originalPath);
            var backupFileName = $"{fileInfo.Name}.backup";
            var backupPath = Path.Combine(backupDirectory, backupFileName);
            
            // Create subdirectories if needed
            var backupFileInfo = new FileInfo(backupPath);
            Directory.CreateDirectory(backupFileInfo.DirectoryName);
            
            // Copy the file
            await File.WriteAllBytesAsync(backupPath, await File.ReadAllBytesAsync(originalPath));
            
            return new BackupFileInfo
            {
                OriginalPath = originalPath,
                BackupPath = backupPath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                BackupCreated = DateTime.Now
            };
        }
        
        /// <summary>
        /// Restores files from a backup session
        /// </summary>
        /// <param name="sessionId">Backup session ID to restore</param>
        /// <returns>Rollback result</returns>
        public async Task<RollbackResult> RollbackAsync(string sessionId)
        {
            var result = new RollbackResult
            {
                SessionId = sessionId,
                StartTime = DateTime.Now
            };
            
            try
            {
                var session = await LoadBackupSessionAsync(sessionId);
                if (session == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Backup session {sessionId} not found";
                    return result;
                }
                
                foreach (var fileInfo in session.BackedUpFiles)
                {
                    try
                    {
                        if (File.Exists(fileInfo.BackupPath))
                        {
                            // Create directory if it doesn't exist
                            var originalFileInfo = new FileInfo(fileInfo.OriginalPath);
                            Directory.CreateDirectory(originalFileInfo.DirectoryName);
                            
                            // Restore the file
                            await File.WriteAllBytesAsync(fileInfo.OriginalPath, await File.ReadAllBytesAsync(fileInfo.BackupPath));
                            result.RestoredFiles.Add(fileInfo.OriginalPath);
                        }
                        else
                        {
                            result.FailedFiles.Add($"{fileInfo.OriginalPath}: Backup file not found at {fileInfo.BackupPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedFiles.Add($"{fileInfo.OriginalPath}: {ex.Message}");
                    }
                }
                
                result.Success = result.FailedFiles.Count == 0;
                result.EndTime = DateTime.Now;
                
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Rollback failed: {ex.Message}";
                result.EndTime = DateTime.Now;
                return result;
            }
        }
        
        /// <summary>
        /// Lists all available backup sessions
        /// </summary>
        /// <returns>List of backup sessions</returns>
        public async Task<List<BackupSession>> ListBackupSessionsAsync()
        {
            var sessions = new List<BackupSession>();
            
            if (!File.Exists(_metadataFile))
                return sessions;
                
            try
            {
                var json = await File.ReadAllTextAsync(_metadataFile);
                var metadata = JsonSerializer.Deserialize<BackupMetadata>(json);
                return metadata?.Sessions ?? new List<BackupSession>();
            }
            catch (Exception)
            {
                return sessions;
            }
        }
        
        /// <summary>
        /// Loads a specific backup session
        /// </summary>
        private async Task<BackupSession> LoadBackupSessionAsync(string sessionId)
        {
            var sessions = await ListBackupSessionsAsync();
            return sessions.FirstOrDefault(s => s.Id == sessionId);
        }
        
        /// <summary>
        /// Saves backup session metadata
        /// </summary>
        private async Task SaveBackupMetadataAsync(BackupSession session)
        {
            var sessions = await ListBackupSessionsAsync();
            sessions.Add(session);
            
            var metadata = new BackupMetadata { Sessions = sessions };
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_metadataFile, json);
        }
        
        /// <summary>
        /// Cleans up old backup sessions
        /// </summary>
        /// <param name="maxAge">Maximum age of backups to keep</param>
        /// <returns>Number of sessions cleaned up</returns>
        public async Task<int> CleanupOldBackupsAsync(TimeSpan maxAge)
        {
            var sessions = await ListBackupSessionsAsync();
            var cutoffDate = DateTime.Now - maxAge;
            var sessionsToRemove = sessions.Where(s => s.CreatedAt < cutoffDate).ToList();
            
            foreach (var session in sessionsToRemove)
            {
                try
                {
                    if (Directory.Exists(session.BackupDirectory))
                    {
                        Directory.Delete(session.BackupDirectory, true);
                    }
                    sessions.Remove(session);
                }
                catch (Exception)
                {
                    // Continue with other sessions if one fails
                }
            }
            
            // Save updated metadata
            var metadata = new BackupMetadata { Sessions = sessions };
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_metadataFile, json);
            
            return sessionsToRemove.Count;
        }
        
        /// <summary>
        /// Gets the total size of all backups
        /// </summary>
        /// <returns>Total backup size in bytes</returns>
        public async Task<long> GetTotalBackupSizeAsync()
        {
            var sessions = await ListBackupSessionsAsync();
            long totalSize = 0;
            
            foreach (var session in sessions)
            {
                if (Directory.Exists(session.BackupDirectory))
                {
                    var directoryInfo = new DirectoryInfo(session.BackupDirectory);
                    totalSize += directoryInfo.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                }
            }
            
            return totalSize;
        }
    }
    
    /// <summary>
    /// Information about a backup session
    /// </summary>
    public class BackupSession
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string BackupDirectory { get; set; } = string.Empty;
        public List<BackupFileInfo> BackedUpFiles { get; set; } = new List<BackupFileInfo>();
    }
    
    /// <summary>
    /// Information about a backed up file
    /// </summary>
    public class BackupFileInfo
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime BackupCreated { get; set; }
    }
    
    /// <summary>
    /// Result of a rollback operation
    /// </summary>
    public class RollbackResult
    {
        public string SessionId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> RestoredFiles { get; set; } = new List<string>();
        public List<string> FailedFiles { get; set; } = new List<string>();
        
        public string GetSummary()
        {
            var summary = new List<string>();
            summary.Add($"Rollback Summary for Session: {SessionId}");
            summary.Add($"Status: {(Success ? "SUCCESS" : "FAILED")}");
            summary.Add($"Duration: {(EndTime - StartTime).TotalSeconds:F2} seconds");
            summary.Add($"Restored Files: {RestoredFiles.Count}");
            
            if (FailedFiles.Count > 0)
            {
                summary.Add($"Failed Files: {FailedFiles.Count}");
                summary.Add("Failed Files Details:");
                foreach (var failure in FailedFiles)
                {
                    summary.Add($"  • {failure}");
                }
            }
            
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                summary.Add($"Error: {ErrorMessage}");
            }
            
            return string.Join(Environment.NewLine, summary);
        }
    }
    
    /// <summary>
    /// Metadata container for backup sessions
    /// </summary>
    public class BackupMetadata
    {
        public List<BackupSession> Sessions { get; set; } = new List<BackupSession>();
    }
}