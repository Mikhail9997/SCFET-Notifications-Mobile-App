using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

public partial class NotificationsPage : ContentPage
{
    private bool _isScrollButtonVisible = false;
    private const double ScrollThreshold = 500;

    public NotificationsPage(NotificationsViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;

        MainScrollView.Scrolled += OnScrollViewScrolled;
        ScrollToTopButton.Clicked += OnScrollToTopClicked;
	}

    private void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        // Показываем кнопку если прокрутили достаточно далеко
        bool shouldShow = e.ScrollY > ScrollThreshold;

        if (shouldShow != _isScrollButtonVisible)
        {
            _isScrollButtonVisible = shouldShow;

            // Анимация появления/исчезновения
            if (shouldShow)
            {
                ScrollToTopButton.IsVisible = true;
                ScrollToTopButton.FadeTo(0.9, 300);
                ScrollToTopButton.ScaleTo(1, 300);
            }
            else
            {
                ScrollToTopButton.FadeTo(0, 300).ContinueWith(t =>
                {
                    if (!_isScrollButtonVisible)
                        MainThread.BeginInvokeOnMainThread(() => ScrollToTopButton.IsVisible = false);
                });
            }
        }
    }

    private async void OnScrollToTopClicked(object sender, EventArgs e)
    {
        // Прокручиваем на самый верх
        await MainScrollView.ScrollToAsync(0, 0, true);

        // Скрываем кнопку после прокрутки
        _isScrollButtonVisible = false;
        await ScrollToTopButton.FadeTo(0, 200);
        ScrollToTopButton.IsVisible = false;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        MainScrollView.Scrolled -= OnScrollViewScrolled;
        ScrollToTopButton.Clicked -= OnScrollToTopClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is NotificationsViewModel viewModel)
		{
			await viewModel.InitializeAsync();
        }
    }
}