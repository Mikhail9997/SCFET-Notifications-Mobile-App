using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Scfet.Notification.Services;
using Scfet.Notification.ViewModels;
using Scfet.Notification.Views;
using FFImageLoading.Maui;
using Scfet.Notification.Handlers;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseFFImageLoading()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            const string baseUrl = "https://amorously-preeminent-godwit.cloudpub.ru/api/";
            //https://amorously-preeminent-godwit.cloudpub.ru/api/
            //http://81.94.159.27:5050/api/

            // Services
            builder.Services.AddSingleton<ITokenService, TokenService>();
            builder.Services.AddSingleton<SignalRService>();
            builder.Services.AddSingleton<LoginService>();
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<NotificationPermissionsService>();
            builder.Services.AddSingleton<IPickImageService, PickImageService>();
            builder.Services.AddSingleton<AuthHandler>();
            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var tokenService = sp.GetRequiredService<ITokenService>();
                var loginService = sp.GetRequiredService<LoginService>();

                var handler = new AuthHandler(tokenService, loginService)
                {
                    InnerHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    }
                };

                return new HttpClient(handler)
                {
                    BaseAddress = new Uri(baseUrl)
                };
            });

            // Регистрация HTTP клиентов для каждого сервиса
            builder.Services.AddSingleton<IAuthApiService, AuthApiService>();
            builder.Services.AddSingleton<IProfileApiService, ProfileApiService>();
            builder.Services.AddSingleton<INotificationsApiService, NotificationsApiService>();
            builder.Services.AddSingleton<IRepliesApiService, RepliesApiService>();
            builder.Services.AddSingleton<IUsersApiService, UsersApiService>();
            builder.Services.AddSingleton<IFavoritesApiService, FavoritesApiService>();
            builder.Services.AddSingleton<IChannelApiService, ChannelApiService>();
            builder.Services.AddSingleton<IChannelMessageApiService, ChannelMessageApiService>();

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<NotificationsViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<CreateNotificationViewModel>();
            builder.Services.AddTransient<SentNotificationsViewModel>();
            builder.Services.AddTransient<EditNotificationViewModel>();
            builder.Services.AddTransient<RepliesViewModel>();
            builder.Services.AddTransient<AppShellViewModel>();
            builder.Services.AddTransient<AvatarsViewModel>();
            builder.Services.AddTransient<FavoritesViewModel>();
            builder.Services.AddTransient<ChannelsViewModel>();
            builder.Services.AddTransient<CreateChannelViewModel>();
            builder.Services.AddTransient<ChannelInvitationsViewModel>();
            builder.Services.AddTransient<InviteUsersViewModel>();
            builder.Services.AddTransient<ChannelMembersViewModel>();
            builder.Services.AddTransient<ChannelMessagesViewModel>();

            // Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<NotificationsPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<CreateNotificationPage>();
            builder.Services.AddTransient<SentNotificationsPage>();
            builder.Services.AddTransient<EditNotificationPage>();
            builder.Services.AddTransient<RepliesPage>();
            builder.Services.AddTransient<AvatarsPage>();
            builder.Services.AddTransient<FavoritesPage>();
            builder.Services.AddTransient<ChannelsPage>();
            builder.Services.AddTransient<CreateChannelPage>();
            builder.Services.AddTransient<ChannelInvitationsPage>();
            builder.Services.AddTransient<InviteUsersPage>();
            builder.Services.AddTransient<ChannelMembersPage>();
            builder.Services.AddTransient<ChannelMessagesPage>();

            return builder.Build();
        }
    }
}
