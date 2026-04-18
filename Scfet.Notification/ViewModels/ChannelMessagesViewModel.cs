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

namespace Scfet.Notification.ViewModels
{
    public partial class ChannelMessagesViewModel : BaseViewModel
    {
        private readonly IChannelMessageApiService _messageService;
        private readonly IChannelApiService _channelService;
        private readonly SignalRService _signalRService;
        private readonly LoginService _loginService;
        private readonly IPickImageService _pickImageService;
        private readonly FileService _fileService;

        // для отображения статуса печати
        private IDisposable? _typingDebounceSubscription;
        private IDisposable? _typingTimeoutSubscription; 
        private Dictionary<Guid, string> _typingUsers = new();

        private bool _isTyping = false;
        private readonly object _typingLock = new();

        // для статуса прочтения
        private DateTime _lastMarkReadCall = DateTime.MinValue;
        private SemaphoreSlim _markReadSemaphore;
        private Guid? _lastMarkedMessageId;

        private bool _isDisposed;

        public ChannelMessagesViewModel(
            IChannelMessageApiService messageService,
            IChannelApiService channelService,
            SignalRService signalRService,
            LoginService loginService,
            IPickImageService pickImageService,
            FileService fileService)
        {
            _messageService = messageService;
            _channelService = channelService;
            _signalRService = signalRService;
            _loginService = loginService;
            _pickImageService = pickImageService;
            _fileService = fileService;
        }

        // Main fields
        [ObservableProperty]
        private string channelId = string.Empty;

        [ObservableProperty]
        private Guid currentUserId;

        [ObservableProperty]
        private ChannelRole? currentUserChannelRole;

        [ObservableProperty]
        private ChannelDto? channel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMoreMessages))]
        private ObservableCollection<ChannelMessageDto> messages = new();

        [ObservableProperty]
        private string newMessageText = string.Empty;

        [ObservableProperty]
        private ChannelMessageDto? replyToMessage;

        [ObservableProperty]
        private ChannelMessageDto? editingMessage;

        // UI
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
        private MessageFilter filter = new() { PageSize = 30, SortOrder = SortOrder.Descending };

        // images
        [ObservableProperty]
        private FileResult? selectedImage;

        [ObservableProperty]
        private ImageSource? imagePreview;

        [ObservableProperty]
        private bool isImageLoading;

        [ObservableProperty]
        private string imageSizeText = string.Empty;

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

            // Загружаем предпросмотр
            if (value != null)
            {
                LoadImagePreviewAsync(value);

                // Получаем размер файла
                Task.Run(async () =>
                {
                    try
                    {
                        using var stream = await value.OpenReadAsync();
                        var sizeInBytes = stream.Length;
                        var sizeText = _fileService.FormatFileSize(sizeInBytes);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ImageSizeText = sizeText;
                        });
                    }
                    catch
                    {
                        ImageSizeText = string.Empty;
                    }
                });
            }
            else
            {
                ImagePreview = null;
                ImageSizeText = string.Empty;
            }
        }

        public async Task InitializeAsync()
        {
            _isDisposed = false;
            _markReadSemaphore = new(1, 1);
            var auth = _loginService.GetCurrentAuth();
            CurrentUserId = Guid.TryParse(auth?.UserId, out var id) ? id : Guid.Empty;

            List<Task> tasks = new() { LoadChannelInfoAsync(), LoadMessagesAsync() };
            await Task.WhenAll(tasks);

            await SubscribeToSignalREvents();

            await _signalRService.JoinChannelAsync(Guid.Parse(ChannelId));
            await MarkVisibleMessagesAsReadAsync();
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
                    CurrentUserChannelRole = Channel.UserRole;
                    OnPropertyChanged(nameof(Title));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load channel info error: {ex.Message}");
            }
        }

        #region main methods
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
                    var newMessages = response.Data.OrderBy(m => m.CreatedAt).ToList();

                    // Получаем первое существующее сообщение
                    var firstExisting = Messages.FirstOrDefault();

                    // Подготавливаем новые сообщения
                    ChannelMessageDto? previousInNew = null;
                    foreach (var msg in newMessages)
                    {
                        msg.IsOwnMessage = msg.SenderId == CurrentUserId;

                        if (previousInNew == null)
                        {
                            msg.ShowDateHeader = true;
                            msg.ShowAvatar = true;
                            msg.ShowSenderName = !msg.IsOwnMessage;
                        }
                        else
                        {
                            msg.ShowDateHeader = msg.CreatedAt.Date != previousInNew.CreatedAt.Date;

                            // Показываем аватар если:
                            // - новая дата
                            // - другой отправитель
                            // - прошло больше 1 минуты
                            var showAvatar = msg.ShowDateHeader ||
                                            previousInNew.SenderId != msg.SenderId ||
                                            (msg.CreatedAt - previousInNew.CreatedAt).TotalMinutes > 1;
                            msg.ShowAvatar = showAvatar;
                            msg.ShowSenderName = showAvatar && !msg.IsOwnMessage;
                        }
                        previousInNew = msg;
                    }

                    // Корректируем стык
                    if (previousInNew != null && firstExisting != null)
                    {
                        var needDateHeader = previousInNew.CreatedAt.Date != firstExisting.CreatedAt.Date;
                        var showAvatar = needDateHeader ||
                                        previousInNew.SenderId != firstExisting.SenderId ||
                                        (firstExisting.CreatedAt - previousInNew.CreatedAt).TotalMinutes > 1;

                        // Обновляем только если изменилось
                        if (firstExisting.ShowDateHeader != needDateHeader)
                            firstExisting.ShowDateHeader = needDateHeader;
                        if (firstExisting.ShowAvatar != showAvatar)
                            firstExisting.ShowAvatar = showAvatar;
                        if (firstExisting.ShowSenderName != (showAvatar && !firstExisting.IsOwnMessage))
                            firstExisting.ShowSenderName = showAvatar && !firstExisting.IsOwnMessage;
                    }

                    // Вставляем в обратном порядке
                    for (int i = newMessages.Count - 1; i >= 0; i--)
                    {
                        if (!Messages.Any(m => m.Id == newMessages[i].Id))
                        {
                            Messages.Insert(0, newMessages[i]);
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

            // Очищаем поля
            NewMessageText = string.Empty;
            ClearSelectedImage();
            CancelReply();

            // Отправляем статус печати
            await _signalRService.SendTypingStatusAsync(Guid.Parse(ChannelId), false);
            StopTyping();

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
                // Восстанавливаем данные при ошибке
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

        public async Task MarkVisibleMessagesAsReadAsync()
        {
            if (string.IsNullOrEmpty(ChannelId)) return;
            if (_isDisposed) return;

            // Защита от частых вызовов
            if ((DateTime.UtcNow - _lastMarkReadCall).TotalMilliseconds < 300) return;

            // Используем семафор чтобы избежать параллельных вызовов
            if (!await _markReadSemaphore.WaitAsync(0)) return;

            try
            {
                _lastMarkReadCall = DateTime.UtcNow;

                // Находим последнее непрочитанное чужое сообщение
                var messagesToReadIds = Messages
                    .Where(m => m.SenderId != CurrentUserId && !m.IsRead)
                    .Select(m => m.Id)
                    .ToList();

                Guid? lastUnreadOtherMessageId = messagesToReadIds
                    .LastOrDefault();

                if (lastUnreadOtherMessageId == null) return;

                // Не отмечаем одно и то же сообщение повторно
                if (_lastMarkedMessageId == lastUnreadOtherMessageId) return;

                _lastMarkedMessageId = lastUnreadOtherMessageId;

                // Отмечаем на сервере (fire-and-forget, не ждем)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _messageService.MarkMessagesAsReadAsync(Guid.Parse(ChannelId), messagesToReadIds);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Mark as read error: {ex.Message}");
                    }
                });
            }
            finally
            {
                _markReadSemaphore.Release();
            }
        }
        #endregion

        #region Image
        [RelayCommand]
        private async Task SelectImageAsync()
        {
            try
            {
                var result = await _pickImageService.SelectImageAsync();
                if (result != null)
                {
                    if (!await _pickImageService.CheckFileResultAsync(result)) return;

                    SelectedImage = result;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось выбрать изображение: {ex.Message}", "OK");
            }
        }

        private async void LoadImagePreviewAsync(FileResult fileResult)
        {
            if (fileResult == null) return;

            IsImageLoading = true;

            try
            {
                using var stream = await fileResult.OpenReadAsync();

                // Создаем копию потока для предпросмотра
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                ImagePreview = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image preview: {ex.Message}");
                ImagePreview = null;
            }
            finally
            {
                IsImageLoading = false;
            }
        }

        [RelayCommand]
        private async Task ViewFullImageAsync()
        {
            if (SelectedImage == null) return;

            try
            {
                using var stream = await SelectedImage.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var imageSource = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));
                var fileName = SelectedImage.FileName;
                var imageSize = ImageSizeText;

                var image = new Image
                {
                    Source = imageSource,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    BackgroundColor = Colors.Black
                };

                var pinchGesture = new PinchGestureRecognizer();
                double currentScale = 1;
                double startScale = 1;

                pinchGesture.PinchUpdated += (s, e) =>
                {
                    if (e.Status == GestureStatus.Started)
                    {
                        startScale = currentScale;
                    }
                    else if (e.Status == GestureStatus.Running)
                    {
                        currentScale = startScale * e.Scale;
                        currentScale = Math.Clamp(currentScale, 1, 3);
                        image.Scale = currentScale;
                    }
                };
                image.GestureRecognizers.Add(pinchGesture);

                var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
                doubleTap.Tapped += (s, e) =>
                {
                    currentScale = 1;
                    image.ScaleTo(1, 250, Easing.CubicOut);
                };
                image.GestureRecognizers.Add(doubleTap);

                var grid = new Grid
                {
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Star },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    BackgroundColor = Colors.Black
                };

                // Верхняя панель с HorizontalStackLayout для лучшего распределения
                var topPanel = new Border
                {
                    BackgroundColor = Color.FromArgb("#CC000000"),
                    Padding = new Thickness(12, 8, 12, 8),
                    HeightRequest = 50
                };

                var topStack = new HorizontalStackLayout
                {
                    Spacing = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };

                var closeButton = new Button
                {
                    Text = "✕",
                    BackgroundColor = Colors.Transparent,
                    TextColor = Colors.White,
                    FontSize = 18,
                    WidthRequest = 36,
                    HeightRequest = 36,
                    CornerRadius = 18
                };
                closeButton.Clicked += async (s, e) => await Shell.Current.Navigation.PopModalAsync();

                var titleLabel = new Label
                {
                    Text = fileName?.Length > 25 ? fileName[..22] + "..." : fileName,
                    TextColor = Colors.White,
                    FontSize = 13,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };

                var infoButton = new Button
                {
                    Text = "ℹ",
                    BackgroundColor = Colors.Transparent,
                    TextColor = Colors.White,
                    FontSize = 16,
                    WidthRequest = 36,
                    HeightRequest = 36,
                    CornerRadius = 18
                };
                infoButton.Clicked += async (s, e) =>
                {
                    await Shell.Current.DisplayAlert("Информация", $"📁 {fileName}\n📏 {imageSize}", "OK");
                };

                topStack.Children.Add(closeButton);
                topStack.Children.Add(titleLabel);
                topStack.Children.Add(infoButton);
                topPanel.Content = topStack;

                var bottomHint = new Label
                {
                    Text = "👆 Два пальца — масштаб • Двойной тап — сброс",
                    TextColor = Colors.LightGray,
                    FontSize = 11,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 0, 0, 15),
                    Padding = new Thickness(10, 0)
                };

                grid.Children.Add(topPanel);
                grid.Children.Add(image);
                grid.Children.Add(bottomHint);
                Grid.SetRow(topPanel, 0);
                Grid.SetRow(image, 1);
                Grid.SetRow(bottomHint, 2);

                // Для iOS добавляем отступ сверху
                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    topPanel.Margin = new Thickness(0, 20, 0, 0);
                    topPanel.HeightRequest = 50;
                }

                var modalPage = new ContentPage { Content = grid, BackgroundColor = Colors.Black };
                await Shell.Current.Navigation.PushModalAsync(modalPage);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private void ClearSelectedImage()
        {
            SelectedImage = null;
            ImagePreview = null;
        }

        #endregion

        #region actions

        private bool CanDeleteMessage(ChannelMessageDto message)
        {
            // Отправитель всегда может удалить своё сообщение
            if (message.SenderId == CurrentUserId)
                return true;

            // Проверяем права текущего пользователя
            return CurrentUserChannelRole switch
            {
                ChannelRole.Owner => true, // Владелец может удалять всё

                ChannelRole.Admin => message.SenderChannelRole != ChannelRole.Owner &&
                                    message.SenderChannelRole != ChannelRole.Admin,

                ChannelRole.Moderator => message.SenderChannelRole == ChannelRole.Member,

                _ => false
            };
        }

        private bool CanEditMessage(ChannelMessageDto message)
        {
            // Только отправитель может редактировать
            return message.SenderId == CurrentUserId;
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
        #endregion

        #region Typing methods
        private void HandleTypingIndicator(string text)
        {
            if (string.IsNullOrEmpty(ChannelId)) return;

            lock (_typingLock)
            {
                // Отменяем все предыдущие таймеры
                _typingDebounceSubscription?.Dispose();
                _typingTimeoutSubscription?.Dispose();

                // Если текст пустой, сразу отправляем статус "не печатает"
                if (string.IsNullOrWhiteSpace(text))
                {
                    StopTyping();
                    return;
                }

                // Если ещё не печатаем — отправляем с debounce
                if (!_isTyping)
                {
                    _isTyping = true;

                    _typingDebounceSubscription = Observable.Timer(TimeSpan.FromMilliseconds(300))
                        .Subscribe(_ =>
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await SendTypingStatusSafeAsync(true);
                            });

                            // Запускаем таймер автоматического сброса
                            _typingTimeoutSubscription = Observable.Timer(TimeSpan.FromSeconds(5))
                                .Subscribe(__ =>
                                {
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        lock (_typingLock)
                                        {
                                            if (_isTyping)
                                                StopTyping();
                                        }
                                    });
                                });
                        });
                }
                else
                {
                    // Уже печатаем — просто сбрасываем таймер неактивности
                    _typingTimeoutSubscription = Observable.Timer(TimeSpan.FromSeconds(5))
                        .Subscribe(_ =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                lock (_typingLock)
                                {
                                    if (_isTyping)
                                        StopTyping();
                                }
                            });
                        });
                }
            }
        }

        private void StopTyping()
        {
            lock (_typingLock)
            {
                if (_isTyping)
                {
                    _isTyping = false;
                    _typingDebounceSubscription?.Dispose();
                    _typingTimeoutSubscription?.Dispose();
                    _typingUsers.Clear();
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending typing status: {ex.Message}");

                lock (_typingLock)
                {
                    _isTyping = false;
                    _typingDebounceSubscription?.Dispose();
                    _typingTimeoutSubscription?.Dispose();
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

        private async Task SubscribeToSignalREvents()
        {
            await _signalRService.ConnectAsync();

            _signalRService.OnNewMessage += OnNewMessageReceived;
            _signalRService.OnMessageUpdated += OnMessageUpdated;
            _signalRService.OnMessageDeleted += OnMessageDeleted;
            _signalRService.OnUserTyping += OnUserTyping;
            _signalRService.OnUserJoined += OnUserJoined;
            _signalRService.OnUserLeft += OnUserLeft;
            _signalRService.OnMyMessageRead += OnMyMessageRead;
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
                        SenderRole = message.SenderRole,
                        SenderChannelRole = message.SenderChannelRole,
                        ReplyToMessageId = message.ReplyToMessageId,
                        ReplyToMessage = message.ReplyToMessage,
                        ImageUrl = message.ImageUrl,
                        CreatedAt = message.CreatedAt,
                        IsOwnMessage = message.SenderId == CurrentUserId,
                        IsRead = message.SenderId == CurrentUserId ? false : true,
                        ShowDateHeader = ShouldShowDateHeader(new ChannelMessageDto { CreatedAt = message.CreatedAt }, Messages.LastOrDefault())
                    };

                    // Рассчитываем права на клиенте
                    messageDto.CanEdit = CanEditMessage(messageDto);
                    messageDto.CanDelete = CanDeleteMessage(messageDto);

                    var lastMessage = Messages.LastOrDefault();
                    ApplyGroupingForSingleMessage(messageDto, lastMessage);

                    Messages.Add(messageDto);
                    ShowScrollToBottomButton = true;

                    if (message.SenderId != CurrentUserId)
                    {
                        _ = _messageService.MarkAsReadAsync(Guid.Parse(ChannelId), message.Id);
                    }
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

        private void OnMessageDeleted(Guid messageId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Messages.FirstOrDefault(m => m.Id == messageId);
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
                }
                else
                {
                    _typingUsers.Remove(eventData.UserId);
                }

                UpdateTypingIndicator();
            });
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

        private void OnMyMessageRead(Guid messageId, Guid channelId)
        {
            if (channelId.ToString() != ChannelId) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var targetMessage = Messages.FirstOrDefault(m => m.Id == messageId);
                if (targetMessage?.SenderId != CurrentUserId) return;

                var cutoffTime = targetMessage.CreatedAt;

                // Обновляем только свои непрочитанные сообщения до этого времени
                foreach (var msg in Messages)
                {
                    if (msg.SenderId == CurrentUserId &&
                        !msg.IsRead &&
                        msg.CreatedAt <= cutoffTime)
                    {
                        msg.IsRead = true;
                        msg.ReadAt = DateTime.UtcNow;
                    }
                }
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

        #endregion

        private void ProcessMessagesForGrouping(ICollection<ChannelMessageDto> messages)
        {
            ApplyGroupingToMessages(messages);
        }

        public void Cleanup()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            // Очищаем изображение
            ClearSelectedImage();

            // Останавливаем все таймеры
            StopTyping();

            _markReadSemaphore?.Dispose();

            // Отписываемся от событий SignalR
            _signalRService.OnNewMessage -= OnNewMessageReceived;
            _signalRService.OnMessageUpdated -= OnMessageUpdated;
            _signalRService.OnMessageDeleted -= OnMessageDeleted;
            _signalRService.OnUserTyping -= OnUserTyping;
            _signalRService.OnUserJoined -= OnUserJoined;
            _signalRService.OnUserLeft -= OnUserLeft;
            _signalRService.OnMyMessageRead -= OnMyMessageRead;

            // Покидаем канал
            if (!string.IsNullOrEmpty(ChannelId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _signalRService.LeaveChannelAsync(Guid.Parse(ChannelId));
                    }
                    catch {}
                });
            }
        }
    }
}
