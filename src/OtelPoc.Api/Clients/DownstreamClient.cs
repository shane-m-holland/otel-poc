namespace OtelPoc.Api.Clients;

public class DownstreamClient(HttpClient httpClient)
{
    public async Task<string> SendAsync(string correlationId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/receive");
        request.Headers.Add("X-Correlation-Id", correlationId);
        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
