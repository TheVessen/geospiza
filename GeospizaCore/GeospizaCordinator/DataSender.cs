using System.Text;
using Newtonsoft.Json;

public class DataSender
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task SendDataAsync(object data, Guid instanceId)
    {
        var url = "http://localhost:8080/data";
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("InstanceId", instanceId.ToString());

        var response = await client.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Data sent successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to send data. Status code: {response.StatusCode}");
        }
    }
}