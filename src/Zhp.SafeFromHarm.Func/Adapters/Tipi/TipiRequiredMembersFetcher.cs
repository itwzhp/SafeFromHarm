using System.Text.Json;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal class TipiRequiredMembersFetcher : IRequiredMembersFetcher
{
    private readonly HttpClient httpClient;

    public TipiRequiredMembersFetcher(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async IAsyncEnumerable<ZhpMember> GetMembersRequiredToCertify()
    {
        var response = await httpClient.GetAsync("sfhmembersfortrainig");
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.DeserializeAsyncEnumerable<ResultEntry>(await response.Content.ReadAsStreamAsync())
            ?? throw new Exception("Received null result from Tipi");

        var members = await result.OfType<ResultEntry>().ToListAsync();
        if (!members.Any())
            throw new Exception("Received empty results from Tipi");

        foreach (var member in members)
            yield return new(
                member.firstName,
                member.lastName,
                member.memberId,
                member.allocationUnitContactEmails,
                member.allocationUnitName);
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used for deserialization")]
    public record ResultEntry(
        string memberId,
        string firstName,
        string lastName,
        string allocationUnitName,
        string allocationUnitContactEmails);

}
