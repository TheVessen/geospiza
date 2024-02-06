using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Geospiza.Lib.Helpers
{
    public static class Helpers
    {
        public static void SendRequest(MeshBody datalist)
        {
            var json = datalist.ToJson();
            using (var client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Replace 'your_server_endpoint' with your actual server endpoint
                var result = client.PostAsync("http://127.0.0.1:5173/api/geoget", content).Result;

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



