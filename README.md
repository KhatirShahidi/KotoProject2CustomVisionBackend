# KotoProject2CustomVisionBackend
### CustomVision Azure Function

The `CustomVision` function is an Azure Function designed to process image predictions using the Azure Custom Vision API. It handles HTTP GET and POST requests to analyze images either uploaded directly as files or provided via URLs in JSON payloads.

#### Features

- **Image Input Options**: Accepts images through form uploads or URLs specified in the JSON body of the request.
- **Azure Custom Vision Integration**: Uses Azure Custom Vision API to detect objects in the provided images.
- **Error Handling**: Includes checks for empty image files, handles download failures for image URLs, and logs prediction errors for troubleshooting.
- **Configuration**: Retrieves necessary configuration settings from environment variables, including:
  - `CUSTOM_VISION_ENDPOINT`
  - `CUSTOM_VISION_KEY`
  - `CUSTOM_VISION_PROJECT_ID`
  - `CUSTOM_VISION_MODEL_NAME`

#### How to Use

1. **Endpoint Configuration**: Ensure that the environment variables for the Custom Vision endpoint, prediction key, project ID, and model name are correctly set up in your Azure environment.
   
2. **Sending Requests**:
   - **Image File**: Send a POST request with form data containing an image file.
   - **Image URL**: Send a POST request with a JSON payload including an `Url` key pointing to the image location.
   
3. **Response**:
   - Returns prediction results as JSON on success.
   - Provides appropriate error messages and HTTP status codes for various error scenarios (e.g., missing images, failed downloads).

#### Example Request

- **Form Upload**:
  
  ```http
  POST /api/CustomVision
  Content-Type: multipart/form-data

  (form-data with image file)
  ```

- **JSON with URL**:

  ```http
  POST /api/CustomVision
  Content-Type: application/json

  {
    "Url": "https://example.com/image.jpg"
  }
  ```

#### Logging and Troubleshooting

- The function logs all key actions and errors to help monitor its operations and troubleshoot issues effectively. Ensure your Azure Function App's logging is enabled to capture these logs.
