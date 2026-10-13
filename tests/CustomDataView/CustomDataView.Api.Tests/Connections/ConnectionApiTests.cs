using System.Net.Http.Json;

namespace CustomDataView.Api.Tests.Connections;

public abstract class ConnectionApiTests
{
    protected const string ConnectionsUrl = "/api/custom-data-view/connections";

    protected CustomDataViewApplicationFactory Factory { get; private set; }
    protected HttpClient Client { get; private set; }

    [SetUp]
    public void SetUpApplication()
    {
        Factory = new CustomDataViewApplicationFactory();
        Client = Factory.CreateClient();
    }

    [TearDown]
    public void TearDownApplication()
    {
        Client.Dispose();
        Factory.Dispose();
    }

    protected async Task<Guid> CreateConnection(object definition)
    {
        var response = await Client.PostAsJsonAsync(ConnectionsUrl, definition);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedConnection>();
        return created.Id;
    }

    protected sealed record CreatedConnection(Guid Id);
}
