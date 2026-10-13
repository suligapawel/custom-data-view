namespace CustomDataView.Api.Requests;

public sealed record ConnectionDraftTestRequest(ConnectionRequest Connection, ConnectionParametersRequest Parameters)
{
    public IDictionary<string, string[]> Validate()
    {
        if (Connection is null)
        {
            return new Dictionary<string, string[]> { ["connection"] = ["Definicja połączenia jest wymagana."] };
        }

        return Connection.Validate().ToDictionary(error => $"connection.{error.Key}", error => error.Value);
    }
}
