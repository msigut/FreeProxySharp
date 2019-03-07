using System;
using System.IO;
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
			services.AddSingleton<HttpProxyClient>();

			// proxy
			Options.AssignToConfig(codeFilter: new[] { "SE", "DE", "ES", "PL" }, required: 2);
			services.AddHttpProxyClient(Options);

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
