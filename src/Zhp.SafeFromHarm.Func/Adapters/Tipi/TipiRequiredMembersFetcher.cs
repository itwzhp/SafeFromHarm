using System.Text.Json;
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

        if(entry.choragiewId == null)
            return null;

        var choragiew = entry.choragiewId.Value;

        int supervisorId = entry.hufiecId ?? choragiew;

        var supervisor = await unitsFetcher.GetUnit(supervisorId);
        if (supervisor == null)
            return null;

        var department = await unitsFetcher.GetUnit(choragiew);
        if (department == null)
            return null;

        return new(
                entry.firstName,
                entry.lastName,
                entry.memberId,
                supervisor,
                department);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used for deserialization")]
    private record ResultEntry(
        string memberId,
        string firstName,
        string lastName,
        int? hufiecId,
        int? choragiewId);

}
