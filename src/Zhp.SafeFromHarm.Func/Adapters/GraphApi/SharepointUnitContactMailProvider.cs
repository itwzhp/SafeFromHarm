using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.GraphApi;

internal class SharepointUnitContactMailProvider(GraphServiceClient client, IOptions<GraphApiOptions> options) : IUnitContactMailProvider
{
    private Dictionary<int, string[]>? map;
    public async IAsyncEnumerable<string> GetEmailAddresses(int unitId)
    {
        this.map ??= await BuildMap();

        var results = map.TryGetValue(unitId, out var contacts)
            ? contacts
            : Enumerable.Empty<string>();

        foreach (var result in results)
            yield return result;
    }

    private async Task<Dictionary<int, string[]>> BuildMap()
    {
        var response = await client
            .Sites[options.Value.SfhSiteId.ToString()]
            .Lists[options.Value.UnitContactsListId.ToString()]
            .Items.GetAsync(c =>
            {
                c.QueryParameters.Select = ["fields"];
                c.QueryParameters.Expand = ["fields($select=IDJednostki,Kontakt)"];
            });

        if (response == null)
            return [];

        var result = new Dictionary<int, string[]>();

        var iterator = PageIterator<ListItem, ListItemCollectionResponse>.CreatePageIterator(client, response,
            i =>
            {
                if (i.Fields?.AdditionalData.TryGetValue("IDJednostki", out var unitIdObj) != true || unitIdObj is not decimal unitIdDecimal)
                    return true;

                if (i.Fields?.AdditionalData.TryGetValue("Kontakt", out var contactsObj) != true || contactsObj is not JsonElement { ValueKind: JsonValueKind.Array } contactsArray)
                    return true;

                int unitId = (int)unitIdDecimal;

                if (unitId <= 0)
                    return true;

                var contacts = contactsArray.EnumerateArray()
                    .Select(c => c.TryGetProperty("Email", out var email) && email.ValueKind == JsonValueKind.String ? email.GetString() : null)
                    .OfType<string>()
                    .ToArray();

                if (contacts.Length == 0)
                    return true;

                result[unitId] = contacts;

                return true;
            });

        await iterator.IterateAsync();

        return result;
    }
}
