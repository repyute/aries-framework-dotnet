
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Utils;

namespace WebAgent.Protocols.GenericFetch
{
    /// <summary>
    /// A collection of convenience methods for the <see cref="IGenericFetchService"/> class.
    /// </summary>
    public static class GenericFetchServiceExtensions
    {
        ///// <summary>Retrieves a list of credential offers</summary>
        ///// <param name="credentialService">Credential service.</param>
        ///// <param name="context">The context.</param>
        ///// <param name="count">Count.</param>
        ///// <returns>The offers async.</returns>
        //public static Task<List<CredentialRecord>> ListOffersAsync(this ICredentialService credentialService,
        //    IAgentContext context, int count = 100)
        //    => credentialService.ListAsync(context,
        //       SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Offered.ToString("G")), count);

        ///// <summary>Retrieves a list of credential requests</summary>
        ///// <param name="credentialService">Credential service.</param>
        ///// <param name="context">The context.</param>
        ///// <param name="count">Count.</param>
        ///// <returns>The requests async.</returns>
        //public static Task<List<CredentialRecord>> ListRequestsAsync(this ICredentialService credentialService,
        //    IAgentContext context, int count = 100)
        //    => credentialService.ListAsync(context,
        //        SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Requested.ToString("G")), count);

        ///// <summary>Retrieves a list of issued credentials</summary>
        ///// <param name="credentialService">Credential service.</param>
        ///// <param name="context">The context.</param>
        ///// <param name="count">Count.</param>
        ///// <returns>The issued credentials async.</returns>
        //public static Task<List<CredentialRecord>> ListIssuedCredentialsAsync(this ICredentialService credentialService,
        //    IAgentContext context, int count = 100)
        //    => credentialService.ListAsync(context,
        //        SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Issued.ToString("G")), count);

        ///// <summary>Retrieves a list of revoked credentials</summary>
        ///// <param name="credentialService">Credential service.</param>
        ///// <param name="context">The context.</param>
        ///// <param name="count">Count.</param>
        ///// <returns>The revoked credentials async.</returns>
        //public static Task<List<CredentialRecord>> ListRevokedCredentialsAsync(
        //    this ICredentialService credentialService, IAgentContext context, int count = 100)
        //    => credentialService.ListAsync(context,
        //        SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Revoked.ToString("G")), count);

        ///// <summary>Retrieves a list of rejected/declined credentials.
        ///// Rejected credentials will only be found in the issuer wallet, as the rejection is not communicated back to the holder.</summary>
        ///// <param name="credentialService">Credential service.</param>
        ///// <param name="context">The context.</param>
        ///// <param name="count">Count.</param>
        ///// <returns>The rejected credentials async.</returns>
        //public static Task<List<CredentialRecord>> ListRejectedCredentialsAsync(
        //    this ICredentialService credentialService, IAgentContext context, int count = 100)
        //    => credentialService.ListAsync(context,
        //        SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Rejected.ToString("G")), count);

        public static async Task<GenericFetchRecord> GetByThreadIdAsync(
            this IGenericFetchService genericFetchService, IAgentContext context, string threadId)
        {
            var search = await genericFetchService.ListAsync(context, SearchQuery.Equal(TagConstants.LastThreadId, threadId), 100);

            if (search.Count == 0)
                throw new AriesFrameworkException(ErrorCode.RecordNotFound, $"GenericFetch record not found by thread id : {threadId}");

            if (search.Count > 1)
                throw new AriesFrameworkException(ErrorCode.RecordInInvalidState, $"Multiple GenericFetch records found by thread id : {threadId}");

            return search.Single();
        }
    }
}