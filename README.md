## FreeProxySharp

HttpClient *(IHttpClientFactory)* + Proxy implementation with Dependency injection support and https://free-proxy-list.net/ as proxy-list source for .NET Standard 2.0 (netstandard2.0).

Library **Checks**:
- transparency (with reverse-IP check at: http://www.whatismyip.cz/)
- HTTPS (with access to: http://www.google.com/)
- measure **elapsed** time in miliseconds

For proxy **Parse** & **Check** use (test: [BasicTest.cs](/src/FreeProxySharp.Test/BasicTest.cs)):

```c#
// get proxy list
var proxies = await FreeProxyListNet.Parse();
// check all proxies
var checkedProxies = await FreeProxyListNet.Check(proxies,
	codeFilter: new[] { "DE", "PL" }, required: 1, maxMiliseconds: 1200);
```

Or use it all together by **Dependency injection** init procedure (example at: [TestFixture.cs](/src/FreeProxySharp.Test/TestFixture.cs)).

```c#
Options.AssignToConfig(codeFilter: new[] { "SE", "DE", "ES", "PL" }, required: 2);
services.AddHttpProxyClient(Options);
```

And then use **build-in client** [HttpProxyClient.cs](/src/FreeProxySharp/HttpProxyClient.cs) (test: [BasicTest.cs](/src/FreeProxySharp.Test/BasicTest.cs))

```c#
var client = _test.Services.GetRequiredService<HttpProxyClient>();
await client.GetStringAsync("https://www.amazon.com/", retry: 2);
```

Configuration example [TestOptions.cs](/src/FreeProxySharp.Test/TestOptions.cs):

```c#
public class TestOptions : IHttpProxyConfiguration
{
	public int Retry => 2;
        public int RetryFirstDelay => 1;
        public bool GzipEnabled => true;
        public string UserAgent => HttpExtensions.DEFAULT_AGENT;
	
        public bool ProxyEnabled => true;
        public IHttpProxyServer[] Proxies { get; set; }
}
```
