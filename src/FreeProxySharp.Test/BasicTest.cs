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
			Assert.Null(await client.GetStringAsync("https://www.algorim.com/dedadsadsa", retry: 2));
		}

		[Fact]
		public async Task TestHttpClientRetry()
		{
			var client = _test.Services.GetRequiredService<HttpProxyClient>();

			for (var x = 0; x < 100; x++)
			{
				Assert.NotEmpty(await client.GetStringAsync("https://www.cardkingdom.com/mtg/ravnica-allegiance/singles", retry: 5));
			}
		}

		[Fact]
		public async Task TestFreeProxyListNet()
		{
			// parse proxies from list
			var proxies = await FreeProxyListNet.Parse();
			Assert.NotEmpty(proxies);
			Assert.Contains(proxies, x => !string.IsNullOrEmpty(x.Ip));
			Assert.Contains(proxies, x => x.Port > 0);
			Assert.Contains(proxies, x => !string.IsNullOrEmpty(x.Code));
			Assert.Contains(proxies, x => !string.IsNullOrEmpty(x.Country));
			Assert.Contains(proxies, x => x.Type == FreeProxyTypes.Elite);
			Assert.Contains(proxies, x => x.IsHttps);

			// check all proxies
			var checkedProxies = await FreeProxyListNet.Check(proxies, requied: 1, maxMiliseconds: 1200);
			Assert.NotEmpty(checkedProxies);
			Assert.All(checkedProxies, x => Assert.True(x.ElapsedMiliseconds > 0));
		}

		[Fact]
		public void TestConfigAssign()
		{
			// all together
			var proxies = _test.Options.AssignToConfig(codeFilter: new[] { "SE", "DE", "ES", "GB", "RU" }, requied: 2);
			Assert.NotEmpty(proxies);
		}
	}
}
