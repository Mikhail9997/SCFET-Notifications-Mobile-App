using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

[QueryProperty(nameof(AvatarUrl), "avatarUrl")]
public partial class AvatarsPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;
    private string _avatarUrl;

    public string AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            _avatarUrl = value;
            var viewModel = _serviceProvider.GetRequiredService<AvatarsViewModel>();
            viewModel.AvatarUrl = value;

            BindingContext = viewModel;
        }
    }

    public AvatarsPage(IServiceProvider serviceProvider)
	{
        _serviceProvider = serviceProvider;
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(BindingContext is AvatarsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}