using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Geospiza.Lib.Helpers
{
    public static class Helpers
    {
        public static void SendRequest(List<MeshBody> dataList, string endpoint)
        {
            var json = JsonConvert.SerializeObject(dataList);
            using (var client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Replace 'your_server_endpoint' with your actual server endpoint
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



