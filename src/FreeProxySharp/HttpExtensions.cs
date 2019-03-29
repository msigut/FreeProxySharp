using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog;

namespace FreeProxySharp
{
	/// <summary>
	/// Extensions for configure common & proxy HttpClients
	/// </summary>
	public static class HttpExtensions
    {
        /// <summary>
        /// number of retry
        /// </summary>
        public const int DEFAULT_RETRY = 3;
        /// <summary>
        /// first retry delay in seconds
        /// </summary>
        public const int DEFAULT_RETRY_FIRST_DELAY = 5;
        /// <summary>
        /// gzip enabled?
        /// </summary>
        public const bool DEFAULT_GZIP = true;
        /// <summary>
        /// default Agent name
        /// </summary>
        public const string DEFAULT_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36";

		/// <summary>
		/// HttpClient DI settings by name
		/// </summary>
		public static void AddHttpClient(this IServiceCollection services, string name, IHttpProxyConfiguration config, Func<HttpResponseMessage, bool> whenRetry = null)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			services.AddHttpClient(name, retry: config.Retry, retryFirstDelay: config.RetryFirstDelay, gzipEnabled: config.GzipEnabled, userAgent: config.UserAgent, whenRetry: whenRetry);
		}
		public static void AddHttpClient(this IServiceCollection services, string name,
			int retry = DEFAULT_RETRY, int retryFirstDelay = DEFAULT_RETRY_FIRST_DELAY, bool gzipEnabled = DEFAULT_GZIP, string userAgent = DEFAULT_AGENT,
			Func<HttpResponseMessage, bool> whenRetry = null)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException(nameof(name));

			// check Result status code; OK -> continue
			if (whenRetry == null)
			{
				whenRetry = res => res?.StatusCode != HttpStatusCode.OK;
			}

			services.AddHttpClient(name,
				// user-agent
				client => client.DefaultRequestHeaders.Add("User-Agent", userAgent))
				// GZIP
				.ConfigureHttpMessageHandlerBuilder(config => new HttpClientHandler
				{
					AutomaticDecompression = gzipEnabled ? DecompressionMethods.GZip : DecompressionMethods.None,
				})
				.AddTransientHttpErrorPolicy(builder => builder
					.OrResult(whenRetry)
					// exponential waiting; number of retry by parameters
					.WaitAndRetryAsync(retry,
						retryAttempt => GetDelay(retryFirstDelay, retryAttempt),
						onRetry: (outcome, timespan, retryAttempt, context) =>
						{
							Log.Warning($"Retry [client] delay: {timespan.TotalSeconds}s #{retryAttempt} url: '{outcome.Result?.RequestMessage?.RequestUri?.OriginalString}'");
						}));
		}

		/// <summary>
		/// HttpClient DI settings by name with proxy
		/// </summary>
		public static void AddHttpClientProxy(this IServiceCollection services, string name, IHttpProxyConfiguration config, Func<HttpResponseMessage, bool> whenRetry = null)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			services.AddHttpClientProxy(name, config.Proxies, retry: config.Retry, retryFirstDelay: config.RetryFirstDelay, gzipEnabled: config.GzipEnabled, userAgent: config.UserAgent, whenRetry: whenRetry);
		}
		public static void AddHttpClientProxy(this IServiceCollection services, string name, IHttpProxyServer[] proxies,
			int retry = DEFAULT_RETRY, int retryFirstDelay = DEFAULT_RETRY_FIRST_DELAY, bool gzipEnabled = DEFAULT_GZIP,
			string userAgent = DEFAULT_AGENT, Func<HttpResponseMessage, bool> whenRetry = null)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException(nameof(name));
			if (proxies == null || proxies.Length == 0)
				throw new ArgumentNullException(nameof(proxies));

			// check Result status code; OK -> continue
			if (whenRetry == null)
			{
				whenRetry = res => res?.StatusCode != HttpStatusCode.OK;
			}

			var x = 1;
			foreach (var p in proxies)
			{
				var proxy = new WebProxy(p.Ip, p.Port);
				var proxyName = $"{name}.{x}";

				services.AddHttpClient(proxyName,
					// user-agent
					client =>
					{
						client.DefaultRequestHeaders.Add("User-Agent", userAgent);
					})
					// proxy
					// https://stackoverflow.com/questions/29856543/httpclient-and-using-proxy-constantly-getting-407
					.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
					{
						Proxy = proxy,
					})
					// GZIP
					.ConfigureHttpMessageHandlerBuilder(config => new HttpClientHandler
					{
						AutomaticDecompression = gzipEnabled ? DecompressionMethods.GZip : DecompressionMethods.None,
						UseProxy = true,
					})
					.AddTransientHttpErrorPolicy(builder => builder
						.OrResult(whenRetry)
						// exponential waiting; number of retry by parameters
						.WaitAndRetryAsync(retry,
							retryAttempt => GetDelay(retryFirstDelay, retryAttempt),
							onRetry: (outcome, timespan, retryAttempt, context) =>
							{
								Log.Warning($"Retry [client] delay: {timespan.TotalSeconds}s #{retryAttempt} url: '{outcome.Result?.RequestMessage?.RequestUri?.OriginalString}'");
							}));

				Log.Information($"HttpClient {proxyName} proxy {p.Ip}:{p.Port} {p.Note}");
				x++;
			}
		}

		/// <summary>
		/// safe GetString for HttpClient
		/// </summary>
		public static async Task<string> GetStringSafeAsync(this HttpClient client, string url, int? encodingPage = null)
		{
			try
			{
				using (var response = await client.GetAsync(url))
				{
					response.EnsureSuccessStatusCode();

					if (encodingPage == null)
					{
						// bez kodovani
						return await response.Content.ReadAsStringAsync();
					}
					else
					{
						// specialni kodovani obsahu stranky
						var enc = CodePagesEncodingProvider.Instance.GetEncoding((int)encodingPage);
						using (var stream = await response.Content.ReadAsStreamAsync())
						{
							using (var read = new StreamReader(stream, enc))
							{
								return await read.ReadToEndAsync();
							}
						}
					}
				}
			}
			catch (TaskCanceledException)
			{
				Log.Warning($"Retry failed, url: '{url}'");
				return null;
			}
		}

		#region Helpers

		/// <summary>
		/// exponential waiting
		/// </summary>
		internal static TimeSpan GetDelay(int firstRetryDelay, int retryAttempt)
		{
			var jitterer = new Random();
			var waitFor = firstRetryDelay + (int)Math.Pow(2, retryAttempt);
			var spanWaitFor = TimeSpan.FromSeconds(waitFor) + TimeSpan.FromMilliseconds(jitterer.Next(0, (waitFor * 100)));
			return spanWaitFor;
		}

		#endregion
	}
}
