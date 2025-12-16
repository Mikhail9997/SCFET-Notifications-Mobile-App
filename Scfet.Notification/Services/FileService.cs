using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Services
{
    public class FileService
    {
        private readonly string _cacheDirectory;

        public FileService()
        {
            _cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "notification_images");

            if (!Directory.Exists(_cacheDirectory)) Directory.CreateDirectory(_cacheDirectory);
        }

        public async Task<string> DownloadImageToLocalFile(string imageUrl)
        {
            try
            {
                var httpClient = new HttpClient();

                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

                var localFileName = Path.Combine(_cacheDirectory, $"notification_img_{Guid.NewGuid()}.jpg");

                await File.WriteAllBytesAsync(localFileName, imageBytes);

                return localFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteCacheAsync()
        {
            if (!Directory.Exists(_cacheDirectory)) return false;

            return await Task.Run(() =>
            {
                var isAllDeleted = true;
                try
                {
                    foreach (string file in Directory.GetFiles(_cacheDirectory))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            isAllDeleted = false;
                        }

                    }
                    return isAllDeleted;
                }
                catch
                {
                    return false;
                }
            });
        }

        public long GetCacheSizeInBytes()
        {
            if (!Directory.Exists(_cacheDirectory))
                return 0;

            var files = Directory.GetFiles(_cacheDirectory);
            long totalSize = 0;
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }
            return totalSize;
        }

        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
