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
var checkedProxies = await FreeProxyListNet.Check(proxies, codeFilter: new[] { "DE", "PL" },
	required: 1, maxMiliseconds: 1200);
```

Or use it all together by **Dependency injection** init procedure (example at: [TestFixture.cs](/src/FreeProxySharp.Test/TestFixture.cs)).

```c#
// parse & check proxies ; save it into configuration
Options.CheckAndAssignToConfig(codeFilter: new[] { "SE", "DE", "ES", "PL" }, required: 2);

// proxy client, with all proxies gets by CheckAndAssignToConfig
services.AddHttpClientProxy("PROXY", Options);
```

And then use **build-in client** [HttpProxyClient.cs](/src/FreeProxySharp/HttpProxyClient.cs) (test: [BasicTest.cs](/src/FreeProxySharp.Test/BasicTest.cs))

```c#
var factory = _test.Services.GetRequiredService<HttpProxyFactory>();
var client = factory.GetClientProxy("PROXY");
var html = await client.GetStringSafeAsync("https://www.amazon.com/");
```

For common work with *IHttpClientFactory* clients, configure it by **Dependency injection** init procedure (example at: [TestFixture.cs](/src/FreeProxySharp.Test/TestFixture.cs)).

```c#
// common client, with all settings from Configuration
services.AddHttpClient("COMMON", Options);
// common client, with retry = 5
services.AddHttpClient("5RETRY", retry: 5);
// common client, with configured for retry when 404 status found (example)
services.AddHttpClient("404TEST", Options, whenRetry: res => res.StatusCode == HttpStatusCode.NotFound);
```

Configuration example [TestOptions.cs](/src/FreeProxySharp.Test/TestOptions.cs):

```c#
public class TestOptions : IHttpProxyConfiguration
{
	public int Retry => 2;
        public int RetryFirstDelay => 1;
        public bool GzipEnabled => true;
        public string UserAgent => HttpExtensions.DEFAULT_AGENT;
	
        public IHttpProxyServer[] Proxies { get; set; }
}
```

### Update notice

For update from version 1.0.x -> 1.1.x:

- ADD: `AddHttpClient` and `AddHttpClientProxy` parameter whenRetry for HttpClient retry settings
- ADD: `CheckAndAssignToConfig` switch: `throwWhenLessThanRequired` for exc. when less than requied proxy found
- ADD: `CheckAndAssignToConfig` parameter: `Timeout` for timeout for checking all proxies
- DEL: `IHttpProxyConfiguration.ProxyEnabled` removed (use `AddHttpClient` of `AddHttpClientProxy` instead manually)
- DEL: `HttpProxyClient` removed ; now use `HttpExtensions` (for common configuration and tasks) & `HttpProxyFactory` (for work with proxy)

