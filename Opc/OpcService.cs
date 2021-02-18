using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaWinForms.Opc {
    public class OpcService : IHostedService {
        private ILogger<OpcService> _logger;
        private readonly List<UaTcpSessionChannel> _channels = new List<UaTcpSessionChannel>();
        private readonly ReplaySubject<SessionConfig> _sessionConfigs;
        private ApplicationDescription _appDesc;
        private FrmMain _frmMain;

        public OpcService(
            IOptions<SessionConfig> sessionConfigOptions, 
            ILogger<OpcService> logger, 
            ApplicationDescription appDesc,
            FrmMain frmMain
            ) {
            _sessionConfigs = new ReplaySubject<SessionConfig>();
            _logger = logger;
            _appDesc = appDesc;
            _frmMain = frmMain;
            var sessionConfig = sessionConfigOptions.Value;
            _sessionConfigs.OnNext(sessionConfig);
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation($"Starting opc service v{Assembly.GetCallingAssembly().GetName().Version}");
            await InitializeChannels(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation($"Stopping opc service v{Assembly.GetCallingAssembly().GetName().Version}");
            await CloseChannels(cancellationToken);
        }

        // --

        private Task InitializeChannels(CancellationToken cancellationToken) {
            _sessionConfigs.Delay(TimeSpan.FromSeconds(1)).Subscribe(async sessionConfig => {
                if (!sessionConfig.EndpointUrl.StartsWith("opc.tcp://"))
                    throw new Exception($"SessionConfiguration {sessionConfig.Name}'s EndpointUrl is in a bad format: it must start with 'opc.tcp://'.");
                var channel = new UaTcpSessionChannel(_appDesc, sessionConfig.CertificateStore, sessionConfig.UserIdentity, sessionConfig.EndpointUrl, sessionConfig.SecurityPolicy);
                _channels.Add(channel);
                //IDisposable watchdog = null;
                channel.Faulted += (sender, args) => {
                    //watchdog?.Dispose();
                    _sessionConfigs.OnNext(sessionConfig);
                    _channels.Remove(channel);
                };
                try { 
                    await channel.OpenAsync(cancellationToken);
                    _logger.LogInformation($"Successfully connected to {sessionConfig.EndpointUrl}");
                    //watchdog = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(async o => {
                    //    try {
                    //        var rq = new ReadRequest() {
                    //            NodesToRead = 
                    //        };
                    //        var response = await channel.RequestAsync(new CallRequest()); 
                    //    }
                    //    catch (Exception ex) {
                    //        _logger.LogError(ex, "Channel watchdog couldn't complete its request to server.");
                    //    }
                    //});
                    await ConfigureSubscriptions(channel, cancellationToken);
                }
                catch (Exception ex) {
                    _logger.LogError($"Error connecting to {sessionConfig.EndpointUrl}: {ex.Message}");
                    try { await channel.CloseAsync(cancellationToken); }
                    catch (Exception) {
                        _logger.LogError(ex, "Error while closing channel, but it shouldn't cause too much trouble.");
                    }
                }
            });
            return Task.CompletedTask;
        }

        private async Task CloseChannels(CancellationToken cancellationToken) {
            foreach (var channel in _channels) {
                try { await channel.CloseAsync(cancellationToken); }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error while closing channel, but it shouldn't cause too much trouble.");
                }
            }
        }

        // --

        public async Task ConfigureSubscriptions(UaTcpSessionChannel channel, CancellationToken cancellationToken) {
            var res = await channel.CreateSubscriptionAsync(
                new CreateSubscriptionRequest {
                    RequestedPublishingInterval = 1000.0, // ms
                    RequestedMaxKeepAliveCount = 30,
                    RequestedLifetimeCount = 30 * 3,
                    PublishingEnabled = true,
                },
                cancellationToken
            );
            var id = res.SubscriptionId;
            var btnBrowseHandle = (uint)42;
            var rq = new CreateMonitoredItemsRequest {
                SubscriptionId = id,
                TimestampsToReturn = TimestampsToReturn.Both,
                ItemsToCreate = new MonitoredItemCreateRequest[] {
                        new MonitoredItemCreateRequest {
                            ItemToMonitor= new ReadValueId {
                                NodeId = NodeId.Parse("ns=6;s=::AsGlobalPV:dbgBtnBrowsePrograms"),
                                AttributeId = AttributeIds.Value
                            },
                            MonitoringMode = MonitoringMode.Reporting,
        	                // Specify a unique ClientHandle,
                            // which will be returned in the PublishResponse.
        	                RequestedParameters = new MonitoringParameters{
                                ClientHandle = btnBrowseHandle, QueueSize = 2, DiscardOldest = true, SamplingInterval = 1000.0
                            },
                        },
                    },
            };
            var res2 = await channel.CreateMonitoredItemsAsync(rq);
            var unsubToken = channel
                // While session is open, the client sends a stream of PublishRequests to the server.
                // You can subscribe to all the PublishResponses.
                .Where(response => response.SubscriptionId == id)
                .Subscribe(
                    onNext: response => {
                        var notifications = response.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                        foreach (var notification in notifications) {
                            foreach (var item in notification.MonitoredItems) {
                                if (item.ClientHandle == btnBrowseHandle) {
                                    if (item.Value.Value is null) return;
                                    if ((bool)item.Value.Value) {
                                        _frmMain.OnBrowseSafe();
                                    }
                                }
                            }
                        }
                    },
                    onError: ex => {
                        // TODO: What should I go?
                    }
                );
        }
    }
}