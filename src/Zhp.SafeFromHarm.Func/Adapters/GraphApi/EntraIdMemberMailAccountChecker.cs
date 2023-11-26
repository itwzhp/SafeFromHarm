using Microsoft.Graph;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.GraphApi;

internal class EntraIdMemberMailAccountChecker(GraphServiceClient client) : IMemberMailAccountChecker
{
    public async Task<bool> HasEmailAccount(string membershipId, CancellationToken cancellationToken)
    {
        var result = await client.Users.Count
            .GetAsync(p =>
            {
                p.QueryParameters.Filter = $"employeeId eq '{membershipId}' and accountEnabled eq true";
                p.Headers.Add("ConsistencyLevel", "eventual");
            },
            cancellationToken);

        return result > 0;
    }
}
