# README

## Reconnect
```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Haprotec.Opc.Sessions;
using Microsoft.Extensions.Hosting;
using NLog;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace Haprotec.Opc
{
    public class OpcService : IOpcService, IHostedService
    {
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // ##############################################################################################################################
        // Properties
        // ##############################################################################################################################

        #region Properties

        // ##########################################################################################
        // Public Properties
        // ##########################################################################################


        // ##########################################################################################
        // Private Properties
        // ##########################################################################################

        private readonly List<UaTcpSessionChannel> _Channels = new List<UaTcpSessionChannel>();
        private readonly ReplaySubject<SessionConfig> _SessionConfigs;
        
        #endregion

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.Info($"starting opc service v{Assembly.GetCallingAssembly().GetName().Version}");

            await _InitializeChannels(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.Info($"stopping opc service v{Assembly.GetCallingAssembly().GetName().Version}");

            await _DisposeChannels(cancellationToken);
        }


        // ##############################################################################################################################
        // Constructor
        // ##############################################################################################################################

        #region Constructor

        public OpcService(IEnumerable<SessionConfig> sessionConfigs)
        {
            _SessionConfigs = new ReplaySubject<SessionConfig>();
            foreach (SessionConfig sessionConfig in sessionConfigs)
            {
                Logger.Debug($"add new channel for endpoint {sessionConfig.DiscoveryUrl}");

                _SessionConfigs.OnNext(sessionConfig);
            }
        }

        private Task<IUserIdentity> _UserIdentityProvider(EndpointDescription endpoint)
        {
            IUserIdentity userIdentity = null;

            // if server accepts anonymous identity, then choose to remain anonymous.
            if (endpoint.UserIdentityTokens.Any(p => p.TokenType == UserTokenType.Anonymous))
            {
                userIdentity = new AnonymousIdentity();
            }

            // if server accepts username and password identity, then ask the user.
            else if (endpoint.UserIdentityTokens.Any(p => p.TokenType == UserTokenType.UserName))
            {
                Console.WriteLine("Server is requesting UserName identity...");
                Console.Write("Enter user name: ");
                var userName = Console.ReadLine();
                Console.Write("Enter password: ");
                var password = Console.ReadLine();
                userIdentity = new UserNameIdentity(userName, password);
            }

            else
            {
                Console.WriteLine("Program supports servers requesting Anonymous and UserName identity.");
            }

            return Task.FromResult(userIdentity);
        }

        #endregion

        // ##############################################################################################################################
        // Private Methods
        // ##############################################################################################################################

        #region Private Methods

        private Task _InitializeChannels(CancellationToken cancellationToken)
        {
            // initialize sessions
            _SessionConfigs.Delay(TimeSpan.FromSeconds(1)).Subscribe(async sessionConfig =>
            {
                var appDescription = new ApplicationDescription
                {
                    ApplicationName = "MyHomework",
                    ApplicationUri = $"urn:{sessionConfig.DiscoveryUrl}",
                    ApplicationType = ApplicationType.Client,
                };

                var discoveryUrl = $"opc.tcp://{sessionConfig.DiscoveryUrl}";

                var channel = new UaTcpSessionChannel(appDescription, sessionConfig.CertificateStore, _UserIdentityProvider, discoveryUrl);
                _Channels.Add(channel);

                // add watchdog
                IDisposable watchdog = null;
                channel.Opened += (sender, args) =>
                {
                    Logger.Info($"successfully connected to {discoveryUrl}");
                    watchdog = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(async o =>
                    {
                        try
                        {
                            var response = await channel.RequestAsync(new CallRequest());
                        }
                        catch (Exception)
                        {
                            // ignore
                        }
                    });
                };
                
                // add reconnect item on error
                channel.Faulted += (sender, args) =>
                {
                    // dispose watchdog
                    watchdog?.Dispose();
                    _SessionConfigs.OnNext(sessionConfig);
                    _Channels.Remove(channel);
                };
                
                try
                {
                    await channel.OpenAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Error($"error connecting to {discoveryUrl}: {ex.Message}");
                    try
                    {
                        await channel.CloseAsync(cancellationToken);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            });
            return Task.CompletedTask;
        }
        
        private async Task _DisposeChannels(CancellationToken cancellationToken)
        {
            try
            {
                foreach (UaTcpSessionChannel channel in _Channels)
                {
                    try
                    {
                        await channel.CloseAsync(cancellationToken);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while disposing channel");
            }
        }

        #endregion
    }
}
```