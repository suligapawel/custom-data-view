namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class ConnectionRow
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Method { get; set; }

    public string Url { get; set; }

    public string Body { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ConnectionPathParameterRow> PathParameters { get; set; } = [];

    public List<ConnectionQueryParameterRow> QueryParameters { get; set; } = [];

    public List<ConnectionBodyParameterRow> BodyParameters { get; set; } = [];
}
