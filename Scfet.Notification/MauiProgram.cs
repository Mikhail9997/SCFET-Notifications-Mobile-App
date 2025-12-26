using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Scfet.Notification.Services;
using Scfet.Notification.ViewModels;
using Scfet.Notification.Views;

namespace Scfet.Notification
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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
            builder.Services.AddSingleton<IApiService, ApiService>();
            builder.Services.AddSingleton<NotificationService>();
            builder.Services.AddSingleton<LoginService>();
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<NotificationPermissionsService>();

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<NotificationsViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<CreateNotificationViewModel>();
            builder.Services.AddTransient<SentNotificationsViewModel>();
            builder.Services.AddTransient<EditNotificationViewModel>();

            // Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<NotificationsPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<CreateNotificationPage>();
            builder.Services.AddTransient<SentNotificationsPage>();
            builder.Services.AddTransient<EditNotificationPage>();

            return builder.Build();
        }
    }
}
