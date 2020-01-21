using System;
using System.IO;
using System.Reflection;
using Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace GrpcService1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
           
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args)

        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
            .UseWindowsService()

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        var config = new ConfigurationBuilder()
                     .AddJsonFile("hosting.json", optional: true)
                       .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                        .AddCommandLine(args)
                        .Build();
                        var endPoint = config.CreateIPEndPoint();

                        // ListenAnyIP will work with IPv4 and IPv6.
                        // Chosen over Listen+IPAddress.Loopback, which would have a 2 second delay when
                        // creating a connection on a local Windows machine.
                        options.ListenAnyIP(endPoint.Port, listenOptions =>
                        {
                            var protocol = config["protocol"] ?? "";

                            Console.WriteLine($"Protocol: {protocol}");

                            if (protocol.Equals("h2", StringComparison.OrdinalIgnoreCase))
                            {
                                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;

                                var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                                var certPath = Path.Combine(basePath!, "Certs/testCert.pfx");
                                listenOptions.UseHttps(certPath, "testPassword", httpsOptions =>
                                {
#if CLIENT_CERTIFICATE_AUTHENTICATION
                                    httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.AllowCertificate;
#endif
                                });
                            }
                            else if (protocol.Equals("h2c", StringComparison.OrdinalIgnoreCase))
                            {
                                listenOptions.Protocols = HttpProtocols.Http2;
                            }
                            else if (protocol.Equals("http1", StringComparison.OrdinalIgnoreCase))
                            {
                                listenOptions.Protocols = HttpProtocols.Http1;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Unexpected protocol: {protocol}");
                            }
                        });
                    });
                });

            return hostBuilder;
        }
    }
}
