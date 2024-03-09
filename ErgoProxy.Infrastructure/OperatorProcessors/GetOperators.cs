namespace ErgoProxy.Infrastructure.OperatorProcessors;

using Domain.Operator;
using Domain.SharedKernel;
using System.Text.Json;
using System.Text;
using Domain.SharedKernel.Responses;

public class GetOperators : IOperatorUseCaseSelector<GetOperatorsDto>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GetOperators(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GenericResponse<object>> ExecuteAsync(GetOperatorsDto body)
    {
        var client = _httpClientFactory.CreateClient("GovCarpeta");
        var request = new HttpRequestMessage(HttpMethod.Get, "/apis/getOperators");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var operators = JsonSerializer.Deserialize<GovCarpetaGetOperatorsResponse[]>(responseContent);
        var statusCode = (int)response.StatusCode;
        return new GenericResponse<object>() { Data = operators, Message = "Operators obtained successfully", StatusCode = statusCode };
    }
}
