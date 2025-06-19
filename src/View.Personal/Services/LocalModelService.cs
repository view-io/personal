namespace View.Personal.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text.Json;
    using View.Personal.Classes;
    using View.Personal.Enums;

    /// <summary>
    /// Service for managing locally pulled AI models.
    /// </summary>
    public class LocalModelService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly App _app;
        private List<LocalModel> _localModels;

        #endregion

        #region Constructor

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalModelService"/> class.
        /// </summary>
        /// <param name="app">The application instance.</param>
        public LocalModelService(App app)
        {
            _app = app;
            _localModels = new List<LocalModel>();
        }

        /// <summary>
        /// Gets the list of local models.
        /// </summary>
        /// <returns>A list of local models.</returns>
        public List<LocalModel> GetModels()
        {
            return _localModels;
        }

        /// <summary>
        /// Gets the list of local models asynchronously.
        /// </summary>
        /// <returns>A task that resolves to a list of local models.</returns>
        public async Task<List<LocalModel>> GetModelsAsync()
        {
            await LoadModelsAsync();
            return _localModels;
        }

        /// <summary>
        /// Checks if Ollama service is available and running.
        /// </summary>
        /// <returns>A task that resolves to true if Ollama is available, false otherwise.</returns>
        public async Task<bool> IsOllamaAvailableAsync()
        {
            try
            {
                string endpoint = GetOllamaEndpoint();
                using var httpClient = CreateHttpClient();
                // Set a short timeout to quickly determine if Ollama is available
                httpClient.Timeout = TimeSpan.FromSeconds(3);
                
                // Try to connect to the Ollama API endpoint
                var response = await httpClient.GetAsync($"{endpoint}api/tags");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _app?.Log(SeverityEnum.Error, $"Ollama service is not available: {ex.Message}");
                _app?.LogExceptionToFile(ex, $"Ollama service is not available");
                return false;
            }
        }

        /// <summary>
        /// Activates or deactivates a model.
        /// </summary>
        /// <param name="modelId">The ID of the model to update.</param>
        /// <param name="isActive">Whether the model should be active.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool SetModelActiveState(string modelId, bool isActive)
        {
            var model = _localModels.FirstOrDefault(m => m.Id == modelId);
            if (model == null)
            {
                return false;
            }

            model.IsActive = isActive;
            return true;
        }

        /// <summary>
        /// Deletes a model from the system.
        /// </summary>
        /// <param name="modelId">The ID of the model to delete.</param>
        /// <returns>A task that resolves to true if the operation was successful, false otherwise.</returns>
        public async Task<bool> DeleteModelAsync(string modelId)
        {
            var model = _localModels.FirstOrDefault(m => m.Id == modelId);
            if (model == null)
            {
                return false;
            }

            try
            {
                string endpoint = GetOllamaEndpoint();
                using var httpClient = CreateHttpClient();
                var deleteRequest = new { name = model.Name };
                var content = JsonContent.Create(deleteRequest);

                using var request = new HttpRequestMessage(HttpMethod.Delete, $"{endpoint}api/delete")
                {
                    Content = content
                };

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Reload models after deletion
                await LoadModelsAsync();
                return true;
            }
            catch (Exception ex)
            {
                _app?.Log(SeverityEnum.Error, $"Error deleting model {model.Name}: {ex.Message}");
                _app?.LogExceptionToFile(ex, $"Error deleting model {model.Name}");
                return false;
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Gets the normalized Ollama API endpoint URL. This method ensures that the endpoint URL
        /// always ends with a trailing slash and uses the default endpoint if none is configured.
        /// </summary>
        /// <returns>The normalized endpoint URL with trailing slash.</returns>
        private string GetOllamaEndpoint()
        {
            var endpoint = _app.ApplicationSettings?.Ollama?.Endpoint;

            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = "http://localhost:11434/";
            }

            if (!endpoint.EndsWith("/"))
            {
                endpoint += "/";
            }

            return endpoint;
        }

        /// <summary>
        /// Creates a new HttpClient instance for making HTTP requests to the Ollama API.
        /// </summary>
        /// <returns>A new HttpClient instance configured for Ollama API communication.</returns>
        private HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Logs an error message and exception to both the application log and the exception log file.
        /// </summary>
        /// <param name="errorMessage">The error message describing what operation failed.</param>
        /// <param name="ex">The exception that was thrown during the operation.</param>
        private void LogError(string errorMessage, Exception ex)
        {
            _app?.Log(SeverityEnum.Error, errorMessage);
            _app?.LogExceptionToFile(ex, errorMessage);
        }

        /// <summary>
        /// Loads models from the Ollama API and updates the internal model list. This method
        /// fetches all available models from the Ollama server and converts them to LocalModel instances.
        /// If an error occurs during the fetch operation, the model list will be reset to empty.
        /// </summary>
        private async Task LoadModelsAsync()
        {
            try
            {
                var models = await FetchOllamaModelsAsync();
                _localModels = models;
            }
            catch (Exception ex)
            {
                _app.Log(SeverityEnum.Error, $"Error loading models: {ex.Message}");
                _app.LogExceptionToFile(ex, $"Error loading models");

                _localModels = new List<LocalModel>();
            }
        }

        /// <summary>
        /// Fetches models from the Ollama API by making an HTTP request to the tags endpoint.
        /// This method retrieves all available models from the Ollama server and converts them
        /// to LocalModel instances. If an error occurs during the fetch operation, an empty list is returned.
        /// </summary>
        /// <returns>A list of LocalModel instances representing the models available on the Ollama server.</returns>
        private async Task<List<LocalModel>> FetchOllamaModelsAsync()
        {
            var models = new List<LocalModel>();
            string endpoint = GetOllamaEndpoint();

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetFromJsonAsync<OllamaModelsResponse>($"{endpoint}api/tags");

                if (response?.Models != null)
                {
                    foreach (var ollamaModel in response.Models)
                    {
                        models.Add(ConvertToLocalModel(ollamaModel));
                    }
                }
            }
            catch (Exception ex)
            {
                _app?.Log(SeverityEnum.Error, $"Error fetching Ollama models: {ex.Message}");
                _app?.LogExceptionToFile(ex, $"Error fetching Ollama models");
            }

            return models;
        }

        /// <summary>
        /// Converts an OllamaModel instance to a LocalModel instance. This method maps properties from
        /// the Ollama API model representation to the application's internal model representation.
        /// It also extracts and formats additional information such as parameter count and quantization level.
        /// </summary>
        /// <param name="ollamaModel">The Ollama model instance to convert, containing data from the Ollama API.</param>
        /// <returns>A LocalModel instance populated with data from the OllamaModel.</returns>
        private LocalModel ConvertToLocalModel(OllamaModel ollamaModel)
        {
            string parameterCount = ollamaModel.Details.ParameterSize;
            if (string.IsNullOrEmpty(parameterCount))
            {
                var paramMatch = System.Text.RegularExpressions.Regex.Match(ollamaModel.Name, "[0-9]+[bB]");
                if (paramMatch.Success)
                {
                    parameterCount = paramMatch.Value.ToUpper();
                }
                else
                {
                    parameterCount = "Unknown";
                }
            }

            return new LocalModel
            {
                Id = ollamaModel.Name,
                Name = ollamaModel.Name,
                Description = $"{ollamaModel.Model} model",
                Provider = "Ollama",
                LocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ollama", "models", ollamaModel.Name),
                SizeInBytes = ollamaModel.Size,
                PulledDate = string.IsNullOrEmpty(ollamaModel.ModifiedAt) ?
                    DateTime.UtcNow :
                    DateTime.Parse(ollamaModel.ModifiedAt),
                IsActive = true,
                Version = "1.0",
                ParameterCount = parameterCount,
                Quantization = ollamaModel.Details.QuantizationLevel ?? "Unknown"
            };
        }

        /// <summary>
        /// Pulls a new model from the specified provider. This method initiates a download of the model
        /// from the Ollama API, processes the streaming response to track download progress, and adds
        /// the model to the local model list upon successful completion. The method provides progress
        /// updates through the optional callback and supports cancellation through the cancellation token.
        /// </summary>
        /// <param name="modelName">The name of the model to pull (e.g., "llama2", "mistral").</param>
        /// <param name="provider">The provider of the model (currently only "Ollama" is supported).</param>
        /// <param name="progressCallback">Optional callback to report download progress. The callback receives OllamaPullResponse objects containing status information.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation if needed.</param>
        /// <returns>The newly pulled model as a LocalModel instance, or null if the operation failed.</returns>
        public async Task<LocalModel?> PullModelAsync(string modelName, string provider, Action<Classes.OllamaPullResponse>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            string endpoint = GetOllamaEndpoint();

            try
            {
                using var httpClient = CreateHttpClient();
                var pullRequest = new { name = modelName };
                var content = JsonContent.Create(pullRequest);

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}api/pull")
                {
                    Content = content
                };

                HttpResponseMessage response;
                try
                {
                    response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException httpEx)
                {
                    LogError($"HTTP error pulling model {modelName}: {httpEx.Message}", httpEx);

                    var errorResponse = new OllamaPullResponse
                    {
                        Status = "error",
                        Error = httpEx.Message
                    };

                    if (httpEx.Message.Contains("400") || httpEx.Message.Contains("Bad Request"))
                    {
                        errorResponse.Error = $"Invalid model name '{modelName}'. Please check if the model name is correct.";
                    }

                    progressCallback?.Invoke(errorResponse);
                    return null;
                }

                await ProcessModelPullStreamAsync(response, modelName, progressCallback, cancellationToken);

                await LoadModelsAsync();
                return _localModels.FirstOrDefault(m => m.Name == modelName);
            }
            catch (Exception ex)
            {
                LogError($"Error pulling model {modelName}: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Processes the streaming response from the model pull operation. This method reads the HTTP response
        /// stream line by line, deserializes each line into an OllamaPullResponse object, and invokes the
        /// progress callback with the response data. It handles special cases such as error responses and
        /// adjusts the progress data when needed. The method continues processing until the stream ends
        /// or the operation is cancelled.
        /// </summary>
        /// <param name="response">The HTTP response containing the streaming data from the Ollama API.</param>
        /// <param name="modelName">The name of the model being pulled, used for error logging.</param>
        /// <param name="progressCallback">Callback to report progress updates to the caller.</param>
        /// <param name="cancellationToken">Cancellation token to stop processing if the operation is cancelled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessModelPullStreamAsync(
            HttpResponseMessage response,
            string modelName,
            Action<OllamaPullResponse>? progressCallback,
            CancellationToken cancellationToken)
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var pullResponse = JsonSerializer.Deserialize<OllamaPullResponse>(line, options);
                    if (pullResponse != null)
                    {
                        if (!string.IsNullOrEmpty(pullResponse.Error) &&
                            pullResponse.Error.Contains("pull model manifest: file does not exist"))
                        {
                            _app?.Log(SeverityEnum.Error, $"Invalid model name detected: {modelName}. Model manifest does not exist.");
                            progressCallback?.Invoke(pullResponse);

                            return;
                        }

                        if (pullResponse.Total == 0 && pullResponse.Completed > 0)
                        {
                            pullResponse.Total = pullResponse.Completed;
                        }

                        progressCallback?.Invoke(pullResponse);
                    }
                }
                catch (JsonException jsonEx)
                {
                    LogError($"JSON parsing error in pull response: {jsonEx.Message}. Line: {line}", jsonEx);
                }
                catch (Exception ex)
                {
                    LogError($"Error processing pull response: {ex.Message}", ex);
                }
            }
        }
    }

    #endregion
}
