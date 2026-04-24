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
    private const double LOAD_MORE_THRESHOLD = 50;
    private const double BOTTOM_THRESHOLD = 50;
    private bool _isLoadingMore;
    private bool _isRestoringScroll;

    // Для дебаунса отметки прочтения
    private IDisposable? _markReadDebouncer;
    private const int MARK_READ_IDLE_MS = 2000; // Отмечаем через 2 секунды после остановки скролла

    // Флаг для отслеживания первого скролла (чтобы не отмечать при инициализации)
    private bool _hasScrolled;

    // Для обработки изображения
    private double _startScale;
    private double _currentScale = 1;

    public ChannelMessagesPage(ChannelMessagesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        if (BindingContext is not ChannelMessagesViewModel vm
            || vm.IsMessagesLoading || _isRestoringScroll) return;

        var scrollView = (ScrollView)sender;
        _hasScrolled = true;

        // Проверяем, достигли ли верха для подгрузки
        if (e.ScrollY <= LOAD_MORE_THRESHOLD && !_isLoadingMore && vm.HasMoreMessages)
        {
            await LoadMoreMessagesAsync(scrollView, vm, e.ScrollY);
        }

        // Проверяем, находимся ли внизу
        UpdateScrollToBottomButton(scrollView, vm, e.ScrollY);

        // Сбрасываем таймер отметки прочитанных при каждом скролле
        ResetMarkReadTimer(vm);
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

    private void ResetMarkReadTimer(ChannelMessagesViewModel vm)
    {
        // Отменяем предыдущий таймер
        _markReadDebouncer?.Dispose();

        // Если еще не было ни одного скролла, не запускаем таймер
        if (!_hasScrolled) return;

        // Запускаем новый таймер на 2 секунды
        _markReadDebouncer = Observable.Timer(TimeSpan.FromMilliseconds(MARK_READ_IDLE_MS))
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

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        var viewModel = BindingContext as ChannelMessagesViewModel;
        if (viewModel == null) return;

        switch (e.Status)
        {
            case GestureStatus.Started:
                _startScale = viewModel.CurrentScale;
                break;

            case GestureStatus.Running:
                // Ограничиваем масштаб от 0.5 до 5.0
                _currentScale = Math.Clamp(_startScale * e.Scale, 0.5, 5.0);
                viewModel.CurrentScale = _currentScale;
                break;

            case GestureStatus.Completed:
                // Масштаб уже сохранен в CurrentScale
                break;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Register(this);

        if (BindingContext is ChannelMessagesViewModel viewModel)
        {
            viewModel.ChannelId = ChannelId;
            await viewModel.InitializeAsync();
        }
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

        // Если были скроллы, отмечаем прочитанными при уходе
        if (_hasScrolled && BindingContext is ChannelMessagesViewModel vm)
        {
            _ = vm.MarkVisibleMessagesAsReadAsync();
        }

        MessagesScrollView.Scrolled -= OnScrollViewScrolled;
    }
}