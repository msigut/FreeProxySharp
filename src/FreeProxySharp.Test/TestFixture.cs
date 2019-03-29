using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FreeProxySharp.Test
{
    public class TestFixture : IDisposable
	{
		/// <summary>
		/// UNIT test configuration
		/// </summary>
		public TestOptions Options;

		/// <summary>
		/// DI
		/// </summary>
		public IServiceProvider Services { get; private set; }

		/// <summary>
		/// initialize
		/// </summary>
		public TestFixture()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", true)
				.Build();

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.LiterateConsole()
				.WriteTo.Debug()
				.CreateLogger();

			// initialize configuration
			Options = new TestOptions();
			configuration.GetSection("Http").Bind(Options);

			// DI
			var services = new ServiceCollection();
			services.AddSingleton(s => Log.Logger);
			services.AddSingleton<IHttpProxyConfiguration>(Options);
			services.AddSingleton<HttpProxyFactory>();

			// common IHttpClientFactory
			services.AddHttpClient();

			// parse & check proxies ; save it into configuration
			Options.CheckAndAssignToConfig(required: 1, throwWhenLessThanRequired: true);

			// common client, with all settings from Configuration
			services.AddHttpClient("COMMON", Options);
			// common client, with retry = 5
			services.AddHttpClient("5RETRY", retry: 5);
			// common client, with configured for retry when 404 status found (example)
			services.AddHttpClient("404TEST", Options, whenRetry: res => res.StatusCode == HttpStatusCode.NotFound);
			services.AddHttpClientProxy("PROXY", Options);

			Services = services.BuildServiceProvider();
		}

		/// <summary>
		/// clean up
		/// </summary>
		public void Dispose()
		{
		}
	}
}
