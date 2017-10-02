using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeIot.Infrastructure.Mqtt
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMqttService(this IServiceCollection services)
        {
            services.AddSingleton<IHostedService, MqttService>();
            services.AddSingleton(provider => provider.GetRequiredService<IHostedService>() as IMqttService);
            services.AddTransient<IMqttMessageSerializer, MqttMessageJsonSerializer>();

            services.AddTransient(factory =>
            {
                Func<Type, IEnumerable<IMqttEventHandler>> accessor = type =>
                {
                    var genericType = typeof(IMqttEventHandler<>).MakeGenericType(type);
                    return factory.GetServices(genericType) as IEnumerable<IMqttEventHandler>;
                };
                return accessor;
            });

            services.AddTransient(factory =>
            {
                Func<Type, IEnumerable<IMqttMessageEnricher>> accessor = type =>
                {
                    var genericType = typeof(IMqttMessageEnricher<>).MakeGenericType(type);
                    return factory.GetServices(genericType) as IEnumerable<IMqttMessageEnricher>;
                };
                return accessor;
            });

        }
    }
}
