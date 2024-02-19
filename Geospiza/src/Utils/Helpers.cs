using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Geospiza
{
    public static class Helpers
    {
        public static void SendRequest(List<WebIndividual> dataList, string endpoint)
        {
            var jsonList = dataList.Select(individual => individual.ToAnonymousObject()).ToList();
            var json = JsonConvert.SerializeObject(jsonList);
    
            using (var client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var result = client.PostAsync(endpoint, content).Result;

                if (result.IsSuccessStatusCode)
                {
                    // Handle success
                }
                else
                {
                    // Handle failure
                }
            }
        }
    }
};



