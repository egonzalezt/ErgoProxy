namespace ErgoProxy.Workers.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class ApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _apiUri;

    public ApiHealthCheck(IHttpClientFactory httpClientFactory, Uri apiUri)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _apiUri = apiUri ?? throw new ArgumentNullException(nameof(apiUri));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(_apiUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }
            else
            {
                return HealthCheckResult.Unhealthy($"API returned status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Error checking API health: {ex.Message}");
        }
    }
}