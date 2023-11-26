using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.GraphApi;

internal class SharepointAccountCreationResultPublisher(GraphServiceClient client, IOptions<GraphApiOptions> options) : IAccountCreationResultPublisher
{
    public async Task PublishResult(IReadOnlyCollection<AccountCreationResult> result, string requestorEmail)
    {
        var requests = result
            .Where(r => r.Result == AccountCreationResult.ResultType.Success && r.Password != null)
            .Select(r => BuildRequest(r.Member, r.Password!, requestorEmail));

        var batchRequest = new BatchRequestContentCollection(client);

        foreach (var request in requests)
            await batchRequest.AddBatchRequestStepAsync(request);

        await client.Batch.PostAsync(batchRequest);
    }

    private RequestInformation BuildRequest(Member member, string password, string requestorEmail)
        => client
                .Sites[options.Value.SfhSiteId.ToString()]
                .Lists[options.Value.CreatedAccountsListId.ToString()]
                .Items.ToPostRequestInformation(
                    new()
                    {
                        Fields = new()
                        {
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["Title"] = member.MembershipNumber,
                                ["Imi_x0119_"] = member.FirstName,
                                ["Nazwisko"] = member.LastName,
                                ["Has_x0142_o"] = password,
                                ["Zak_x0142_adaj_x0105_cy"] = requestorEmail,
                            }
                        }
                    });
}
