using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpcUaWinForms.Opc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Workstation.ServiceModel.Ua;

namespace OpcUaWinForms {
    static class Program {
        static void Main() {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: false)
                .Build();
            Host.CreateDefaultBuilder()
                .ConfigureLogging(builder => {
                    builder.AddDebug();
                })
                .UseWindowsFormsLifetime<FrmMain>(options => {
                    options.HighDpiMode = HighDpiMode.SystemAware;
                    options.EnableVisualStyles = true;
                    options.CompatibleTextRenderingDefault = false;
                    options.SuppressStatusMessages = false;
                    options.EnableConsoleShutdown = false;
                })
                .ConfigureServices(services => {
                    var appDesc = new ApplicationDescription() {
                        ApplicationName = $"{nameof(OpcUaWinForms)}",
                        ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:{nameof(OpcUaWinForms)}",
                        ApplicationType = ApplicationType.Client
                    };
                    services.AddSingleton(appDesc);
                    services.Configure<SessionConfig>(config.GetSection(nameof(SessionConfig)));
                    services.AddHostedService<OpcService>();
                })
                .Build()
                .Run();
        }
    }
}
