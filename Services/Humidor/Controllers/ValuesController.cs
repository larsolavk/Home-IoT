using System.Collections.Generic;
using System.Threading.Tasks;
using Humidor.Mqtt;
using HomeIot.Infrastructure.Mqtt;
using Microsoft.AspNetCore.Mvc;

namespace Humidor.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly IMqttService _mqttService;

        public ValuesController(IMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            await _mqttService.Publish("humidor/sensors", "{\"Humidity\":65.00, \"Temperature\":21.40, \"Voltage\":2.52}");

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
