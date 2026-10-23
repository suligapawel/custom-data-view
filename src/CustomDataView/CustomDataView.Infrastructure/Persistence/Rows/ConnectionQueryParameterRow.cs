namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class ConnectionQueryParameterRow
{
    public Guid ConnectionId { get; set; }

    public string Name { get; set; }

    public bool Required { get; set; }

    public string DefaultValue { get; set; }
}
