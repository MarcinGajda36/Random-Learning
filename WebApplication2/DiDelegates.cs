namespace WebApplication2;


public delegate TClient ClientFactory<out TClient>(string url, HttpClient httpClient);

public class Client<TClient>
{
    private readonly ClientFactory<TClient> clientFactory;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;

    public Client(
        ClientFactory<TClient> clientFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        this.clientFactory = clientFactory;
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
    }

    public async Task<TResult> UsingClient<TResult>(Func<TClient, Task<TResult>> use)
    {
        string url = configuration.GetRequiredSection("url secton").Get<string>();
        using var httpClient = httpClientFactory.CreateClient();
        var client = clientFactory(url, httpClient);
        return await use(client);
    }
}
