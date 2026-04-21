using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

public partial class CreateChannelPage : ContentPage
{
	public CreateChannelPage(CreateChannelViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}
}