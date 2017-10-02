using System;
using System.Collections.Generic;
using Humidor.Model;
using Humidor.Mqtt.Enrichers;
using Humidor.Mqtt.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HomeIot.Infrastructure.Mqtt;

namespace Humidor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging()
                .AddMvc();

            services.AddMqttService();
            services.AddTransient<IMqttMessageEnricher<HumidorSensorData>, UtcDateTimeEnricher>();
            services.AddTransient<IMqttEventHandler<HumidorSensorData>, SensorDataLogger>();
            services.AddTransient<IMqttEventHandler<HumidorSensorData>, DocumentDbInserter>();

            services.AddTransient(factory =>
            {
                var dict = new Dictionary<string, Type>
                {
                    { "humidor/sensors", typeof(HumidorSensorData) }
                };

                Func<string, Type> map = key => dict[key.ToLower()];
                return map;
            });

            services.Configure<DocumentDbInserterConfig>(Configuration.GetSection("DocumentDB"));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
