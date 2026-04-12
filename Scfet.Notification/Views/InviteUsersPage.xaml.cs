using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

[QueryProperty(nameof(ChannelId), "channelId")]
public partial class InviteUsersPage : ContentPage
{
    private const double ScrollThreshold = 500;

    public string ChannelId { get; set; } = string.Empty;

    public InviteUsersPage(InviteUsersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
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
        await MainScrollView.ScrollToAsync(0, 0, true);
        await scrollToTopButton.FadeTo(0, 200);
        scrollToTopButton.IsVisible = false;
    }

    private async void OnScrollToBottomClicked(object sender, EventArgs e)
    {
        await MainScrollView.ScrollToAsync(0, contentLayout.Height, true);
        await scrollToBottomButton.FadeTo(0, 200);
        scrollToBottomButton.IsVisible = false;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MainScrollView.Scrolled -= OnScrollViewScrolled;
        MainScrollView.Scrolled -= OnScrollBottomViewScrolled;
        scrollToTopButton.Clicked -= OnScrollToTopClicked;
        scrollToBottomButton.Clicked -= OnScrollToBottomClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MainScrollView.Scrolled += OnScrollViewScrolled;
        MainScrollView.Scrolled += OnScrollBottomViewScrolled;
        scrollToTopButton.Clicked += OnScrollToTopClicked;
        scrollToBottomButton.Clicked += OnScrollToBottomClicked;

        if (BindingContext is InviteUsersViewModel viewModel)
        {
            viewModel.ChannelId = ChannelId;
            await viewModel.InitializeAsync();
        }
    }
}