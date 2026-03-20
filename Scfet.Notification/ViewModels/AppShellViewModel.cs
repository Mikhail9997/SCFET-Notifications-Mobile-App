using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Scfet.Notification.ViewModels
{
    public partial class AppShellViewModel:ObservableObject
    {
        public AppShellViewModel() { }

        [ObservableProperty]
        private string appVersion = $"Версия {AppInfo.Current.VersionString} (сборка {AppInfo.Current.BuildString})";
        [ObservableProperty]
        private int year = DateTime.Now.Year;
    }
}
