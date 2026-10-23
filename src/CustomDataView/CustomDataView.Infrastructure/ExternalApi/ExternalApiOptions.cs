namespace CustomDataView.Infrastructure.ExternalApi;

public sealed class ExternalApiOptions
{
    public const string SectionName = "CustomDataView:ExternalApi";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    public long MaxResponseBytes { get; set; } = 1_048_576;
}
