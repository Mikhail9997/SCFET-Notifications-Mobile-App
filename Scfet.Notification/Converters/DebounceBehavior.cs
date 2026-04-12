using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Scfet.Notification.Converters
{
    public class DebounceBehavior : Behavior<Entry>
    {
        private IDisposable _debounceSubscription;

        public static readonly BindableProperty DelayProperty =
            BindableProperty.Create(nameof(Delay), typeof(int), typeof(DebounceBehavior), 500);

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(DebounceBehavior));

        public int Delay
        {
            get => (int)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            entry.TextChanged += OnTextChanged;
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            base.OnDetachingFrom(entry);
            entry.TextChanged -= OnTextChanged;
            _debounceSubscription?.Dispose();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceSubscription?.Dispose();

            _debounceSubscription = Observable.Timer(TimeSpan.FromMilliseconds(Delay))
                .Subscribe(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (Command?.CanExecute(null) == true)
                        {
                            Command.Execute(null);
                        }
                    });
                });
        }
    }
}
