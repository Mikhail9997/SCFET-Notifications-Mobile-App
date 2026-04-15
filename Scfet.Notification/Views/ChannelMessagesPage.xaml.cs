using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Scfet.Notification.Messages;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.ViewModels;

namespace Scfet.Notification.Views;

[QueryProperty(nameof(ChannelId), "channelId")]
public partial class ChannelMessagesPage : ContentPage, IRecipient<ScrollToBottomMessage>
{
    public string ChannelId { get; set; } = string.Empty;

    private bool _isLoadingMore = false;
    private const int LOAD_MORE_THRESHOLD = 100;
    private const int BOTTOM_THRESHOLD = 50;
    private double _savedScrollPosition = 0;
    private bool _isRestoringScroll = false;

    public ChannelMessagesPage(ChannelMessagesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        WeakReferenceMessenger.Default.Register(this);

        MessagesScrollView.Scrolled += OnScrollViewScrolled;
    }

    private async void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        if (BindingContext is not ChannelMessagesViewModel vm) return;
        if (_isRestoringScroll) return; // Игнорируем события во время восстановления

        var scrollView = (ScrollView)sender;
        _savedScrollPosition = e.ScrollY;

        // Проверяем, достигли ли верха для подгрузки
        if (e.ScrollY <= LOAD_MORE_THRESHOLD && !_isLoadingMore && vm.HasMoreMessages)
        {
            _isLoadingMore = true;

            // Сохраняем позицию перед загрузкой
            var oldScrollY = e.ScrollY;
            var oldContentHeight = scrollView.ContentSize.Height;

            await vm.LoadMoreMessagesAsync();

            // Восстанавливаем позицию после загрузки
            _isRestoringScroll = true;
            await Task.Delay(100); // Ждем обновления UI

            var newContentHeight = scrollView.ContentSize.Height;
            var heightDifference = newContentHeight - oldContentHeight;
            var newScrollY = oldScrollY + heightDifference;

            if (newScrollY > 0 && newScrollY < newContentHeight)
            {
                await scrollView.ScrollToAsync(0, newScrollY, false);
            }

            await Task.Delay(50);
            _isRestoringScroll = false;
            _isLoadingMore = false;
        }

        // Проверяем, находимся ли внизу
        var contentHeight = scrollView.ContentSize.Height;
        var scrollViewHeight = scrollView.Height;
        var isAtBottom = (contentHeight - e.ScrollY - scrollViewHeight) <= BOTTOM_THRESHOLD;
        vm.ShowScrollToBottomButton = !isAtBottom && contentHeight > scrollViewHeight;
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        MessagingCenter.Unsubscribe<ChannelMessagesViewModel>(this, "ScrollToBottom");

        if (BindingContext is ChannelMessagesViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}