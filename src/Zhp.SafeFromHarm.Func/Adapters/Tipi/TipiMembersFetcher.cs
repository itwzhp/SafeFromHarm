using System.Text.Json;
using Zhp.SafeFromHarm.Domain.Model.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal class TipiMembersFetcher(HttpClient httpClient) : IMembersFetcher
{
    public async Task<Member?> GetMember(string membershipId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"memberdetails/{membershipId}", cancellationToken);

        if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = (await JsonSerializer.DeserializeAsync<MemberDto>(stream, cancellationToken: cancellationToken))
            ?? throw new Exception("Received null result from Tipi");

        return result.activeMember
            ? new(result.firstName, result.lastName, result.memberId)
            : null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used to map json")]
    private record MemberDto(string memberId, string firstName, string lastName, bool activeMember);
}
