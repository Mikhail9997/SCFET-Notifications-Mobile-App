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
    private const int LOAD_MORE_THRESHOLD_INDEX = 3; // Загружаем когда видим 3-й элемент сверху
    private const int BOTTOM_THRESHOLD_INDEX = 3; // Считаем что внизу если видим 3-й элемент снизу
    private ChannelMessageDto? _firstVisibleMessage;

    public ChannelMessagesPage(ChannelMessagesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        WeakReferenceMessenger.Default.Register(this);

        MessagesCollectionView.Scrolled += OnCollectionViewScrolled;
    }

    private async void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        if (BindingContext is not ChannelMessagesViewModel vm) return;

        var totalItems = vm.Messages.Count;
        if (totalItems == 0) return;

        // Сохраняем первый видимый элемент
        _firstVisibleMessage = vm.Messages.ElementAtOrDefault(e.FirstVisibleItemIndex);

        // Загрузка при скролле вверх
        if (e.FirstVisibleItemIndex <= LOAD_MORE_THRESHOLD_INDEX && !_isLoadingMore && vm.HasMoreMessages)
        {
            _isLoadingMore = true;
            var anchor = _firstVisibleMessage;

            await vm.LoadMoreMessagesAsync();

            if (anchor != null)
            {
                await Task.Delay(100);
                var newIndex = vm.Messages.IndexOf(anchor);
                if (newIndex >= 0)
                {
                    MessagesCollectionView.ScrollTo(newIndex, position: ScrollToPosition.Start, animate: false);
                }
            }

            _isLoadingMore = false;
        }

        vm.ShowScrollToBottomButton = e.LastVisibleItemIndex < totalItems - BOTTOM_THRESHOLD_INDEX;
    }

    public async void Receive(ScrollToBottomMessage message)
    {
        await ScrollToBottom(message.Animated);
    }

    private async Task ScrollToBottom(bool animated = true)
    {
        if (MessagesCollectionView.ItemsSource != null)
        {
            try
            {
                var itemsSource = MessagesCollectionView.ItemsSource as ObservableCollection<ChannelMessageDto>;
                if (itemsSource?.Count > 0)
                {
                    await Task.Delay(50);
                    MessagesCollectionView.ScrollTo(itemsSource.Count - 1, position: ScrollToPosition.End, animate: animated);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scroll to bottom error: {ex.Message}");
            }
        }
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