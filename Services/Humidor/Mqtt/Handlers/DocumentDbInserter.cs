using System;
using System.Threading.Tasks;
using Humidor.Model;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HomeIot.Infrastructure.Mqtt;

namespace Humidor.Mqtt.Handlers
{
    public class DocumentDbInserter : IMqttEventHandler<HumidorSensorData>
    {
        private readonly ILogger<DocumentDbInserter> _logger;
        private readonly DocumentDbInserterConfig _config;

        public DocumentDbInserter(
            ILogger<DocumentDbInserter> logger,
            IOptions<DocumentDbInserterConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task Handle(HumidorSensorData sensorData)
        {
            var collectionLink = UriFactory.CreateDocumentCollectionUri(_config.DatabaseId, _config.CollectionId);

            try
            {
                var client = new DocumentClient(new Uri(_config.ServiceEndpoint), _config.AuthKey);
                await client.CreateDocumentAsync(collectionLink, sensorData);
                _logger.LogDebug("Document successfully written to DocumentDb");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public class DocumentDbInserterConfig
    {
        public string ServiceEndpoint { get; set; }
        public string AuthKey { get; set; }
        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }
    }
}
