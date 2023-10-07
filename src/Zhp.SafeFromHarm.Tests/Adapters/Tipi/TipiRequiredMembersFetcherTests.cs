using System.Net;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Func.Adapters.Tipi;

namespace Zhp.SafeFromHarm.Tests.Adapters.Tipi;

public class TipiRequiredMembersFetcherTests
{
    private readonly TestHandler httpHandler = new();
    private readonly TipiRequiredMembersFetcher subject;

    public TipiRequiredMembersFetcherTests()
    {
        subject = new(new(httpHandler) { BaseAddress = new("https://example.zhp.pl") });
    }

    [Fact]
    public async Task EmptyResults_Exception()
    {
        httpHandler.ResponseBody = "[]";

        await subject.Awaiting(s => s.GetMembersRequiredToCertify().ToListAsync())
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SomeResults_MapsProperly()
    {
        httpHandler.ResponseBody = """
        [
            {
            	"memberId": "AA01",
            	"personId": 12345,
            	"firstName": "Jan",
            	"lastName": "Kowalski",
            	"birthdate": 1408744800,
            	"allocationUnitName": "Hufiec Radomsko",
            	"allocationUnitContactEmails": "radomsko@zhp.pl",
            	"memberRoles": "skarbnik"
            },
            {
            	"memberId": "AA02",
            	"personId": 54321,
            	"firstName": "Anna",
            	"lastName": "Malinowska",
            	"birthdate": 1374962400,
            	"allocationUnitName": "Hufiec Ziemi Cieszyńskiej",
            	"allocationUnitContactEmails": "cieszyn@zhp.pl",
            	"memberRoles": "członek referatu nieprzetartego szlaku"
            }
        ]
        """;

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().BeEquivalentTo(new ZhpMember[]
        {
            new("Jan", "Kowalski", "AA01", "radomsko@zhp.pl", "Hufiec Radomsko"),
            new("Anna", "Malinowska", "AA02", "cieszyn@zhp.pl", "Hufiec Ziemi Cieszyńskiej"),
        });
    }
    
    [Fact]
    public async Task DuplicateMail_TakesFirst()
    {
        httpHandler.ResponseBody = """
        [
            {
        	    "memberId": "AB123",
        	    "personId": 111,
        	    "firstName": "Jan",
        	    "lastName": "Kowalski",
        	    "birthdate": -446086800,
        	    "allocationUnitName": "Chorągiew Dolnośląska",
        	    "allocationUnitContactEmails": "choragiew@dolnoslaska.zhp.pl;dolnoslaska@zhp.pl",
        	    "memberRoles": null
            }
        ]
        """;

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.Should().Be(new ZhpMember("Jan", "Kowalski", "AB123", "choragiew@dolnoslaska.zhp.pl", "Chorągiew Dolnośląska"));
    }

    private class TestHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public string ResponseBody { get; set; } = string.Empty;

        override protected Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json") });
    }
}