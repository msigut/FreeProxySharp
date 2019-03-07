using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FreeProxySharp
{
	/// <summary>
	/// Http proxy client
	/// </summary>
	public class HttpProxyClient
	{
        internal const string NAME = "proxy.client";

        #region DI

        private readonly ILogger _logger;
		private readonly IHttpClientFactory _http;
        private readonly IHttpProxyConfiguration _configuration;

		public HttpProxyClient(ILogger logger, IHttpClientFactory http, IHttpProxyConfiguration configuration)
		{
			_logger = logger;
			_http = http;
            _configuration = configuration;
		}

		#endregion

		/// <summary>
		/// load cilove URL
		/// </summary>
		public async Task<string> GetStringAsync(string url, int? encodingPage = null, int? retry = null, int? clientNum = null)
		{
			var retryAttempt = 0;
			var retryMax = retry ?? _configuration.Retry;

			// klient, vcetne nahodne vybraneho proxy nastaveni
			var client = GetClient(clientNum);

			// cekani na dalsi zpracovani
			async Task Delay(string label)
			{
				retryAttempt++;
				var delay = HttpExtensions.GetDelay(_configuration.RetryFirstDelay, retryAttempt);
				Log.Warning($"Retry [{label}] delay: {delay.TotalSeconds}s #{retryAttempt} url: '{url}'");
				await Task.Delay(delay);
			}

			again:

			try
			{
				using (var response = await client.GetAsync(url))
				{
					response.EnsureSuccessStatusCode();

					string result = null;
					if (encodingPage == null)
					{
						// bez kodovani
						result = await response.Content.ReadAsStringAsync();
					}
					else
					{
						// specialni kodovani obsahu stranky
						var enc = CodePagesEncodingProvider.Instance.GetEncoding((int)encodingPage);
						using (var stream = await response.Content.ReadAsStreamAsync())
						{
							using (var read = new StreamReader(stream, enc))
							{
								result = read.ReadToEnd();
							}
						}
					}

					if (!string.IsNullOrEmpty(result))
					{
						return result;
					}
					else
					{
						if (retryAttempt > retryMax)
						{
							Log.Error($"Empty result in url: '{url}'");
							return null;
						}
						else
						{
							await Delay("empty");
							goto again;
						}
					}
				}
			}
			catch (TaskCanceledException)
			{
				Log.Warning($"Retry failed, url: '{url}'");
				return null;
			}
			catch (HttpRequestException ex)
			{
				Log.Error(ex, $"RequestException in url: '{url}'");
				return null;
			}
			catch (Exception ex)
			{
				if (retryAttempt > retryMax)
				{
					Log.Error(ex, $"Exception in url: '{url}'");
					return null;
				}
				else
				{
					await Delay("exception");
					goto again;
				}
			}
		}

		/// <summary>
		/// nahodne vybere jednoho z klientu
		/// </summary>
		private HttpClient GetClient(int? num = null)
		{
			if (_configuration.ProxyEnabled && _configuration.Proxies?.Length > 0)
			{
				var _num = num ?? new Random().Next(0, _configuration.Proxies.Length - 1);

				return _http.CreateClient($"{NAME}.{_num}");
			}
			else
			{
				return _http.CreateClient(NAME);
			}
		}
	}

	/// <summary>
	/// Http proxy client extensions
	/// </summary>
	public static class HttpProxyClientExtensions
	{
		/// <summary>
		/// konfigurace IHttpClientFactory
		/// </summary>
		public static void AddHttpProxyClient(this IServiceCollection services, IHttpProxyConfiguration configuration)
		{
			// default klient
			AddClientProxy();

			// jen jestlize jsou proxy povolene
			if (configuration.ProxyEnabled)
			{
				if (configuration.Proxies == null || !configuration.Proxies.Any())
					throw new ArgumentNullException("Proxy.Proxies");

				var x = 0;
				foreach (var p in configuration.Proxies)
				{
					AddClientProxy(p, x++);
				}
			}

			void AddClientProxy(IHttpProxyServer p = null, int? num = null)
			{
				if (p != null && num == null)
					throw new ArgumentNullException($"{nameof(p)} {nameof(num)}");

				var name = HttpProxyClient.NAME;
				WebProxy webProxy = null;

				// jestlize 
				if (p != null)
				{
					name = $"{HttpProxyClient.NAME}.{num}";
					webProxy = new WebProxy(p.Ip, p.Port);

					Log.Information($"HttpClient #{num} proxy {p.Ip}:{p.Port} {p.Note}");
				}
				else
				{
					Log.Information($"HttpClient #default");
				}

				services.AddHttpClient(configuration, name, webProxy);
			}
		}
	}
}
