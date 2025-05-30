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
                _app?.Log($"[Error] Error deleting model {model.Name}: {ex.Message}");
                _app?.LogExceptionToFile(ex, $"Error deleting model {model.Name}");
                return false;
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Gets the normalized Ollama API endpoint URL
        /// </summary>
        /// <returns>The normalized endpoint URL with trailing slash</returns>
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
        /// Creates a new HttpClient instance
        /// </summary>
        /// <returns>A new HttpClient instance</returns>
        private HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Logs an error message and exception
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="ex">The exception</param>
        private void LogError(string errorMessage, Exception ex)
        {
            _app?.Log(errorMessage);
            _app?.LogExceptionToFile(ex, errorMessage);
        }

        /// <summary>
        /// Loads models from Ollama API
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
                _app.Log($"[Error] Error loading models: {ex.Message}");
                _app.LogExceptionToFile(ex, $"Error loading models");

                _localModels = new List<LocalModel>();
            }
        }

        /// <summary>
        /// Fetches models from Ollama API
        /// </summary>
        /// <returns>A list of local models from Ollama</returns>
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
                _app?.Log($"[Error] Error fetching Ollama models: {ex.Message}");
                _app?.LogExceptionToFile(ex, $"Error fetching Ollama models");
            }

            return models;
        }

        /// <summary>
        /// Converts an OllamaModel to a LocalModel
        /// </summary>
        /// <param name="ollamaModel">The Ollama model to convert</param>
        /// <returns>A LocalModel instance</returns>
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
        /// Pulls a new model from the provider.
        /// </summary>
        /// <param name="modelName">The name of the model to pull.</param>
        /// <param name="provider">The provider of the model.</param>
        /// <param name="progressCallback">Optional callback to report download progress.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>The newly pulled model, or null if the operation failed.</returns>
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
                        errorResponse.Error = $"[Error] Invalid model name '{modelName}'. Please check if the model name is correct.";
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
                LogError($"[Error] Error pulling model {modelName}: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Processes the streaming response from the model pull operation
        /// </summary>
        /// <param name="response">The HTTP response</param>
        /// <param name="modelName">The name of the model being pulled</param>
        /// <param name="progressCallback">Callback to report progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
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
                            _app?.Log($"[Error] Invalid model name detected: {modelName}. Model manifest does not exist.");
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
                    LogError($"[Error] JSON parsing error in pull response: {jsonEx.Message}. Line: {line}", jsonEx);
                }
                catch (Exception ex)
                {
                    LogError($"[Error] Error processing pull response: {ex.Message}", ex);
                }
            }
        }
    }

    #endregion
}
