namespace FreeProxySharp
{
	/// <summary>
	/// proxy types by: https://free-proxy-list.net/
	/// </summary>
	public enum FreeProxyTypes
	{
		Unknown,
		Anonymous,
		Elite,
		Transparent
	}

    /// <summary>
    /// proxy server
    /// </summary>
    public class FreeProxyServer : IHttpProxyServer
    {
		public string Ip { get; set; }
		public int Port { get; set; }
		public string Code { get; set; }
		public string Country { get; set; }
		public FreeProxyTypes Type { get; set; }
		public bool IsHttps { get; set; }
		public long ElapsedMiliseconds { get; set; }

		public string Note => $"({Code}, {Country}: {Type})";
    }
}
