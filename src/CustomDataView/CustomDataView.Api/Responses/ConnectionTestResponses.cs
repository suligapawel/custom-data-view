using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CustomDataView.Core.Connections.Http;
using Microsoft.AspNetCore.Http;

namespace CustomDataView.Api.Responses;

public sealed record ConnectionTestSuccessResponse(int Status, long ElapsedMs, JsonNode Body);

public sealed record ConnectionTestFailureResponse(
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Status);

public static class ConnectionTestResponses
{
    public static IResult From(ExternalApiResult result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new ConnectionTestSuccessResponse(result.Status!.Value, result.ElapsedMs, result.Body));
        }

        return Results.Json(
            new ConnectionTestFailureResponse(result.ErrorCode, result.Status),
            statusCode: StatusCodes.Status502BadGateway);
    }
}
