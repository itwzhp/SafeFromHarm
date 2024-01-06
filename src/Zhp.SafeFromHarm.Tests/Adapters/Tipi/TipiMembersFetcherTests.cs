using Zhp.SafeFromHarm.Domain.Model.AccountCreation;
using Zhp.SafeFromHarm.Func.Adapters.Tipi;

namespace Zhp.SafeFromHarm.Tests.Adapters.Tipi;

public class TipiMembersFetcherTests
{
    private readonly TipiMembersFetcher subject;
    private readonly TestHandler httpHandler = new();

    public TipiMembersFetcherTests()
    {
        subject = new TipiMembersFetcher(new(httpHandler) { BaseAddress = new("https://example.zhp.pl") });
    }

    [Fact]
    public async Task GetMember_ExistingMember_ProperResult()
    {
        httpHandler.ResponseBody["/memberdetails/AA123"] = """
        {
        	"memberId": "AA123",
        	"personId": 112233,
        	"firstName": "Jan",
        	"lastName": "Kowalski",
        	"birthdate": 1044486000,
        	"exitdate": null,
        	"activeMember": true,
        	"allocationUnitName": null,
        	"allocationUnitId": null,
        	"hufiec": null,
        	"choragiew": null,
        	"requiredConsents": true,
        	"m365MinorConsent": true
        }
        """;

        var result = await subject.GetMember("AA123", CancellationToken.None);

        result.Should().Be(new Member("Jan", "Kowalski", "AA123"));
    }

    [Fact]
    public async Task GetMember_NotExistingMember_Null()
    {
        httpHandler.StatusCode = System.Net.HttpStatusCode.NotFound;
        httpHandler.ResponseBody["/memberdetails/AA123"] = """
        {
        	"detail": "Member not found"
        }
        """;

        var result = await subject.GetMember("AA123", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMember_NotActiveMember_Null()
    {
        httpHandler.ResponseBody["/memberdetails/AA123"] = """
        {
        	"memberId": "AA123",
        	"personId": 112233,
        	"firstName": "Jan",
        	"lastName": "Kowalski",
        	"birthdate": 1044486000,
        	"exitdate": null,
        	"activeMember": false,
        	"allocationUnitName": null,
        	"allocationUnitId": null,
        	"hufiec": null,
        	"choragiew": null,
        	"requiredConsents": true,
        	"m365MinorConsent": true
        }
        """;

        var result = await subject.GetMember("AA123", CancellationToken.None);

        result.Should().BeNull();
    }
}
