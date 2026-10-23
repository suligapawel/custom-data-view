namespace CustomDataView.WebApi.Requests;

public sealed record ConnectionDraftTestRequest(ConnectionRequest Connection, ConnectionParametersRequest Parameters)
{
    public IDictionary<string, string[]> Validate()
        => Connection is null
            ? new Dictionary<string, string[]> { ["connection"] = ["Definicja połączenia jest wymagana."] }
            : Connection.Validate().ToDictionary(error => $"connection.{error.Key}", error => error.Value);
}
