using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Services
{
    public interface IPickImageService
    {
        Task<FileResult?> SelectAvatarAsync();
        Task<FileResult?> SelectImageAsync();
        Task<bool> CheckAvatarFileAsync(FileResult result);
        Task<bool> CheckImageFileAsync(FileResult result);
        bool IsImageExtension(string fileName);
        bool IsAvatarExtension(string fileName);
        string[] GetAllowedImageExtensions();
        string[] GetAllowedAvatarExtensions();
    }

    public class PickImageService : IPickImageService
    {
        // Максимальные размеры
        private const long MaxAvatarSize = 5 * 1024 * 1024; // 5 MB
        private const long MaxImageSize = 15 * 1024 * 1024; // 15 MB

        // Расширения для аватарок (только статичные)
        private readonly string[] _avatarExtensions = { ".jpg", ".jpeg", ".png" };

        // Расширения для изображений в сообщениях
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        public string[] GetAllowedImageExtensions() => _imageExtensions;
        public string[] GetAllowedAvatarExtensions() => _avatarExtensions;

        #region Select Methods

        public async Task<FileResult?> SelectAvatarAsync()
        {
            return await SelectFileAsync("Выберите аватар", _avatarExtensions, MaxAvatarSize, isAvatar: true);
        }

        public async Task<FileResult?> SelectImageAsync()
        {
            return await SelectFileAsync("Выберите изображение", _imageExtensions, MaxImageSize, isAvatar: false);
        }

        private async Task<FileResult?> SelectFileAsync(string title, string[] extensions, long maxSize, bool isAvatar)
        {
            try
            {
                FileResult? result = null;

                if (DeviceInfo.Manufacturer?.ToLower().Contains("xiaomi") == true)
                {
                    result = await PickFileForXiaomi(title, extensions);
                }
                else
                {
                    result = await PickFileStandard(title, extensions);
                }

                if (result != null)
                {
                    // Проверяем файл
                    bool isValid = isAvatar
                        ? await CheckAvatarFileAsync(result)
                        : await CheckImageFileAsync(result);

                    if (isValid) return result;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }

            await Shell.Current.DisplayAlert("Информация", "Файл не выбран", "OK");
            return null;
        }

        #endregion

        #region Pick Methods

        private async Task<FileResult?> PickFileForXiaomi(string title, string[] extensions)
        {
            try
            {
                // 1. Сначала пробуем MediaPicker
                await Task.Delay(100);
                var mediaResult = await MediaPicker.Default.PickPhotoAsync();
                if (mediaResult != null) return mediaResult;

                // 2. FilePicker с MIME types
                var fileOptions = CreatePickOptions(title, extensions);
                await Task.Delay(100);
                var fileResult = await FilePicker.Default.PickAsync(fileOptions);
                if (fileResult != null) return fileResult;

                // 3. С общим типом image/*
                await Task.Delay(100);
                var wildcardOptions = new PickOptions
                {
                    PickerTitle = title,
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.Android, new[] { "image/*", "application/octet-stream" } }
                        })
                };
                return await FilePicker.Default.PickAsync(wildcardOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выбора файла на Xiaomi: {ex.Message}");
                return null;
            }
        }

        private async Task<FileResult?> PickFileStandard(string title, string[] extensions)
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.RequestAsync<Permissions.StorageRead>();
                if (status != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Разрешение требуется",
                        "Необходимо разрешение на доступ к хранилищу для выбора изображения", "OK");
                    return null;
                }
            }

            var options = CreatePickOptions(title, extensions);
            return await FilePicker.Default.PickAsync(options);
        }

        private PickOptions CreatePickOptions(string title, string[] extensions)
        {
            return new PickOptions
            {
                PickerTitle = title,
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, GetAndroidMimeTypes(extensions) },
                        { DevicePlatform.iOS, GetIosUTIs(extensions) },
                        { DevicePlatform.WinUI, extensions }
                    })
            };
        }

        private IEnumerable<string> GetAndroidMimeTypes(string[] extensions)
        {
            var mimeTypes = new HashSet<string>();

            foreach (var ext in extensions)
            {
                var mime = ext.ToLower() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => null
                };

                if (mime != null) mimeTypes.Add(mime);
            }

            // Добавляем общие типы
            mimeTypes.Add("application/octet-stream");

            return mimeTypes.ToList();
        }

        private IEnumerable<string> GetIosUTIs(string[] extensions)
        {
            var utis = new HashSet<string>();

            foreach (var ext in extensions)
            {
                var uti = ext.ToLower() switch
                {
                    ".jpg" or ".jpeg" => "public.jpeg",
                    ".png" => "public.png",
                    ".gif" => "com.compuserve.gif",
                    _ => null
                };

                if (uti != null) utis.Add(uti);
            }

            utis.Add("public.image");

            return utis.ToList();
        }

        #endregion

        #region Check Methods

        public async Task<bool> CheckAvatarFileAsync(FileResult result)
        {
            return await CheckFileAsync(result, _avatarExtensions, MaxAvatarSize, "аватара");
        }

        public async Task<bool> CheckImageFileAsync(FileResult result)
        {
            return await CheckFileAsync(result, _imageExtensions, MaxImageSize, "изображения");
        }

        private async Task<bool> CheckFileAsync(FileResult result, string[] allowedExtensions, long maxSize, string fileTypeName)
        {
            try
            {
                // Проверяем размер файла
                var fileInfo = new FileInfo(result.FullPath);
                if (fileInfo.Exists && fileInfo.Length > maxSize)
                {
                    var sizeInMb = maxSize / (1024 * 1024);
                    await Shell.Current.DisplayAlert(
                        "Ошибка",
                        $"Размер {fileTypeName} не должен превышать {sizeInMb} MB",
                        "OK");
                    return false;
                }

                // Проверяем расширение
                var extension = Path.GetExtension(result.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    var extensionsList = string.Join(", ", allowedExtensions.Select(e => e.ToUpper().TrimStart('.')));
                    await Shell.Current.DisplayAlert(
                        "Ошибка",
                        $"Поддерживаются форматы: {extensionsList}",
                        "OK");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка проверки файла: {ex.Message}", "OK");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        public bool IsImageExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return _imageExtensions.Contains(ext);
        }

        public bool IsAvatarExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return _avatarExtensions.Contains(ext);
        }

        #endregion
    }
}
