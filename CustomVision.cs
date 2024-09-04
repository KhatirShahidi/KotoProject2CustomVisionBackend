using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace birdDetector
{
    public class CustomVision
    {
        private readonly ILogger<CustomVision> _logger;
        private static readonly HttpClient _client = new HttpClient();

        public CustomVision(ILogger<CustomVision> logger)
        {
            _logger = logger;
            // Set a default User-Agent header to comply with User-Agent policies of most servers
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AzureFunction/1.0)");
        }

        [Function("CustomVision")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Retrieve configuration settings from environment variables
            string predictionEndpoint = Environment.GetEnvironmentVariable("CUSTOM_VISION_ENDPOINT");
            string predictionKey = Environment.GetEnvironmentVariable("CUSTOM_VISION_KEY");
            string projectId = Environment.GetEnvironmentVariable("CUSTOM_VISION_PROJECT_ID");
            string modelName = Environment.GetEnvironmentVariable("CUSTOM_VISION_MODEL_NAME");

            // Construct the full prediction URL using the project ID and model name
            string predictionUrl = $"{predictionEndpoint}/customvision/v3.0/Prediction/{projectId}/detect/iterations/{modelName}/image";

            // Check if the request contains an image file
            if (req.HasFormContentType && req.Form.Files.Count > 0)
            {
                var file = req.Form.Files[0];
                if (file.Length == 0)
                {
                    _logger.LogWarning("The provided image file is empty.");
                    return new BadRequestObjectResult("The provided image file is empty.");
                }

                // Read and send the image file
                using (var imageStream = file.OpenReadStream())
                {
                    return await SendImageToCustomVision(predictionUrl, predictionKey, imageStream);
                }
            }

            // Check if the request contains JSON with a URL
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string imageUrl = data?.Url;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                try
                {
                    // Set the User-Agent header to avoid 403 errors due to User-Agent policies
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                    requestMessage.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AzureFunction/1.0)");

                    // Send the request and get the image stream
                    var response = await _client.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode(); // Throw if the response is not successful
                    var imageStream = await response.Content.ReadAsStreamAsync();

                    return await SendImageToCustomVision(predictionUrl, predictionKey, imageStream);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error downloading image from URL: {ex.Message}");
                    return new BadRequestObjectResult("Failed to download image from the provided URL.");
                }
            }

            _logger.LogWarning("No image file or URL provided in the request.");
            return new BadRequestObjectResult("Please provide an image file or a URL.");
        }

        private async Task<IActionResult> SendImageToCustomVision(string predictionUrl, string predictionKey, Stream imageStream)
        {
            try
            {
                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);

                using (var content = new StreamContent(imageStream))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    // Send the image to the Custom Vision API
                    var response = await _client.PostAsync(predictionUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Prediction failed with status: {response.StatusCode}, reason: {response.ReasonPhrase}");
                        return new StatusCodeResult((int)response.StatusCode);
                    }

                    // Read and parse the JSON response from the Custom Vision API
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var predictions = JsonConvert.DeserializeObject(jsonResponse);

                    // Log and return the predictions
                    _logger.LogInformation($"Predictions: {jsonResponse}");
                    return new OkObjectResult(predictions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while processing the image: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
