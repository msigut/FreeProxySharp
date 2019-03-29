using System.Net.Http;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FreeProxySharp.Test
{
	public class BasicTest : IClassFixture<TestFixture>
	{
		#region DI

		private readonly TestFixture _test;

		public BasicTest(TestFixture test)
		{
			_test = test;
		}

		#endregion

		[Fact]
		public async Task TestHttpClientCommon()
		{
			var factory = _test.Services.GetRequiredService<IHttpClientFactory>();
			var client = factory.CreateClient("COMMON");

			Assert.NotEmpty(await client.GetStringSafeAsync("https://httpstat.us"));
		}

		[Fact]
		public async Task TestHttpClientNotFound()
		{
			var factory = _test.Services.GetRequiredService<IHttpClientFactory>();
			var client = factory.CreateClient("404TEST");

			await Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetStringSafeAsync("https://httpstat.us/404"));
		}

		[Fact]
		public async Task TestHttpClientProxy()
		{
			var factory = _test.Services.GetRequiredService<HttpProxyFactory>();
			var client = factory.GetClientProxy("PROXY");

			var html = await client.GetStringSafeAsync("http://www.whatismyip.cz/");
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var ipValue = doc.DocumentNode.QuerySelector("div.ip")?.InnerText;

			Assert.Contains(_test.Options.Proxies, x => x.Ip == ipValue);
		}

		[Fact]
		public async Task TestFreeProxyListNet()
		{
			// get proxy list
			var proxies = await FreeProxyListNet.Parse();
			Assert.NotEmpty(proxies);
			Assert.Contains(proxies, x => !string.IsNullOrEmpty(x.Ip));
			Assert.Contains(proxies, x => x.Port > 0);
			Assert.Contains(proxies, x => !string.IsNullOrEmpty(x.Code));
			Assert.Contains(proxies, x => !string.IsNullOrEmpty(x.Country));
			Assert.Contains(proxies, x => x.Type == FreeProxyTypes.Elite);
			Assert.Contains(proxies, x => x.IsHttps);

			// check all proxies
			var checkedProxies = await FreeProxyListNet.Check(proxies, codeFilter: new[] { "SE", "DE", "ES", "PL", "FR", "NL", "CZ", "US", "RU" }, required: 1, maxMiliseconds: 1200, timeoutSeconds: 3);
			Assert.NotEmpty(checkedProxies);
			Assert.All(checkedProxies, x => Assert.True(x.ElapsedMiliseconds > 0));
		}

		[Fact]
		public void TestFreeProxyConfigAssign()
		{
			// check config for proxy list
			Assert.NotEmpty(_test.Options.Proxies);
		}
	}
}
