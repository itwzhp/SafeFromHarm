using Microsoft.Extensions.Options;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal class TipiUnitsFetcher(HttpClient httpClient, IOptions<SafeFromHarmOptions> options)
{
    private readonly string? controlTeamsChannelMail = options.Value.ControlTeamsChannelMail;
    private IReadOnlyDictionary<int, Unit>? units;

    public async ValueTask<Unit?> GetUnit(int id)
    {
        var map = units ??= await GetUnits();

        return map?.GetValueOrDefault(id);
    }

    private async Task<IReadOnlyDictionary<int, Unit>> GetUnits()
    {
        using var result = await httpClient.GetAsync("/orgunits");
        result.EnsureSuccessStatusCode();

        using var stream = await result.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsyncEnumerable<UnitDto>(stream)
            .Select(MapUnit)
            .OfType<Unit>()
            .ToDictionaryAsync(u => u.Id);
    }

    private Unit? MapUnit(UnitDto? dto)
    {
        if(dto == null)
            return null;

        var mail = dto.primaryEmail ?? controlTeamsChannelMail;
        mail = mail?.Split(';').First();

        return mail == null
            ? null
            : new(dto.orgunitId, dto.name, mail);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used for deserialization")]
    private record UnitDto(int orgunitId, string name, string? primaryEmail);
}
