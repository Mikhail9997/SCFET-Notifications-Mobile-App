using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Scfet.Notification.Services;
using Scfet.Notification.ViewModels;
using Scfet.Notification.Views;
using FFImageLoading.Maui;

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

            // Services
            builder.Services.AddSingleton<ITokenService, TokenService>();
            builder.Services.AddSingleton<IApiService, ApiService>();
            builder.Services.AddSingleton<NotificationService>();
            builder.Services.AddSingleton<LoginService>();
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<NotificationPermissionsService>();
            builder.Services.AddSingleton<IPickImageService, PickImageService>();

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

            return builder.Build();
        }
    }
}
