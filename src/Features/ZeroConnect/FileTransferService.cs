using System.IO;

namespace ZeroMix.Features.ZeroConnect;

public class FileTransferService
{
   private readonly string _saveDirectory;
   private readonly FileHistoryService? _history;

   public FileTransferService()
   {
         _saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
           "Downloads", "ZeroConnect");
         Directory.CreateDirectory(_saveDirectory);
   }

   public FileTransferService(string saveDirectory, FileHistoryService? history = null)
   {
        _saveDirectory = saveDirectory;
        Directory.CreateDirectory(_saveDirectory);
        _history = history;
   }
  public async Task<string> SaveAsync(string fileName, byte[] data)
      {
          // Reject oversized files
          const long MaxBytes = 50L * 1024 * 1024; // 50MB
          if (data?.LongLength > MaxBytes)
              throw new InvalidOperationException("File size exceeds 50MB limit.");

          // Strip any path components that may be present
          var fileOnly = Path.GetFileName(fileName ?? "unnamed");
          var safeNameCore = string.Concat(
              Path.GetFileNameWithoutExtension(fileOnly)
                  .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
          var ext = Path.GetExtension(fileOnly);
          var candidate = Path.Combine(_saveDirectory, $"{safeNameCore}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");

          var fullPath = Path.GetFullPath(candidate);
          // Prevent path traversal: ensure fullPath is within configured save directory
          var saveDirFull = Path.GetFullPath(_saveDirectory);
          if (!fullPath.StartsWith(saveDirFull, StringComparison.OrdinalIgnoreCase))
              throw new UnauthorizedAccessException("Invalid file path.");

          await File.WriteAllBytesAsync(fullPath, data);

          // record to history
          try
          {
              if (_history != null)
              {
                  var entry = new FileHistoryEntry
                  {
                      FileName = Path.GetFileName(fullPath),
                      Path = fullPath,
                      Direction = "received",
                      SizeBytes = data.LongLength,
                      Timestamp = DateTime.Now
                  };
                  await _history.AddEntryAsync(entry);
              }
          }
          catch { }

          return fullPath;
      }
}