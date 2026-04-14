using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Scfet.Notification.Messages;
using Scfet.Notification.Models;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.Models.SignalR;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;
using static System.Net.Mime.MediaTypeNames;

namespace Scfet.Notification.ViewModels
{
    public partial class ChannelMessagesViewModel : BaseViewModel
    {
        private readonly IChannelMessageApiService _messageService;
        private readonly IChannelApiService _channelService;
        private readonly SignalRService _signalRService;
        private readonly LoginService _loginService;
        private readonly IPickImageService _pickImageService;

        private IDisposable? _typingDebounceSubscription;
        private IDisposable? _typingStatusTimer;
        private Dictionary<Guid, IDisposable> _typingTimers = new();
        private Dictionary<Guid, string> _typingUsers = new();

        private bool _isTyping = false;
        private DateTime _lastTypingSent = DateTime.MinValue;
        private readonly object _typingLock = new object();

        private const int TYPING_DEBOUNCE_MS = 300; // Задержка перед первой отправкой
        private const int TYPING_RESEND_INTERVAL_SEC = 3; // Интервал повторной отправки
        private const int TYPING_TIMEOUT_SEC = 5; // Таймаут неактивности

        public ChannelMessagesViewModel(
            IChannelMessageApiService messageService,
            IChannelApiService channelService,
            SignalRService signalRService,
            LoginService loginService,
            IPickImageService pickImageService)
        {
            _messageService = messageService;
            _channelService = channelService;
            _signalRService = signalRService;
            _loginService = loginService;
            _pickImageService = pickImageService;

            SubscribeToSignalREvents();
        }

        [ObservableProperty]
        private string channelId = string.Empty;

        [ObservableProperty]
        private Guid currentUserId;

        [ObservableProperty]
        private ChannelDto? channel;

        [ObservableProperty]
        private ObservableCollection<ChannelMessageDto> messages = new();

        [ObservableProperty]
        private string newMessageText = string.Empty;

        [ObservableProperty]
        private ChannelMessageDto? replyToMessage;

        [ObservableProperty]
        private ChannelMessageDto? editingMessage;

        [ObservableProperty]
        private FileResult? selectedImage;

        [ObservableProperty]
        private string typingIndicatorText = string.Empty;

        [ObservableProperty]
        private string onlineStatus = "онлайн";

        [ObservableProperty]
        private bool isMessagesLoading;

        [ObservableProperty]
        private bool isMessagesLoadFailed;

        [ObservableProperty]
        private string messagesError = string.Empty;

        [ObservableProperty]
        private bool isLoadingMore;

        [ObservableProperty]
        private bool showScrollToBottomButton;

        [ObservableProperty]
        private bool isSending;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private bool hasMoreMessages = true;

        [ObservableProperty]
        private MessageFilter filter = new() { PageSize = 10, SortOrder = SortOrder.Descending };

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(NewMessageText) || SelectedImage != null;

        public string Title => Channel?.Name ?? "Канал";

        partial void OnNewMessageTextChanged(string value)
        {
            OnPropertyChanged(nameof(CanSendMessage));
            HandleTypingIndicator(value);
        }

        partial void OnSelectedImageChanged(FileResult? value)
        {
            OnPropertyChanged(nameof(CanSendMessage));
        }

        public async Task InitializeAsync()
        {
            var auth = _loginService.GetCurrentAuth();
            CurrentUserId = Guid.TryParse(auth?.UserId, out var id) ? id : Guid.Empty;

            await LoadChannelInfoAsync();
            await LoadMessagesAsync();
            await _signalRService.JoinChannelAsync(Guid.Parse(ChannelId));
            await _messageService.MarkAllAsReadAsync(Guid.Parse(ChannelId));
        }

        private async Task LoadChannelInfoAsync()
        {
            if (string.IsNullOrEmpty(ChannelId)) return;

            try
            {
                var response = await _channelService.GetChannelByIdAsync(Guid.Parse(ChannelId));
                if (response?.Success == true && response.Data != null)
                {
                    Channel = response.Data;
                    OnPropertyChanged(nameof(Title));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load channel info error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadMessagesAsync()
        {
            if (IsMessagesLoading || string.IsNullOrEmpty(ChannelId)) return;

            IsMessagesLoading = true;
            IsMessagesLoadFailed = false;
            MessagesError = string.Empty;

            try
            {
                CurrentPage = 1;
                Filter.Page = CurrentPage;

                var response = await _messageService.GetMessagesAsync(Guid.Parse(ChannelId), Filter);

                if (response?.Success == true && response.Data != null)
                {
                    Messages.Clear();

                    // Добавляем сообщения в обратном порядке (новые снизу)
                    var sortedMessages = response.Data.OrderBy(m => m.CreatedAt).ToList();

                    // Группируем сообщения
                    ProcessMessagesForGrouping(sortedMessages);

                    // Добавляем заголовки дат
                    DateTime? lastDate = null;
                    foreach (var message in sortedMessages)
                    {
                        var messageDate = message.CreatedAt.Date;
                        message.ShowDateHeader = lastDate != messageDate;
                        lastDate = messageDate;
                        message.IsOwnMessage = message.SenderId == CurrentUserId;

                        Messages.Add(message);
                    }

                    HasMoreMessages = response.Pagination.Page < response.Pagination.TotalPages;
                    CurrentPage = response.Pagination.Page;

                    await ScrollToBottomAsync();
                }
                else
                {
                    IsMessagesLoadFailed = true;
                    MessagesError = response?.Message ?? "Не удалось загрузить сообщения";
                }
            }
            catch (Exception ex)
            {
                IsMessagesLoadFailed = true;
                MessagesError = ex.Message;
            }
            finally
            {
                IsMessagesLoading = false;
            }
        }

        [RelayCommand]
        public async Task LoadMoreMessagesAsync()
        {
            if (IsLoadingMore || !HasMoreMessages || string.IsNullOrEmpty(ChannelId)) return;

            IsLoadingMore = true;

            try
            {
                Filter.Page = CurrentPage + 1;
                var response = await _messageService.GetMessagesAsync(Guid.Parse(ChannelId), Filter);

                if (response?.Success == true && response.Data != null && response.Data.Any())
                {
                    // Добавляем старые сообщения в начало
                    var sortedMessages = response.Data.OrderBy(m => m.CreatedAt).ToList();

                    // Группируем сообщения
                    ProcessMessagesForGrouping(sortedMessages);

                    DateTime? lastDate = Messages.FirstOrDefault()?.CreatedAt.Date;

                    foreach (var message in sortedMessages)
                    {
                        if (!Messages.Any(m => m.Id == message.Id))
                        {
                            var messageDate = message.CreatedAt.Date;
                            message.ShowDateHeader = lastDate != messageDate;
                            lastDate = messageDate;
                            message.IsOwnMessage = message.SenderId == CurrentUserId;

                            Messages.Insert(0, message);
                        }
                    }

                    CurrentPage = response.Pagination.Page;
                    HasMoreMessages = response.Pagination.Page < response.Pagination.TotalPages;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load more messages error: {ex.Message}");
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {

            // Если редактируем сообщение, вызываем обновление
            if (EditingMessage != null)
            {
                await UpdateMessageAsync();
                return;
            }

            if (!CanSendMessage || IsSending || string.IsNullOrEmpty(ChannelId)) return;

            var messageText = NewMessageText;
            var replyTo = ReplyToMessage;
            var image = SelectedImage;

            NewMessageText = string.Empty;
            SelectedImage = null;
            CancelReply();
            _isTyping = false;
            await _signalRService.SendTypingStatusAsync(Guid.Parse(ChannelId), false);

            IsSending = true;

            try
            {
                var request = new SendMessageRequest
                {
                    Content = messageText?.Trim() ?? string.Empty,
                    ReplyToMessageId = replyTo?.Id,
                    Image = image
                };

                var response = await _messageService.SendMessageAsync(Guid.Parse(ChannelId), request);

                if (response?.Success == true && response.Data != null)
                {
                    var message = response.Data;
                    message.IsOwnMessage = true;
                    message.ShowDateHeader = ShouldShowDateHeader(message, Messages.LastOrDefault());
                    Messages.Add(message);
                    await ScrollToBottomAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось отправить сообщение", "OK");
                    NewMessageText = messageText;
                    SelectedImage = image;
                    ReplyToMessage = replyTo;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
                NewMessageText = messageText;
                SelectedImage = image;
                ReplyToMessage = replyTo;
            }
            finally
            {
                IsSending = false;
            }
        }

        [RelayCommand]
        private async Task UpdateMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMessageText) || EditingMessage == null || string.IsNullOrEmpty(ChannelId))
                return;

            var messageText = NewMessageText;
            var editingMessage = EditingMessage;

            // Очищаем состояние редактирования
            NewMessageText = string.Empty;
            CancelEdit();

            IsSending = true;

            try
            {
                var request = new UpdateMessageRequest
                {
                    Content = messageText?.Trim() ?? string.Empty
                };

                var response = await _messageService.UpdateMessageAsync(
                    Guid.Parse(ChannelId),
                    editingMessage.Id,
                    request);

                if (response?.Success == true && response.Data != null)
                {
                    // Обновляем сообщение в коллекции
                    var existingMessage = Messages.FirstOrDefault(m => m.Id == editingMessage.Id);
                    if (existingMessage != null)
                    {
                        existingMessage.Content = response.Data.Content;
                        existingMessage.IsEdited = response.Data.IsEdited;
                        existingMessage.EditedAt = response.Data.EditedAt;
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка",
                        response?.Message ?? "Не удалось обновить сообщение", "OK");

                    // Восстанавливаем состояние редактирования при ошибке
                    EditingMessage = editingMessage;
                    NewMessageText = messageText;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");

                // Восстанавливаем состояние редактирования при ошибке
                EditingMessage = editingMessage;
                NewMessageText = messageText;
            }
            finally
            {
                IsSending = false;
            }
        }

        [RelayCommand]
        private async Task SelectImageAsync()
        {
            try
            {
                var result = await _pickImageService.SelectImageAsync();
                if (result != null)
                {
                    SelectedImage = result;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось выбрать изображение: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void Reply(ChannelMessageDto message)
        {
            ReplyToMessage = message;
            EditingMessage = null;
        }

        [RelayCommand]
        private void CancelReply()
        {
            ReplyToMessage = null;
        }

        [RelayCommand]
        private void Edit(ChannelMessageDto message)
        {
            EditingMessage = message;
            ReplyToMessage = null;
            NewMessageText = message.Content;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            EditingMessage = null;
            NewMessageText = string.Empty;
        }

        [RelayCommand]
        private async Task ShowMessageMenuAsync(ChannelMessageDto message)
        {
            var actions = new List<string>();

            if (message.CanEdit)
            {
                actions.Add("Редактировать");
            }

            actions.Add("Ответить");
            actions.Add("Копировать текст");

            if (message.CanDelete)
            {
                actions.Add("Удалить");
            }

            if (!actions.Any()) return;

            var action = await Shell.Current.DisplayActionSheet(
                "Сообщение",
                "Отмена",
                null,
                actions.ToArray());

            switch (action)
            {
                case "Редактировать":
                    Edit(message);
                    break;
                case "Ответить":
                    Reply(message);
                    break;
                case "Копировать текст":
                    await Clipboard.SetTextAsync(message.Content);
                    await Shell.Current.DisplayAlert("Успех", "Текст скопирован", "OK");
                    break;
                case "Удалить":
                    await DeleteMessageAsync(message);
                    break;
            }
        }

        private async Task DeleteMessageAsync(ChannelMessageDto message)
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Удалить сообщение",
                "Вы уверены, что хотите удалить это сообщение?",
                "Да", "Нет");

            if (!confirm) return;

            try
            {
                var response = await _messageService.DeleteMessageAsync(Guid.Parse(ChannelId), message.Id);

                if (response?.Success == true)
                {
                    Messages.Remove(message);
                    await ScrollToBottomAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось удалить сообщение", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task ViewImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            await Shell.Current.GoToAsync($"ImageViewPage?imageUrl={Uri.EscapeDataString(imageUrl)}");
        }

        [RelayCommand]
        private async Task ShowChannelMenuAsync()
        {
            var actions = new List<string> { "Участники", "Пригласить" };

            if (Channel?.IsOwner == true || Channel?.UserRole == ChannelRole.Admin)
            {
                actions.Add("Настройки канала");
            }

            if (!Channel?.IsOwner == true)
            {
                actions.Add("Покинуть канал");
            }

            var action = await Shell.Current.DisplayActionSheet(
                Channel?.Name ?? "Канал",
                "Отмена",
                null,
                actions.ToArray());

            switch (action)
            {
                case "Участники":
                    await Shell.Current.GoToAsync($"ChannelMembersPage?channelId={ChannelId}");
                    break;
                case "Пригласить":
                    await Shell.Current.GoToAsync($"InviteUsersPage?channelId={ChannelId}");
                    break;
                case "Покинуть канал":
                    await LeaveChannelAsync();
                    break;
            }
        }

        private async Task LeaveChannelAsync()
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Покинуть канал",
                "Вы уверены, что хотите покинуть канал?",
                "Да", "Нет");

            if (!confirm) return;

            try
            {
                var response = await _channelService.LeaveChannelAsync(Guid.Parse(ChannelId));
                if (response?.Success == true)
                {
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task ScrollToBottomAsync()
        {
            await Task.Delay(100);
            ShowScrollToBottomButton = false;

            WeakReferenceMessenger.Default.Send(new ScrollToBottomMessage());
        }

        [RelayCommand]
        private async Task RetryLoadMessagesAsync()
        {
            await LoadMessagesAsync();
        }

        #region Typing methods
        private void HandleTypingIndicator(string text)
        {
            if (string.IsNullOrEmpty(ChannelId)) return;

            lock (_typingLock)
            {
                // Отменяем предыдущий debounce
                _typingDebounceSubscription?.Dispose();

                // Если текст пустой, сразу отправляем статус "не печатает"
                if (string.IsNullOrWhiteSpace(text))
                {
                    StopTyping();
                    return;
                }

                // Если только начали печатать
                if (!_isTyping)
                {
                    _isTyping = true;
                    _lastTypingSent = DateTime.UtcNow;

                    // Отправляем статус с небольшой задержкой (debounce)
                    _typingDebounceSubscription = Observable.Timer(TimeSpan.FromMilliseconds(TYPING_DEBOUNCE_MS))
                        .Subscribe(timerValue => // timerValue = 0
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _ = SendTypingStatusSafeAsync(true);
                            });

                            // Запускаем таймер повторной отправки
                            StartTypingResendTimer();

                            // Запускаем таймер автоматического сброса статуса
                            StartTypingTimeoutTimer();
                        });
                }
                else
                {
                    // Уже печатаем, просто обновляем таймер неактивности
                    ResetTypingTimeoutTimer();

                    // Проверяем, нужно ли отправить повторный сигнал
                    var now = DateTime.UtcNow;
                    if ((now - _lastTypingSent).TotalSeconds >= TYPING_RESEND_INTERVAL_SEC)
                    {
                        _lastTypingSent = now;
                        _ = SendTypingStatusSafeAsync(true);
                    }
                }
            }
        }

        private void StartTypingResendTimer()
        {
            _typingStatusTimer?.Dispose();

            _typingStatusTimer = Observable.Interval(TimeSpan.FromSeconds(TYPING_RESEND_INTERVAL_SEC))
                .Subscribe(interval =>
                {
                    lock (_typingLock)
                    {
                        if (_isTyping && !string.IsNullOrWhiteSpace(NewMessageText))
                        {
                            _lastTypingSent = DateTime.UtcNow;
                            _ = SendTypingStatusSafeAsync(true);
                        }
                        else
                        {
                            _typingStatusTimer?.Dispose();
                        }
                    }
                });
        }

        private void StartTypingTimeoutTimer()
        {
            // Отменяем предыдущий таймер
            _typingDebounceSubscription?.Dispose();

            // Устанавливаем таймер автоматического сброса статуса печати
            _typingDebounceSubscription = Observable.Timer(TimeSpan.FromSeconds(TYPING_TIMEOUT_SEC))
                .Subscribe(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        lock (_typingLock)
                        {
                            if (_isTyping)
                            {
                                StopTyping();
                            }
                        }
                    });
                });
        }

        private void ResetTypingTimeoutTimer()
        {
            _typingDebounceSubscription?.Dispose();
            StartTypingTimeoutTimer();
        }

        private void StopTyping()
        {
            lock (_typingLock)
            {
                if (_isTyping)
                {
                    _isTyping = false;
                    _typingDebounceSubscription?.Dispose();
                    _typingStatusTimer?.Dispose();
                    _ = SendTypingStatusSafeAsync(false);
                }
            }
        }

        private async Task SendTypingStatusSafeAsync(bool isTyping)
        {
            try
            {
                if (Guid.TryParse(ChannelId, out var channelId))
                {
                    await _signalRService.SendTypingStatusAsync(channelId, isTyping);
                    Console.WriteLine($"Typing status sent: {isTyping}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending typing status: {ex.Message}");

                // При ошибке сбрасываем состояние
                lock (_typingLock)
                {
                    _isTyping = false;
                    _typingDebounceSubscription?.Dispose();
                    _typingStatusTimer?.Dispose();
                }
            }
        }

        private bool ShouldShowDateHeader(ChannelMessageDto newMessage, ChannelMessageDto? lastMessage)
        {
            if (lastMessage == null) return true;
            return newMessage.CreatedAt.Date != lastMessage.CreatedAt.Date;
        }
        #endregion

        #region SignalR Events

        private void SubscribeToSignalREvents()
        {
            _signalRService.OnNewMessage += OnNewMessageReceived;
            _signalRService.OnMessageUpdated += OnMessageUpdated;
            _signalRService.OnMessageDeleted += OnMessageDeleted;
            _signalRService.OnUserTyping += OnUserTyping;
            _signalRService.OnUserJoined += OnUserJoined;
            _signalRService.OnUserLeft += OnUserLeft;
        }

        private void OnNewMessageReceived(NewMessageEvent message)
        {
            if (message.ChannelId.ToString() != ChannelId) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!Messages.Any(m => m.Id == message.Id) && !IsSending)
                {
                    var messageDto = new ChannelMessageDto
                    {
                        Id = message.Id,
                        Content = message.Content,
                        ChannelId = message.ChannelId,
                        SenderId = message.SenderId,
                        SenderName = message.SenderName,
                        SenderAvatar = message.SenderAvatar,
                        ReplyToMessageId = message.ReplyToMessageId,
                        ImageUrl = message.ImageUrl,
                        CreatedAt = message.CreatedAt,
                        IsOwnMessage = message.SenderId == CurrentUserId,
                        ShowDateHeader = ShouldShowDateHeader(new ChannelMessageDto { CreatedAt = message.CreatedAt }, Messages.LastOrDefault())
                    };

                    var lastMessage = Messages.LastOrDefault();

                    // Применяем группировку
                    ApplyGroupingForSingleMessage(messageDto, lastMessage);

                    Messages.Add(messageDto);
                    ShowScrollToBottomButton = true;

                    // Автоматически отмечаем как прочитанное
                    _ = _messageService.MarkAsReadAsync(Guid.Parse(ChannelId), message.Id);
                }
            });
        }

        private void OnMessageUpdated(MessageUpdatedEvent message)
        {
            if (message.ChannelId.ToString() != ChannelId || IsSending) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Messages.FirstOrDefault(m => m.Id == message.Id);
                if (existing != null)
                {
                    existing.Content = message.Content;
                    existing.IsEdited = message.IsEdited;
                    existing.EditedAt = message.EditedAt;
                }
            });
        }

        private void OnMessageDeleted(MessageDeletedEvent eventData)
        {
            if (eventData.ChannelId.ToString() != ChannelId) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Messages.FirstOrDefault(m => m.Id == eventData.MessageId);
                if (existing != null)
                {
                    Messages.Remove(existing);
                }
            });
        }

        private void OnUserTyping(UserTypingEvent eventData)
        {
            if (eventData.ChannelId.ToString() != ChannelId || eventData.UserId == CurrentUserId) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (eventData.IsTyping)
                {
                    _typingUsers[eventData.UserId] = eventData.UserFullName;

                    if (_typingTimers.TryGetValue(eventData.UserId, out var timer))
                    {
                        timer.Dispose();
                    }

                    var newTimer = Observable.Timer(TimeSpan.FromSeconds(5))
                        .Subscribe(_ =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _typingUsers.Remove(eventData.UserId);
                                UpdateTypingIndicator();
                            });
                        });

                    _typingTimers[eventData.UserId] = newTimer;
                }
                else
                {
                    _typingUsers.Remove(eventData.UserId);
                    if (_typingTimers.TryGetValue(eventData.UserId, out var timer))
                    {
                        timer.Dispose();
                        _typingTimers.Remove(eventData.UserId);
                    }
                }

                UpdateTypingIndicator();
            });
        }

        private void OnUserJoined(UserJoinedEvent eventData)
        {
            if (eventData.ChannelId.ToString() != ChannelId) return;
            OnlineStatus = "онлайн";
        }

        private void OnUserLeft(UserLeftEvent eventData)
        {
            if (eventData.ChannelId.ToString() != ChannelId) return;
            OnlineStatus = "офлайн";
        }

        private void UpdateTypingIndicator()
        {
            if (_typingUsers.Count == 0)
            {
                TypingIndicatorText = string.Empty;
            }
            else if (_typingUsers.Count == 1)
            {
                TypingIndicatorText = $"{_typingUsers.Values.First()} печатает...";
            }
            else
            {
                TypingIndicatorText = $"{_typingUsers.Count} человека печатают...";
            }
        }

        #endregion

        private void ApplyGroupingToMessages(ICollection<ChannelMessageDto> messages)
        {
            ChannelMessageDto? previousMessage = null;

            foreach (var message in messages)
            {
                ApplyGroupingForSingleMessage(message, previousMessage);
                previousMessage = message;
            }
        }

        private void ApplyGroupingForSingleMessage(ChannelMessageDto message, ChannelMessageDto? previousMessage)
        {
            if (previousMessage == null)
            {
                message.ShowAvatar = true;
                message.ShowSenderName = !message.IsOwnMessage;
                message.ShowDateHeader = true;
                return;
            }

            // Проверяем заголовок даты
            message.ShowDateHeader = message.CreatedAt.Date != previousMessage.CreatedAt.Date;

            // Проверяем аватар и имя
            var shouldShowAvatar = message.ShowDateHeader || // Новая дата - всегда показываем аватар
                                  previousMessage.SenderId != message.SenderId ||
                                  (message.CreatedAt - previousMessage.CreatedAt).TotalMinutes > 1;

            message.ShowAvatar = shouldShowAvatar;
            message.ShowSenderName = shouldShowAvatar && !message.IsOwnMessage;
        }

        private void ProcessMessagesForGrouping(ICollection<ChannelMessageDto> messages)
        {
            ApplyGroupingToMessages(messages);
        }

        public void Cleanup()
        {
            // Останавливаем все таймеры
            StopTyping();

            _typingDebounceSubscription?.Dispose();
            _typingStatusTimer?.Dispose();

            foreach (var timer in _typingTimers.Values)
            {
                timer.Dispose();
            }
            _typingTimers.Clear();
            _typingUsers.Clear();

            // Отписываемся от событий SignalR
            _signalRService.OnNewMessage -= OnNewMessageReceived;
            _signalRService.OnMessageUpdated -= OnMessageUpdated;
            _signalRService.OnMessageDeleted -= OnMessageDeleted;
            _signalRService.OnUserTyping -= OnUserTyping;
            _signalRService.OnUserJoined -= OnUserJoined;
            _signalRService.OnUserLeft -= OnUserLeft;

            // Покидаем канал
            if (!string.IsNullOrEmpty(ChannelId))
            {
                _ = _signalRService.LeaveChannelAsync(Guid.Parse(ChannelId));
            }
        }
    }
}
