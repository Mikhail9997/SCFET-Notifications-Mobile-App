using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification.ViewModels
{
    public partial class CreateChannelViewModel : BaseViewModel
    {
        private readonly IChannelApiService _channelApiService;

        public CreateChannelViewModel(IChannelApiService channelApiService)
        {
            _channelApiService = channelApiService;
        }

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        public bool CanCreate => !string.IsNullOrWhiteSpace(Name) &&
                                 Name.Length <= 100 &&
                                 Description.Length <= 500 &&
                                 !IsBusy;

        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(CanCreate));
        }
        partial void OnDescriptionChanged(string value)
        {
            OnPropertyChanged(nameof(CanCreate));
        }

        [RelayCommand]
        private async Task CreateChannelAsync()
        {
            if (!CanCreate) return;

            if (string.IsNullOrWhiteSpace(Name))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Введите название канала", "OK");
                return;
            }

            IsBusy = true;

            try
            {
                var request = new CreateChannelRequest
                {
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
                };

                var response = await _channelApiService.CreateChannelAsync(request);

                if (response?.Success == true && response.Data != null)
                {
                    await Shell.Current.DisplayAlert("Успех", $"Канал \"{response.Data.Name}\" успешно создан", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось создать канал", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            if (!string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(Description))
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "Отмена",
                    "Вы уверены? Введенные данные будут потеряны.",
                    "Да", "Нет");

                if (!confirm) return;
            }

            await Shell.Current.GoToAsync("..");
        }
    }
}
