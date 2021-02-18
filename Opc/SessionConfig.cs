using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaWinForms.Opc {
    public class SessionConfig {
        /// <summary>
        /// Name of configuration for diagnostic purpose.
        /// </summary>
        public string Name { get; set; } = "Default";
        /// <summary>
        /// Format: opc.tcp://...
        /// </summary>
        public string EndpointUrl { get; set; }
        public ICertificateStore CertificateStore { get; set; }
        public IUserIdentity UserIdentity { get; set; } = new AnonymousIdentity();
        public string SecurityPolicy  = SecurityPolicyUris.None;
    }
}
