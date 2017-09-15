using System.Collections.Generic;
using System.Threading.Tasks;
using Humidor.Mqtt;
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
            await _mqttService.Publish("humidor/cmd/blabla", "Dette er en test!");

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
