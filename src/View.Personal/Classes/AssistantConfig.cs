namespace View.Personal.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class AssistantConfig
    {
        [JsonPropertyName("GUID")] public string GUID { get; set; }
        [JsonPropertyName("Name")] public string Name { get; set; }

        [JsonPropertyName("Description")] public string Description { get; set; }

        [JsonPropertyName("CreatedUTC")] public DateTime CreatedUTC { get; set; }
    }

    public class AssistantConfigResponse
    {
        [JsonPropertyName("AssistantConfigs")] public List<AssistantConfig> AssistantConfigs { get; set; }
    }
}