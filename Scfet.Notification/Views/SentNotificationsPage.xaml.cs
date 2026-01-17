using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

public partial class SentNotificationsPage : ContentPage
{
    private bool _isScrollToTopButtonVisible = false;
    private bool _isScrollToBottomVisible = false;
    private const double ScrollThreshold = 500;

    public SentNotificationsPage(SentNotificationsViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;

        MainScrollView.Scrolled += OnScrollViewScrolled;
        MainScrollView.Scrolled += OnScrollBottomViewScrolled;

        scrollToTopButton.Clicked += OnScrollToTopClicked;
        scrollToBottomButton.Clicked += OnScrollToBottomClicked;
    }

    private void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        bool shouldTopButtonShow = e.ScrollY > ScrollThreshold;


        if (shouldTopButtonShow != _isScrollToTopButtonVisible)
        {
            _isScrollToTopButtonVisible = shouldTopButtonShow;

            // Анимация появления/исчезновения
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
                    if (!_isScrollToTopButtonVisible)
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
            _isScrollToBottomVisible = !shouldBottomButtonNotShow;

            // Анимация появления/исчезновения
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

            _isScrollToTopButtonVisible = false;
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

            _isScrollToBottomVisible = false;
            await scrollToBottomButton.FadeTo(0, 200);
            scrollToBottomButton.IsVisible = false;
        } 
        catch { }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        MainScrollView.Scrolled -= OnScrollViewScrolled;
        scrollToTopButton.Clicked -= OnScrollToTopClicked;
        scrollToBottomButton.Clicked -= OnScrollToBottomClicked;
    }

    protected override async void OnAppearing()
    {
        if(BindingContext is SentNotificationsViewModel viewModel)
		{
			await viewModel.InitializeAsync();
		}
    }
}