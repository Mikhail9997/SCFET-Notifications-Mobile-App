using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

public partial class SentNotificationsPage : ContentPage
{
	public SentNotificationsPage(SentNotificationsViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        if(BindingContext is SentNotificationsViewModel viewModel)
		{
			await viewModel.InitializeAsync();
		}
    }
}