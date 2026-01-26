using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Scfet.Notification.Policies
{
    public class SignalRRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            // Экспоненциальная задержка с максимальным значением
            var delay = Math.Min(30, Math.Pow(2, retryContext.PreviousRetryCount));

            // Добавляем случайность для предотвращения одновременных переподключений
            var jitter = new Random().NextDouble() * 0.2 + 0.9; // 0.9 - 1.1
            var finalDelay = delay * jitter;

            return TimeSpan.FromSeconds(finalDelay);
        }
    }
}
