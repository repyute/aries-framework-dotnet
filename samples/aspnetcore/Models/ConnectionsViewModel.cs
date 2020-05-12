using System.Collections.Generic;
using Hyperledger.Aries.Features.DidExchange;
using WebAgent.Protocols.BasicMessage;
using WebAgent.Protocols.GenericFetch;

namespace WebAgent.Models
{
    public class ConnectionsViewModel
    {
        public IEnumerable<ConnectionRecord> Connections { get; set; }

        public IEnumerable<ConnectionRecord> Invitations { get; set; }
    }

    public class ConnectionDetailsViewModel
    {
        public ConnectionRecord Connection { get; set; }

        public IEnumerable<BasicMessageRecord> Messages { get; set; }

        public IEnumerable<GenericFetchRecord> FetchRecords { get; set; }

        public bool? TrustPingSuccess { get; set; }
    }
}
