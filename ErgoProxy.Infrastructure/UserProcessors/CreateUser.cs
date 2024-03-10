namespace ErgoProxy.Infrastructure.UserProcessors;

using Domain.SharedKernel;
using Domain.User;
using System.Text;
using System.Text.Json;

public class CreateUser(IHttpClientFactory httpClientFactory) : IUserUseCaseSelector<CreateUserDto>
{
    public async Task<GenericResponse<object>> ExecuteAsync(CreateUserDto body)
    {
        var client = httpClientFactory.CreateClient("GovCarpeta");
        var request = new HttpRequestMessage(HttpMethod.Post, "/apis/registerCitizen");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseData = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        return new GenericResponse<object>() { Data = responseData ,Message = responseData, StatusCode = statusCode };
    }
}
