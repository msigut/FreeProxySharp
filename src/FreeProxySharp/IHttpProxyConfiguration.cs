namespace FreeProxySharp
{
	/// <summary>
	/// proxy
	/// </summary>
	public interface IHttpProxyConfiguration
	{
        int RetryFirstDelay { get; }
        int Retry { get; }
        string UserAgent { get; }
        bool GzipEnabled { get; }
        bool ProxyEnabled { get; }
		IHttpProxyServer[] Proxies { get; set; }
    }

    /// <summary>
    /// proxy server
    /// </summary>
    public interface IHttpProxyServer
    {
        string Ip { get; }
        int Port { get; }
        string Note { get; }
    }
}
