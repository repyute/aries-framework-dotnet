namespace WebAgent.Messages
{
    public static class CustomMessageTypes
    {
        /// <summary>
        /// Basic Message Type.
        /// </summary>
        public const string BasicMessageType = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/basicmessage/1.0/message";

        /// <summary>
        /// Ping Message Type.
        /// </summary>
        public const string TrustPingMessageType = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/trust_ping/1.0/ping";

        /// <summary>
        /// Ping Response Message Type.
        /// </summary>
        public const string TrustPingResponseMessageType = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/trust_ping/1.0/ping_response";

        /// <summary>
        /// Generic fetch Request Message Type.
        /// </summary>
        public const string GenericFetchRequestMessageType = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/generic_fetch/1.0/request";

        /// <summary>
        /// Generic fetch Response Message Type.
        /// </summary>
        public const string GenericFetchResponseMessageType = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/generic_fetch/1.0/response";
    }
}
