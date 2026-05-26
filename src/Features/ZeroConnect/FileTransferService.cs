using System.IO;

namespace ZeroMix.Features.ZeroConnect;

public class FileTransferService
{
   private readonly string _saveDirectory;

   public FileTransferService()
   {
   }

   public FileTransferService(string saveDirectory)
   {
      _saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      "Download", "ZeroConnect"); Directory.CreateDirectory(_saveDirectory);
   }
  public async Task<string> SaveAsync(string fileName, byte[] data)
    {
        // Sanitize filename
        var safeName = string.Concat(
            Path.GetFileNameWithoutExtension(fileName)
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        var ext = Path.GetExtension(fileName);
        var fullPath = Path.Combine(_saveDirectory, $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");

        await File.WriteAllBytesAsync(fullPath, data);
        return fullPath;
    }
}