using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Services
{
    public interface IPickImageService
    {
        Task<FileResult?> SelectImageAsync();
    }
    public class PickImageService : IPickImageService
    {
        public async Task<FileResult?> SelectImageAsync()
        {
            try
            {
                FileResult result = null;

                // Проверяем, если это Xiaomi устройство
                if (DeviceInfo.Manufacturer?.ToLower().Contains("xiaomi") == true)
                {
                    result = await PickImageForXiaomi();
                }
                else
                {
                    result = await PickImageStandard();
                }

                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }

            await Shell.Current.DisplayAlert("Информация", "Файл не выбран", "OK");
            return null;
        }

        private async Task<FileResult> PickImageForXiaomi()
        {
            try
            {
                // Для Xiaomi пробуем несколько подходов

                // 1. Сначала пробуем MediaPicker с задержкой
                await Task.Delay(100);
                var mediaResult = await MediaPicker.Default.PickPhotoAsync();
                if (mediaResult != null) return mediaResult;

                // 2. Пробуем FilePicker с явным указанием MIME types
                var fileOptions = new PickOptions
                {
                    PickerTitle = "Выберите изображение",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                    { DevicePlatform.Android, new[]
                        {
                            "image/png",
                            "image/jpeg",
                            "image/jpg"
                        }
                    },
                        })
                };

                await Task.Delay(100);
                var fileResult = await FilePicker.Default.PickAsync(fileOptions);
                if (fileResult != null) return fileResult;

                // 3. Пробуем снова с базовыми настройками
                await Task.Delay(100);
                var basicOptions = new PickOptions
                {
                    PickerTitle = "Выберите изображение"
                };
                return await FilePicker.Default.PickAsync(basicOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выбора изображения на Xiaomi: {ex.Message}");
                return null;
            }
        }

        private async Task<FileResult> PickImageStandard()
        {
            // Стандартная логика для других устройств
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

            var options = new PickOptions
            {
                PickerTitle = "Выберите изображение",
                FileTypes = FilePickerFileType.Images
            };

            return await FilePicker.Default.PickAsync(options);
        }
    }
}
