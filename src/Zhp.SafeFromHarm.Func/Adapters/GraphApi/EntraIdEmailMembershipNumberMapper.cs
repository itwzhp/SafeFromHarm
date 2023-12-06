using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;
namespace Zhp.SafeFromHarm.Func.Adapters.GraphApi;

internal class EntraIdEmailMembershipNumberMapper(
    GraphServiceClient client,
    IOptions<SafeFromHarmOptions> options,
    ILogger<EntraIdEmailMembershipNumberMapper> logger) : IEmailMembershipNumberMapper
{
    private IReadOnlyDictionary<string, string>? map;

    public async ValueTask<string?> GetMembershipNumberForEmail(string email)
    {
        var fakeMailSuffix = $"@{options.Value.MoodleAccountMailFakeDomain}";

        if (email.EndsWith(fakeMailSuffix))
            return email.Replace(fakeMailSuffix, string.Empty);

        this.map ??= await BuildMap();

        return map.GetValueOrDefault(email);
    }

    private async Task<IReadOnlyDictionary<string, string>> BuildMap()
    {
        logger.LogInformation("Getting list of users from Entra ID...");

        var request = await client.Users
            .GetAsync(p =>
            {
                p.Headers.Add("ConsistencyLevel", "eventual");

                p.QueryParameters.Filter = $"startsWith(employeeType,'Tipi')"; // Tipi or Tipi-automat-tmp
                p.QueryParameters.Select = ["userPrincipalName", "employeeId"];
                p.QueryParameters.Top = 999;    
                p.QueryParameters.Count = true; // GraphAPI hidden feature - makes filtering by employee type magically work. Source: https://learn.microsoft.com/en-us/answers/questions/377469/ms-graph-filter-users-using-employeetype
            },
            CancellationToken.None)
                ?? throw new Exception("Unable to get a list of users from Entra ID Graph API");

        var result = new Dictionary<string, string>(((int?)request.OdataCount) ?? 0, StringComparer.OrdinalIgnoreCase);

        var iterator = PageIterator<User, UserCollectionResponse>.CreatePageIterator(client, request,
            u =>
            {
                if(!string.IsNullOrEmpty(u.UserPrincipalName) && !string.IsNullOrEmpty(u.EmployeeId))
                    result.Add(u.UserPrincipalName, u.EmployeeId);

                return true;
            });

        await iterator.IterateAsync();

        logger.LogInformation("Fetched {numberOfUsers} users from Entra ID.", result.Count);

        return result;
    }
}
