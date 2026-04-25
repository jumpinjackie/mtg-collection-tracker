using System.Text.Json;

namespace MtgCollectionTracker.ApiClient.Generated
{
    /// <summary>
    /// Partial class that configures the generated HTTP client to use camelCase JSON serialization,
    /// matching the ASP.NET Core default (<see cref="JsonSerializerDefaults.Web"/>).
    /// </summary>
    public partial class MtgCollectionTrackerApiClient
    {
        static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
        {
            settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            settings.PropertyNameCaseInsensitive = true;
            settings.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
        }
    }
}
