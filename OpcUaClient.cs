using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaWinForms {
    class OpcSubThread {
        public static async Task Run(CancellationToken cancelToken, Action onValueChanged) {
            var clientDescription = new ApplicationDescription {
                ApplicationName = "Workstation.UaClient.FeatureTests",
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.UaClient.FeatureTests",
                ApplicationType = ApplicationType.Client
            };
            var channel = new UaTcpSessionChannel(
                clientDescription,
                null,
                new AnonymousIdentity(),
                "opc.tcp://localhost:4840",
                SecurityPolicyUris.None
            );
            try {
                await channel.OpenAsync();
                // --------------------------------
                var res = await channel.CreateSubscriptionAsync(
                    new CreateSubscriptionRequest {
                        RequestedPublishingInterval = 1000.0, // ms
                        RequestedMaxKeepAliveCount = 30,
                        RequestedLifetimeCount = 30 * 3,
                        PublishingEnabled = true,
                    }, 
                    cancelToken
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
                var isInError = false;
                var token = channel
                    // While session is open, the client sends a stream of PublishRequests to the server.
                    // You can subscribe to all the PublishResponses.
                    .Where(response => response.SubscriptionId == id)
                    .Subscribe(
                        onNext: response => {
                            var notifications = response.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                            foreach (var notification in notifications) {
                                foreach (var item in notification.MonitoredItems) {
                                    if (item.ClientHandle == btnBrowseHandle) {
                                        if ((bool)item.Value.Value) {
                                            onValueChanged();
                                        }
                                    }
                                }
                            }
                        },
                        onError: ex => {
                            isInError = true;
                        }
                    );
                while (!cancelToken.IsCancellationRequested && !isInError) {
                    await Task.Delay(500);
                }
                await channel.CloseAsync();
            }
            catch (Exception ex) {
                await channel.AbortAsync();
                Console.WriteLine(ex.Message);
            }
        }
    }
}
