using System.Text.Json.Serialization;

namespace ErgoProxy.Domain.SharedKernel.Responses;

public class GovCarpetaGetOperatorsResponse
{
    [JsonPropertyName("_id")]
    public string OperatorId { get; set; }
    [JsonPropertyName("operatorName")]
    public string OperatorName { get; set; }
    [JsonPropertyName("transferAPIURL")]
    public string TransferApiUrl { get; set; }
}
