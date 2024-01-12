using Azure.Identity;
using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Func.Adapters.GraphApi;

namespace Zhp.SafeFromHarm.Tests.Adapters.GraphApi;

public class SharepointUnitContactMailProviderTests
{
    private readonly SharepointUnitContactMailProvider subject = new(new(new InteractiveBrowserCredential(), ["https://graph.microsoft.com/.default"]),
        Options.Create(new GraphApiOptions
        {
            SfhSiteId= new("68e38698-4c2b-46a5-b278-a45bc93df050"),
            UnitContactsListId= new("ac263482-0069-4c68-9609-8287fa1f8629")
        }));

    [Fact(Skip = "This is integration test! Don't run automatically")]
    public async Task GK_ProperResults()
    {
        var result = await subject.GetEmailAddresses(2).ToListAsync();

        result.Should().ContainSingle();
    }
}
