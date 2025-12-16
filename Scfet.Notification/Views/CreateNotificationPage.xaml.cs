using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

public partial class CreateNotificationPage : ContentPage
{
	public CreateNotificationPage(CreateNotificationViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        if(BindingContext is CreateNotificationViewModel viewModel)
		{
			await viewModel.InitializeAsync();
		}
    }
}