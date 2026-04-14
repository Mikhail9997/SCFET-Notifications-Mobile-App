using Scfet.Notification.ViewModels;
using Scfet.Notification.Views;

namespace Scfet.Notification
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            RegisterRoutes();
        }

        public void RegisterRoutes()
        {
            Routing.RegisterRoute("CreateNotificationPage", typeof(CreateNotificationPage));
            Routing.RegisterRoute("SentNotificationsPage", typeof(SentNotificationsPage));
            Routing.RegisterRoute("EditNotificationPage", typeof(EditNotificationPage));
            Routing.RegisterRoute("RepliesPage", typeof(RepliesPage));
            Routing.RegisterRoute("AvatarsPage", typeof(AvatarsPage));
            Routing.RegisterRoute("CreateChannelPage", typeof(CreateChannelPage));
            Routing.RegisterRoute("ChannelInvitationsPage", typeof(ChannelInvitationsPage));
            Routing.RegisterRoute("InviteUsersPage", typeof(InviteUsersPage));
            Routing.RegisterRoute("ChannelMembersPage", typeof(ChannelMembersPage));
            Routing.RegisterRoute("ChannelMessagesPage", typeof(ChannelMessagesPage));
        }
    }
}
