using Scfet.Notification.Views;

namespace Scfet.Notification
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            RegisterRoutes();
        }

        public void RegisterRoutes()
        {
            Routing.RegisterRoute("CreateNotificationPage", typeof(CreateNotificationPage));
            Routing.RegisterRoute("SentNotificationsPage", typeof(SentNotificationsPage));
        }
    }
}
