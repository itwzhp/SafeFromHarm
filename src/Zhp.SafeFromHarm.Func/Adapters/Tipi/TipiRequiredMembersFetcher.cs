using Microsoft.Extensions.Options;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal class TipiRequiredMembersFetcher : IRequiredMembersFetcher
{
    private readonly HttpClient httpClient;
    private readonly string? controlTeamsChannelMail;

    public TipiRequiredMembersFetcher(HttpClient httpClient, IOptions<SafeFromHarmOptions> options)
    {
        this.httpClient = httpClient;
        this.controlTeamsChannelMail = options.Value.ControlTeamsChannelMail;
    }

    public async IAsyncEnumerable<ZhpMember> GetMembersRequiredToCertify()
    {
        var response = await httpClient.GetAsync("sfhmembersfortrainig");
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.DeserializeAsyncEnumerable<ResultEntry>(await response.Content.ReadAsStreamAsync())
            ?? throw new Exception("Received null result from Tipi");

        var members = await result.Select(Map).OfType<ZhpMember>().ToListAsync();
        if (!members.Any())
            throw new Exception("Received empty results from Tipi");

        foreach (var member in members)
            yield return member;
    }

    private ZhpMember? Map(ResultEntry? entry)
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
    public record ResultEntry(
        string memberId,
        string firstName,
        string lastName,
        string? allocationUnitName,
        string? allocationUnitContactEmails);

}
