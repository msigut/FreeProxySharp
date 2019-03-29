using FreeProxySharp;
using System.IO;

namespace FreeProxySharp.Test
{
	/// <summary>
	/// UNIT test configuration
	/// </summary>
	public class TestOptions : IHttpProxyConfiguration
	{
        public int Retry => 2;
        public int RetryFirstDelay => 1;
        public bool GzipEnabled => true;
        public string UserAgent => HttpExtensions.DEFAULT_AGENT;

        public IHttpProxyServer[] Proxies { get; set; }
    }
}
