using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Scfet.Notification.Services
{
    public class NotificationService
    {
        private HubConnection _hubConnection;
        private readonly IApiService _apiService;

        public event Action<Models.Notification>? OnNotificationReceived;
        public event Action<Guid>? OnNotificationRemove;
        public event Action<Guid>? OnNotificationRead;
        public event Action<Models.Notification>? OnNotificationUpdate;

        private readonly string BaseUrl = "https://amorously-preeminent-godwit.cloudpub.ru";
        //http://localhost:5050/notificationHub

        public NotificationService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task ConnectAsync()
        {
            if (_hubConnection?.State == HubConnectionState.Connected) return;

            var token = Preferences.Get("auth_token", string.Empty);
            if (string.IsNullOrEmpty(token)) return;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{BaseUrl}/notificationHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token)!;
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<Models.Notification>("ReceiveNotification", (notification) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationReceived?.Invoke(notification);
                });
            });

            _hubConnection.On<Guid>("RemovedNotification", (notificationId) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationRemove?.Invoke(notificationId);
                });
            });

            _hubConnection.On<Guid>("NotificationRead", (notificationId) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationRead?.Invoke(notificationId);
                });
            });

            _hubConnection.On<Models.Notification>("UpdateNotification", (notification) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationUpdate?.Invoke(notification);
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("SignalR connected");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"SignalR connection error: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("MarkAsRead", notificationId);
            }
        }

        public bool isConnected => _hubConnection?.State == HubConnectionState.Connected;
    }
}
