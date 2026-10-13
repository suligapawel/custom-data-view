using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Http;

public sealed record ExternalApiResult(bool IsSuccess, int? Status, long ElapsedMs, JsonNode Body, string ErrorCode)
{
    public static ExternalApiResult Success(int status, long elapsedMs, JsonNode body)
        => new ExternalApiResult(true, status, elapsedMs, body, null);

    public static ExternalApiResult Failure(string errorCode, int? status = null)
        => new ExternalApiResult(false, status, 0, null, errorCode);
}
