using Microsoft.Extensions.Options;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal class TipiRequiredMembersFetcher(
    HttpClient httpClient,
    TipiUnitsFetcher unitsFetcher) : IRequiredMembersFetcher
{
    public async IAsyncEnumerable<MemberToCertify> GetMembersRequiredToCertify()
    {
        using var response = await httpClient.GetAsync("sfhmembersfortrainig");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var result = JsonSerializer.DeserializeAsyncEnumerable<ResultEntry>(stream)
            ?? throw new Exception("Received null result from Tipi");

        var members = await result.SelectAwait(MapAsync).OfType<MemberToCertify>().ToListAsync();
        if (members.Count == 0)
            throw new Exception("Received empty results from Tipi");

        foreach (var member in members)
            yield return member;
    }

    private async ValueTask<MemberToCertify?> MapAsync(ResultEntry? entry)
    {
        if (entry == null)
            return null;

        int supervisorId = entry.hufiecId ?? entry.choragiewId;

        var unit = await unitsFetcher.GetUnit(supervisorId);
        if (unit == null)
            return null;

        return new(
                entry.firstName,
                entry.lastName,
                entry.memberId,
                unit);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used for deserialization")]
    private record ResultEntry(
        string memberId,
        string firstName,
        string lastName,
        int? hufiecId,
        int choragiewId);

}
