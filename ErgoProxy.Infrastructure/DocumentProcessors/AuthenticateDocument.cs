namespace ErgoProxy.Infrastructure.DocumentProcessors;

using ErgoProxy.Domain.Document;
using ErgoProxy.Domain.SharedKernel;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

public class AuthenticateDocument : IDocumentUseCaseSelector<AuthenticateDocumentDto>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthenticateDocument(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GenericResponse<object>> ExecuteAsync(AuthenticateDocumentDto body)
    {
        var client = _httpClientFactory.CreateClient("GovCarpeta");
        var request = new HttpRequestMessage(HttpMethod.Put, "/apis/authenticateDocument");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseData = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        return new GenericResponse<object>() { Data = responseData, Message = responseData, StatusCode = statusCode };
    }
}
