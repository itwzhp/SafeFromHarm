using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Zhp.SafeFromHarm.Domain.Model.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.GraphApi;

internal class SharepointAccountCreationResultPublisher(
    GraphServiceClient client,
    IOptions<GraphApiOptions> options,
    ILogger<SharepointAccountCreationResultPublisher> logger) : IAccountCreationResultPublisher
{
    public async Task PublishResult(IReadOnlyCollection<AccountCreationResult> result, string requestorEmail)
    {
        var requests = result
            .Where(r => r.Result == AccountCreationResult.ResultType.Success && r.Password != null)
            .Select(r => BuildRequest(r.Member, r.Password!, requestorEmail));

        var batchRequest = new BatchRequestContentCollection(client);

        foreach (var request in requests)
            await batchRequest.AddBatchRequestStepAsync(request);

        var requestResponses = await client.Batch.PostAsync(batchRequest);
        var requestStatusCodes = await requestResponses.GetResponsesStatusCodesAsync();

        foreach (var (id, code) in requestStatusCodes)
        {
            if ((int)code >= 200 || (int)code <= 299)
            {
                logger.LogInformation("Sharepoint account entry posted properly");
            }
            else
            {
                var message = await (await requestResponses.GetResponseByIdAsync(id)).Content.ReadAsStringAsync();

                logger.LogError("Error while posting account entry to sharepoint {code}: {response}",
                    code,
                    message);
            }
        }
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
