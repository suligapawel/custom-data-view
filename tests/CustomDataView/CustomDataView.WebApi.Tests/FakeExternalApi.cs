using System.Net;
using System.Text;

namespace CustomDataView.WebApi.Tests;

public sealed class FakeExternalApi : HttpMessageHandler
{
    private readonly List<ReceivedRequest> requests = [];

    public IReadOnlyList<ReceivedRequest> Requests => requests;

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond { get; set; } = RespondWithEmptyObject;

    public void Reset()
    {
        lock (requests)
        {
            requests.Clear();
        }

        Respond = RespondWithEmptyObject;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (requests)
        {
            requests.Add(new ReceivedRequest(
                request.Method.Method,
                request.RequestUri!.AbsoluteUri,
                request.Content?.Headers.ContentType?.MediaType,
                body));
        }

        return await Respond(request, cancellationToken);
    }

    private static Task<HttpResponseMessage> RespondWithEmptyObject(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

    public sealed record ReceivedRequest(string Method, string Url, string ContentType, string Body);
}
