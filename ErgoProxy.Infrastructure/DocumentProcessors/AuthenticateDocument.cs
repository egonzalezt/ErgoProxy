namespace ErgoProxy.Infrastructure.DocumentProcessors;

using Domain.Document;
using Domain.SharedKernel;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

public class AuthenticateDocument(IHttpClientFactory httpClientFactory) : IDocumentUseCaseSelector<AuthenticateDocumentDto>
{
    public async Task<GenericResponse<object>> ExecuteAsync(AuthenticateDocumentDto body)
    {
        var client = httpClientFactory.CreateClient("GovCarpeta");
        var request = new HttpRequestMessage(HttpMethod.Put, "/apis/authenticateDocument");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseData = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        return new GenericResponse<object>() { Data = responseData, Message = responseData, StatusCode = statusCode };
    }
}
