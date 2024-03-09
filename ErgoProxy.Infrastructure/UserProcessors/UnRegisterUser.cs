namespace ErgoProxy.Infrastructure.UserProcessors;

using Domain.SharedKernel;
using Domain.User;
using System.Text;
using System.Text.Json;

public class UnRegisterUser : IUserUseCaseSelector<UnRegisterUserDto>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UnRegisterUser(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GenericResponse> ExecuteAsync(UnRegisterUserDto body)
    {
        var client = _httpClientFactory.CreateClient("GovCarpeta");
        var request = new HttpRequestMessage(HttpMethod.Delete, "/apis/unregisterCitizen");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseData = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        return new GenericResponse() { Message = responseData, StatusCode = statusCode };
    }
}
