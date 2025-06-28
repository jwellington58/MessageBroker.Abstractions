using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Abstractions.Extensions
{
    public static class ConfigMessageBrokerExtensions
    {
        private static void AddBroker(this IServiceCollection services, Action<MessageBrokerOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddSingleton<IMessageBroker, AzureMessageBroker>();
        }
    }
}
