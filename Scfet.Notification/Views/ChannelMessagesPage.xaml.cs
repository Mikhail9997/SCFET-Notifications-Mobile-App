using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Scfet.Notification.Messages;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

[QueryProperty(nameof(ChannelId), "channelId")]
public partial class ChannelMessagesPage : ContentPage, IRecipient<ScrollToBottomMessage>
{
    public string ChannelId { get; set; } = string.Empty;

    // Для динамической пагинации
    private const double LOAD_MORE_THRESHOLD = 100;
    private const double BOTTOM_THRESHOLD = 50;
    private bool _isLoadingMore;
    private bool _isRestoringScroll;
    private double _savedScrollPosition;

    // Для дебаунса отметки прочтения
    private IDisposable? _markReadDebouncer;
    private const int MARK_READ_DEBOUNCE_MS = 500; // Отмечаем не чаще раза в 500мс
    private const int MARK_READ_SCROLL_THRESHOLD = 200; // Отмечаем только после прокрутки на 200px

    public ChannelMessagesPage(ChannelMessagesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        if (BindingContext is not ChannelMessagesViewModel vm
            || vm.IsMessagesLoading) return;
        if (_isRestoringScroll) return;

        var scrollView = (ScrollView)sender;
        _savedScrollPosition = e.ScrollY;

        // Проверяем, достигли ли верха для подгрузки
        if (e.ScrollY <= LOAD_MORE_THRESHOLD && !_isLoadingMore && vm.HasMoreMessages)
        {
            await LoadMoreMessagesAsync(scrollView, vm, e.ScrollY);
        }

        // Проверяем, находимся ли внизу
        UpdateScrollToBottomButton(scrollView, vm, e.ScrollY);

        // Отмечаем сообщения прочитанными с дебаунсом
        DebounceMarkAsRead(vm, e.ScrollY);
    }

    private async Task LoadMoreMessagesAsync(ScrollView scrollView, ChannelMessagesViewModel vm, double oldScrollY)
    {
        _isLoadingMore = true;
        var oldContentHeight = scrollView.ContentSize.Height;

        await vm.LoadMoreMessagesAsync();

        // Восстанавливаем позицию после загрузки
        _isRestoringScroll = true;

        // Даем время на обновление UI
        await Task.Delay(50);

        var newContentHeight = scrollView.ContentSize.Height;
        var heightDifference = newContentHeight - oldContentHeight;
        var newScrollY = oldScrollY + heightDifference;

        if (newScrollY > 0)
        {
            await scrollView.ScrollToAsync(0, newScrollY, false);
        }

        _isRestoringScroll = false;
        _isLoadingMore = false;
    }

    private void UpdateScrollToBottomButton(ScrollView scrollView, ChannelMessagesViewModel vm, double scrollY)
    {
        var contentHeight = scrollView.ContentSize.Height;
        var scrollViewHeight = scrollView.Height;
        var isAtBottom = (contentHeight - scrollY - scrollViewHeight) <= BOTTOM_THRESHOLD;

        if (vm.ShowScrollToBottomButton != !isAtBottom)
        {
            vm.ShowScrollToBottomButton = !isAtBottom && contentHeight > scrollViewHeight;
        }
    }

    private double _lastMarkReadScrollY;

    private void DebounceMarkAsRead(ChannelMessagesViewModel vm, double currentScrollY)
    {
        // Проверяем, достаточно ли прокрутили для отметки
        var scrollDelta = Math.Abs(currentScrollY - _lastMarkReadScrollY);
        if (scrollDelta < MARK_READ_SCROLL_THRESHOLD) return;

        _lastMarkReadScrollY = currentScrollY;

        // Отменяем предыдущий debounce
        _markReadDebouncer?.Dispose();

        // Запускаем новый debounce
        _markReadDebouncer = Observable.Timer(TimeSpan.FromMilliseconds(MARK_READ_DEBOUNCE_MS))
            .Subscribe(async _ =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await vm.MarkVisibleMessagesAsReadAsync();
                });
            });
    }

    private async Task ScrollToBottom(bool animated = true)
    {
        await Task.Delay(50);

        // Скроллим на максимальную высоту контента
        var maxScrollY = MessagesScrollView.ContentSize.Height - MessagesScrollView.Height;

        if (maxScrollY > 0)
        {
            await MessagesScrollView.ScrollToAsync(0, maxScrollY, animated);
        }
    }

    public async void Receive(ScrollToBottomMessage message)
    {
        await ScrollToBottom(message.Animated);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ChannelMessagesViewModel viewModel)
        {
            viewModel.ChannelId = ChannelId;
            await viewModel.InitializeAsync();
        }
        WeakReferenceMessenger.Default.Register(this);
        MessagesScrollView.Scrolled += OnScrollViewScrolled;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        WeakReferenceMessenger.Default.Unregister<ScrollToBottomMessage>(this);

        if (BindingContext is ChannelMessagesViewModel viewModel)
        {
            viewModel.Cleanup();
        }
        _markReadDebouncer?.Dispose();
        MessagesScrollView.Scrolled -= OnScrollViewScrolled;
    }
}