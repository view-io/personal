namespace View.Personal.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
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
            
            var endpoint = _app.ApplicationSettings?.Ollama?.Endpoint;
            
            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = "http://localhost:11434/";
            }
            
            if (!endpoint.EndsWith("/"))
            {
                endpoint += "/";
            }
            
            try
            {
                using var httpClient = new HttpClient();
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
                _app?.Log($"Error deleting model {model.Name}: {ex.Message}");
                _app?.LogExceptionToFile(ex, $"Error deleting model {model.Name}");
                return false;
            }
        }

        #endregion

        #region Private-Methods

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
                _app.Log($"Error loading models: {ex.Message}");
                _app.LogExceptionToFile(ex,$"Error loading models");
          
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
            var endpoint = _app.ApplicationSettings?.Ollama?.Endpoint;
            
            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = "http://localhost:11434/";
            }
            
            if (!endpoint.EndsWith("/"))
            {
                endpoint += "/";
            }
            
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetFromJsonAsync<OllamaModelsResponse>($"{endpoint}api/tags");
                
                if (response?.Models != null)
                {
                    foreach (var ollamaModel in response.Models)
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
                        
                        var localModel = new LocalModel
                        {
                            // Use the model name as a stable ID to ensure consistency between fetches
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
                        
                        models.Add(localModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _app.Log($"Error fetching Ollama models: {ex.Message}");
                _app.LogExceptionToFile(ex,$"Error fetching Ollama models");
            }
            
            return models;
        }

        /// <summary>
        /// Pulls a new model from the provider.
        /// </summary>
        /// <param name="modelName">The name of the model to pull.</param>
        /// <param name="provider">The provider of the model.</param>
        /// <param name="progressCallback">Optional callback to report download progress.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>The newly pulled model, or null if the operation failed.</returns>
        public async Task<LocalModel?> PullModelAsync(string modelName, string provider, Action<Classes.OllamaPullResponse>? progressCallback = null, System.Threading.CancellationToken cancellationToken = default)
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
            
            try
            {
                using var httpClient = new HttpClient();
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
                    _app?.Log($"[ERROR] HTTP error pulling model {modelName}: {httpEx.Message}");
                    _app?.LogExceptionToFile(httpEx, $"HTTP error pulling model {modelName}");
                    
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
                
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);
                
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    
                    try 
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var pullResponse = JsonSerializer.Deserialize<Classes.OllamaPullResponse>(line, options);
                        if (pullResponse != null)
                        {
                            if (!string.IsNullOrEmpty(pullResponse.Error) && 
                                pullResponse.Error.Contains("pull model manifest: file does not exist"))
                            {
                                _app?.Log($"Invalid model name detected: {modelName}. Model manifest does not exist.");
                                progressCallback?.Invoke(pullResponse);
                                
                                return null;
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
                        _app?.Log($"[ERROR] JSON parsing error in pull response: {jsonEx.Message}. Line: {line}");
                        _app?.LogExceptionToFile(jsonEx, $"JSON parsing error in pull response");
                    }
                    catch (Exception ex)
                    {
                        _app?.Log($"[ERROR] Error processing pull response: {ex.Message}");
                        _app?.LogExceptionToFile(ex,$"Error processing pull response");
                    }
                }
                
                await LoadModelsAsync();
                return _localModels.FirstOrDefault(m => m.Name == modelName);
            }
            catch (Exception ex)
            {
                _app?.Log($"[ERROR] Error pulling model {modelName}: {ex.Message}");
                _app?.LogExceptionToFile(ex,$"Error pulling model {modelName}");
            }
            
            return null;
        }

        #endregion
    }
}