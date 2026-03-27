using System.Threading.Tasks;
using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

[QueryProperty(nameof(NotificationId), "id")]
public partial class RepliesPage : ContentPage
{
    private const double ScrollThreshold = 500;

    private readonly IServiceProvider _serviceProvider;
    private string _notificationId;

    public string NotificationId
    {
        get => _notificationId; 
        set
        {
            _notificationId = value;
            if(Guid.TryParse(NotificationId, out Guid id))
            {
                var viewModel = _serviceProvider.GetService<RepliesViewModel>();
                viewModel.NotificationId = id;
                BindingContext = viewModel;

            }
        }
    }

    public RepliesPage(IServiceProvider serviceProvider)
	{
        _serviceProvider = serviceProvider;

		InitializeComponent();
	}

    private void OnScrollTopViewScrolled(object sender, ScrolledEventArgs e)
    {
        bool shouldTopButtonShow = e.ScrollY > ScrollThreshold;


        if (shouldTopButtonShow != scrollToTopButton.IsVisible)
        {
            if (shouldTopButtonShow)
            {
                scrollToTopButton.IsVisible = true;
                scrollToTopButton.FadeTo(0.9, 300);
                scrollToTopButton.ScaleTo(1, 300);
            }
            else
            {
                scrollToTopButton.FadeTo(0, 300).ContinueWith(t =>
                {
                    MainThread.BeginInvokeOnMainThread(() => scrollToTopButton.IsVisible = false);
                });
            }
        }
    }

    private void OnScrollBottomViewScrolled(object sender, ScrolledEventArgs e)
    {
        bool shouldBottomButtonNotShow = (e.ScrollY + MainScrollView.Height) >= contentLayout.Height - ScrollThreshold;

        if (shouldBottomButtonNotShow == scrollToBottomButton.IsVisible)
        {
            if (!shouldBottomButtonNotShow)
            {
                scrollToBottomButton.IsVisible = true;
                scrollToBottomButton.FadeTo(0.9, 300);
                scrollToBottomButton.ScaleTo(1, 300);
            }
            else
            {
                scrollToBottomButton.FadeTo(0, 300).ContinueWith(t =>
                {
                    MainThread.BeginInvokeOnMainThread(() => scrollToBottomButton.IsVisible = false);
                });
            }
        }
    }

    private async void OnScrollToTopClicked(object sender, EventArgs e)
    {
        try
        {
            await MainScrollView.ScrollToAsync(0, 0, true);

            await scrollToTopButton.FadeTo(0, 200);
            scrollToTopButton.IsVisible = false;
        }
        catch { }

    }

    private async void OnScrollToBottomClicked(object sender, EventArgs e)
    {
        try
        {
            var contentHeight = contentLayout.Height;
            await MainScrollView.ScrollToAsync(0, contentHeight, true);

            await scrollToBottomButton.FadeTo(0, 200);
            scrollToBottomButton.IsVisible = false;
        }
        catch { }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        MainScrollView.Scrolled -= OnScrollTopViewScrolled;
        MainScrollView.Scrolled += OnScrollBottomViewScrolled;

        scrollToTopButton.Clicked -= OnScrollToTopClicked;
        scrollToBottomButton.Clicked -= OnScrollToBottomClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        MainScrollView.Scrolled += OnScrollTopViewScrolled;
        MainScrollView.Scrolled += OnScrollBottomViewScrolled;

        scrollToTopButton.Clicked += OnScrollToTopClicked;
        scrollToBottomButton.Clicked += OnScrollToBottomClicked;

        if (BindingContext is RepliesViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}