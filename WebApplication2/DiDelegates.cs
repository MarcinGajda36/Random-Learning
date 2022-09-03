namespace WebApplication2;

public delegate TClient ClientFactory<out TClient>(HttpClient httpClient);

public class Client<TClient>
{
    private readonly ClientFactory<TClient> clientFactory;
    private readonly IHttpClientFactory httpClientFactory;

    public Client(ClientFactory<TClient> clientFactory, IHttpClientFactory httpClientFactory)
    {
        this.clientFactory = clientFactory;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<TResult> UsingClient<TResult>(Func<TClient, Task<TResult>> use)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var client = clientFactory(httpClient);
        return await use(client);
    }
}
