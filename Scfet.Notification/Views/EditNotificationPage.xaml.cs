using Scfet.Notification.Services;
using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

[QueryProperty(nameof(NotificationId), "id")]
public partial class EditNotificationPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    private string _notificationId;
    public string NotificationId
    {
        get => _notificationId;
        set
        {
            _notificationId = value;
            if (Guid.TryParse(value, out Guid id))
            {
                var viewModel = _serviceProvider.GetService<EditNotificationViewModel>();
                viewModel.NotificationId = id;
                BindingContext = viewModel;
            }
        }
    }
    public EditNotificationPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
        _serviceProvider = serviceProvider;

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is EditNotificationViewModel viewModel)
		{
			await viewModel.InitializeAsync();
		}
    }
}