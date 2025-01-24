using System.Text;
using GeospizaCore.Utils;

namespace GeospizaCore.ParallelSpiza;

public class DataSender
{
  // Create a static HttpClient instance to be reused for sending HTTP requests.
  private static readonly HttpClient client = new();

  /// <summary>
  /// Sends data to the specified URL using an HTTP POST request.
  /// </summary>
  /// <param name="data">The data object to be sent.</param>
  /// <param name="instanceId">A unique identifier for the instance.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the response content as a string if the request is successful; otherwise, null.</returns>
  public static async Task<string> SendDataAsync(object data, Guid instanceId)
  {
    // Define the URL to which the data will be sent.
    var url = $"{SharedVars.RootURL}/";

    // Serialize the data object to a JSON string.
    // Create a StringContent object with the JSON string, specifying the content type as application/json.
    var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

    //TODO: ADD SOMETING TO TRACK FROM WICH INSTANCE THE DATA CAME FROMs
    // Add an InstanceId header to the content with the value of instanceId.
    content.Headers.Add("InstanceId", instanceId.ToString());

    // Send a POST request to the specified URL with the content.
    var response = await client.PostAsync(url, content);

    // Check if the response indicates success.
    if (response.IsSuccessStatusCode)
    {
      // Print a success message to the console.
      Console.WriteLine("Data sent successfully.");

      // Return the response content as a string.
      return await response.Content.ReadAsStringAsync();
    }
    else
    {
      // Print a failure message to the console with the status code.
      Console.WriteLine($"Failed to send data. Status code: {response.StatusCode}");

      // Return null to indicate failure.
      return null;
    }
  }
}