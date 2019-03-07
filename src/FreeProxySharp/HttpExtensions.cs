using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog;

namespace FreeProxySharp
{
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
        /// exponential waiting
        /// </summary>
        internal static TimeSpan GetDelay(int firstRetryDelay, int retryAttempt)
		{
			var jitterer = new Random();
			var waitFor = firstRetryDelay + (int)Math.Pow(2, retryAttempt);
			var spanWaitFor = TimeSpan.FromSeconds(waitFor) + TimeSpan.FromMilliseconds(jitterer.Next(0, (waitFor * 100)));
			return spanWaitFor;
		}

        /// <summary>
        /// HttpClient DI settings by name and configuration
        /// </summary>
		public static void AddHttpClient(this IServiceCollection services, IHttpProxyConfiguration configuration, string name, WebProxy proxy = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            AddHttpClient(services, name, configuration.Retry, configuration.RetryFirstDelay, configuration.GzipEnabled, configuration.UserAgent, proxy);
        }

        /// <summary>
        /// HttpClient DI settings by name
        /// </summary>
        public static void AddHttpClient(this IServiceCollection services,
            string name, int retry = DEFAULT_RETRY, int retryFirstDelay = DEFAULT_RETRY_FIRST_DELAY,
            bool gzipEnabled = DEFAULT_GZIP, string userAgent = DEFAULT_AGENT, WebProxy proxy = null)
        {

			services.AddHttpClient(name,
				// user-agent
				client => client.DefaultRequestHeaders.Add("User-Agent", userAgent))
				// proxy
				// https://stackoverflow.com/questions/29856543/httpclient-and-using-proxy-constantly-getting-407
				.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
				{
					Proxy = proxy
				})
				// GZIP
				.ConfigureHttpMessageHandlerBuilder(config => new HttpClientHandler
				{
					AutomaticDecompression = gzipEnabled ? DecompressionMethods.GZip : DecompressionMethods.None,
					UseProxy = (proxy != null)
				})
				.AddTransientHttpErrorPolicy(builder => builder
					// check Result status code; OK -> continue
					.OrResult(res => res?.StatusCode != HttpStatusCode.OK)
					// exponential waiting; number of retry by parameters
					.WaitAndRetryAsync(retry,
						retryAttempt => GetDelay(retryFirstDelay, retryAttempt),
						onRetry: (outcome, timespan, retryAttempt, context) =>
						{
							Log.Warning($"Retry [client] delay: {timespan.TotalSeconds}s #{retryAttempt}");
						}));
		}
	}
}
