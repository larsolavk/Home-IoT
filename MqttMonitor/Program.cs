using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HomeIot.Infrastructure.Mqtt.MqttNetAdapter;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MqttMonitor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostInfo = await MqttBrokerResolver.ResolveMqttBroker("_mqtt._tcp.local.");
            if (hostInfo == null)
                return;
            
            Console.WriteLine($"Conncting to {hostInfo.DisplayName} ({hostInfo.IpAddress}:{hostInfo.Port})");

            var client = new CustomMqttClientFactory().CreateMqttClient(new MqttClientOptions
            {
                Server = hostInfo.IpAddress,
                Port = hostInfo.Port,
                TlsOptions = new MqttClientTlsOptions
                {
                    UseTls = true,
                    Certificates = new List<byte[]>
                    {
                        new X509Certificate2(@"c:\source\cert\LarsOlav-PC.crt.pfx")
                            .Export(X509ContentType.SerializedCert)
                    },
                    CheckCertificateRevocation = true
                }
            });

            client.Connected += async (sender, eventArgs) =>
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");

                await client.SubscribeAsync(new List<TopicFilter>
                {
                    new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce)
                });

                Console.WriteLine("### SUBSCRIBED ###");
            };

            client.Disconnected += async (s, e) =>
            {
                Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await client.ConnectAsync();
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            };

            client.ApplicationMessageReceived += (sender, eventArgs) =>
            {
                Console.WriteLine($"{DateTime.Now:yyyy.MM.dd HH:mm:ss} - {eventArgs.ApplicationMessage.Topic} {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)}");
            };

            try
            {
                await client.ConnectAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("### CONNECTING FAILED ###");
                Console.WriteLine(e);
            }

            Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

            while (true)
            {
                Console.ReadLine();

                var applicationMessage = new MqttApplicationMessage(
                    "A/B/C",
                    Encoding.UTF8.GetBytes("Hello World"),
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    false
                );

                await client.PublishAsync(applicationMessage);
            }

        }
    }
}
