using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Serilog;

namespace FreeProxySharp
{
	/// <summary>
	/// https://free-proxy-list.net/ scanner
	/// </summary>
	public static class FreeProxyListNet
    {
		public const string URL = "https://free-proxy-list.net/";

		/// <summary>
		/// parse list of all proxies
		/// </summary>
		public static async Task<IEnumerable<FreeProxyServer>> Parse()
        {
			// parse proxy type
			FreeProxyTypes ParseType(string str)
			{
				switch (str.ToLowerInvariant())
				{
					case "anonymous":
						return FreeProxyTypes.Anonymous;
					case "elite proxy":
						return FreeProxyTypes.Elite;
					default:
						return FreeProxyTypes.Transparent;
				}
			}

			using (var client = new HttpClient())
			{
				var html = await client.GetStringAsync(URL);

				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				var rows = doc.DocumentNode.QuerySelectorAll("table tbody tr")
					.Select(p => new FreeProxyServer()
					{
						Ip = p.QuerySelector("td:nth-child(1)").InnerHtml,
						Port = int.Parse(p.QuerySelector("td:nth-child(2)").InnerHtml),
						Code = p.QuerySelector("td:nth-child(3)").InnerHtml,
						Country = p.QuerySelector("td:nth-child(4)").InnerHtml,
						Type = ParseType(p.QuerySelector("td:nth-child(5)").InnerHtml),
						IsHttps = p.QuerySelector("td:nth-child(7)").InnerHtml != "no",
					});

				return rows.ToArray();
			}
		}

		/// <summary>
		/// check proxy list
		/// </summary>
		public static async Task<IEnumerable<FreeProxyServer>> Check(IEnumerable<FreeProxyServer> list,
			bool nonTransparentOnly = true, string[] codeFilter = null, int required = 10, int maxMiliseconds = 1000, bool? https = true)
        {
			var result = new List<FreeProxyServer>();

			// for non-trasparent forget this kind in input data
			if (nonTransparentOnly)
			{
				list = list.Where(x => x.Type != FreeProxyTypes.Transparent);
			}
			// need support HTTPS?
			if (https != null)
			{
				list = list.Where(x => x.IsHttps == https);
			}
			// filter by Country codes; when defined
			if (codeFilter != null && codeFilter.Length > 0)
			{
				var _filter = codeFilter.Select(x => x.ToUpperInvariant()).ToArray();
				list = list.Where(x => _filter.Contains(x.Code.ToUpperInvariant()));
			}

			Log.Debug($"Check: {list.Count()} proxies, {required} required.");

			var num = 0;
			foreach(var p in list)
			{
				var label = $"#{++num} {p.Ip}:{p.Port} {p.Note}";

				// create client with proxy
				var handler = new HttpClientHandler()
				{
					Proxy = new WebProxy(p.Ip, p.Port),
					UseProxy = true,
				};
				var client = new HttpClient(handler);

				// check myself IP
				var html = "";
				try
				{
					var watch = Stopwatch.StartNew();

					html = await client.GetStringAsync("http://www.whatismyip.cz/");
					if (string.IsNullOrEmpty(html))
					{
						Log.Debug($"{label} [empty #1 - whatismyip]");
						continue;
					}

					// save elapsed miliseconds
					watch.Stop();
					p.ElapsedMiliseconds = watch.ElapsedMilliseconds;

					if (string.IsNullOrEmpty(await client.GetStringAsync("https://www.google.com/")))
					{
						Log.Debug($"{label} [empty #2 - google]");
						continue;
					}
				}
				catch (HttpRequestException)
				{
					Log.Debug($"{label} [exception]");
					continue;
				}

				// check if proxy is not transparent (visible IP is the same as proxy IP)
				if (nonTransparentOnly)
				{
					var doc = new HtmlDocument();
					doc.LoadHtml(html);
					var ipValue = doc.DocumentNode.QuerySelector("div.ip")?.InnerText;

					// check myself IP with proxy settings
					if (ipValue != p.Ip)
					{
						Log.Debug($"{label} [ip]");
						continue;
					}
				}

				// check latency
				if (maxMiliseconds > 0 && p.ElapsedMiliseconds > maxMiliseconds)
				{
					Log.Debug($"{label} [slow in {p.ElapsedMiliseconds}ms]");
					continue;
				}

				result.Add(p);
				Log.Debug($"{label} [OK in {p.ElapsedMiliseconds}ms]");

				// check if already has count of requied
				if (required > 0 && result.Count >= required)
					break;
			}

			return result.OrderBy(x => x.ElapsedMiliseconds);
		}

		/// <summary>
		/// parse & check & assign to configuratuin proxies
		/// </summary>
		public static void AssignToConfig(this IHttpProxyConfiguration configuration, string[] codeFilter = null, int required = 10)
		{
			var proxies = Parse().GetAwaiter().GetResult();
			var checkedProxies = Check(proxies, codeFilter: codeFilter, required: required).GetAwaiter().GetResult();

			configuration.Proxies = checkedProxies.ToArray();
		}
	}
}
