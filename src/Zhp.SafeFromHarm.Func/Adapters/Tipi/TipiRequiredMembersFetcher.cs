using Microsoft.Extensions.Options;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal class TipiRequiredMembersFetcher(HttpClient httpClient, IOptions<SafeFromHarmOptions> options) : IRequiredMembersFetcher
{
    private readonly string? controlTeamsChannelMail = options.Value.ControlTeamsChannelMail;

    public async IAsyncEnumerable<MemberToCertify> GetMembersRequiredToCertify()
    {
        var response = await httpClient.GetAsync("sfhmembersfortrainig");
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.DeserializeAsyncEnumerable<ResultEntry>(await response.Content.ReadAsStreamAsync())
            ?? throw new Exception("Received null result from Tipi");

        var members = await result.Select(Map).OfType<MemberToCertify>().ToListAsync();
        if (!members.Any())
            throw new Exception("Received empty results from Tipi");

        foreach (var member in members)
            yield return member;
    }

    private MemberToCertify? Map(ResultEntry? entry)
    {
        if (entry == null)
            return null;

        var mail = entry.allocationUnitContactEmails?.Split(";").FirstOrDefault() ?? controlTeamsChannelMail;
        if (mail == null)
            return null;

        return new(
                entry.firstName,
                entry.lastName,
                entry.memberId,
                mail,
                entry.allocationUnitName ?? "Brak przypisanej jednostki");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used for deserialization")]
    private record ResultEntry(
        string memberId,
        string firstName,
        string lastName,
        string? allocationUnitName,
        string? allocationUnitContactEmails);

}
