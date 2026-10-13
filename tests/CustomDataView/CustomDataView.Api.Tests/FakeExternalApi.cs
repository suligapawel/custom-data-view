using System.Net;
using System.Text;

namespace CustomDataView.Api.Tests;

public sealed class FakeExternalApi : HttpMessageHandler
{
    private readonly List<ReceivedRequest> requests = [];

    public IReadOnlyList<ReceivedRequest> Requests => requests;

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond { get; set; } =
        (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

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

    public sealed record ReceivedRequest(string Method, string Url, string ContentType, string Body);
}
