using System;
using System.Net.Http;

namespace FreeProxySharp
{
	/// <summary>
	/// Works with random proxy HttpClient
	/// </summary>
	public class HttpProxyFactory
	{
		#region DI

		private readonly IHttpProxyConfiguration _config;
		private readonly IHttpClientFactory _http;

		public HttpProxyFactory(IHttpProxyConfiguration config, IHttpClientFactory clientFactory)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_http = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
		}

		#endregion

		/// <summary>
		/// Returns random proxy HttpClient by configuration 
		/// </summary>
		public HttpClient GetClientProxy(string name, int? num = null)
		{
			if (_config.Proxies?.Length > 0)
			{
				var _num = num ?? new Random().Next(1, _config.Proxies.Length);

				return _http.CreateClient($"{name}.{_num}");
			}
			else
			{
				return _http.CreateClient(name);
			}
		}
	}
}

