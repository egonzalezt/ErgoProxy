namespace ErgoProxy.Domain.Document;

using System.Text.Json.Serialization;

public class AuthenticateDocumentDto
{
    [JsonPropertyName("idCitizen")]
    public long IdCitizen { get; set; }
    [JsonPropertyName("UrlDocument")]
    public string UrlDocument { get; set; }
    [JsonPropertyName("documentTitle")]
    public string DocumentTitle { get; set; }
}
