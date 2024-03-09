using System.Text.Json.Serialization;

namespace ErgoProxy.Domain.User;

public class UnRegisterUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("operatorId")]
    public string OperatorId { get; set; }
    [JsonPropertyName("operatorName")]
    public string OperatorName { get; set; }
}
