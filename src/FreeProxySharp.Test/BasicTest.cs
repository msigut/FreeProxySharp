using System.Threading.Tasks;
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
		public async Task TestHttpClient()
		{
			var client = _test.Services.GetRequiredService<HttpProxyClient>();

			Assert.NotEmpty(await client.GetStringAsync("https://www.amazon.com/", retry: 2));
		}

		[Fact]
		public async Task TestHttpClientNotFound()
		{
			var client = _test.Services.GetRequiredService<HttpProxyClient>();

			// 404 Not found page
			Assert.Null(await client.GetStringAsync("https://www.seznam.cz/sadaasada", retry: 2));
		}

		[Fact]
		public async Task TestHttpClientRetry()
		{
			var client = _test.Services.GetRequiredService<HttpProxyClient>();

			for (var x = 0; x < 10; x++)
			{
				Assert.NotEmpty(await client.GetStringAsync("https://www.google.com/", retry: 5));
			}
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
			var checkedProxies = await FreeProxyListNet.Check(proxies, codeFilter: new[] { "SE", "DE", "ES", "PL", "FR", "NL", "CZ", "US", "RU" }, required: 1, maxMiliseconds: 1200);
			Assert.NotEmpty(checkedProxies);
			Assert.All(checkedProxies, x => Assert.True(x.ElapsedMiliseconds > 0));
		}

		[Fact]
		public void TestFreeProxyConfigAssign()
		{
			// all together
			_test.Options.AssignToConfig(codeFilter: new[] { "SE", "DE", "ES", "GB", "RU" }, required: 2);
			Assert.NotEmpty(_test.Options.Proxies);
		}
	}
}
