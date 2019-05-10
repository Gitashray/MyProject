namespace DigitalAssets.API
{
    using System;
    using System.IO;
    using DigitalAssets.API.Helpers;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using NLog;

    public class Program
    {
        private static ILogger _logger;
        private static AppSettings _appSettings;

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            var serviceProvider = host.Services;
            _logger = serviceProvider.GetService<ILogger>();
            _appSettings = serviceProvider.GetService<AppSettings>();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(o =>
                {
                    try
                    {
                        Uri uri = new Uri(_appSettings.HostURL);
                        IPAddress ipaddress = IPAddress.Parse(uri.Host);

                        o.Listen(new IPEndPoint(ipaddress, uri.Port), listenOptions =>
                        {
                            if (_appSettings.UseSecureProtocol.ToLower() == "true")
                            {
                                string filePath = _appSettings.TlsCertificate;
                                string password = File.ReadAllText(_appSettings.TlsFilePath);

                                if (filePath.IsStringEmpty() || password.IsStringEmpty())
                                {
                                    Console.WriteLine("Certificate details are missing in config");
                                }
                                else
                                {
                                    listenOptions.UseHttps(filePath, password);
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error establishing connection.");
                        Console.Write(ex);
                    }
                });
    }
}
