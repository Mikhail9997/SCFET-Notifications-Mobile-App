using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Services
{
    public class NotificationPermissionsService
    {
        public async Task<bool> CheckAndRequestNotificationPermission()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                return await CheckAndRequestAndroidNotificationPermission();
            }

            return true;
        }

        // Для Android
        private async Task<bool> CheckAndRequestAndroidNotificationPermission()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33)) // Android 13+
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
        
                return status == PermissionStatus.Granted;
            }
            return true; // Для версий ниже Android 13 разрешение не требуется
        }
    }
}
