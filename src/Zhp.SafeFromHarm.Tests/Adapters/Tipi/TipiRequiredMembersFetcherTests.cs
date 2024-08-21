using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Func.Adapters.Tipi;

namespace Zhp.SafeFromHarm.Tests.Adapters.Tipi;

public class TipiRequiredMembersFetcherTests
{
    private readonly TestHandler httpHandler = new();
    private readonly HttpClient httpClient;

    public TipiRequiredMembersFetcherTests()
    {
        httpClient = new(httpHandler) { BaseAddress = new("https://example.zhp.pl") };
        httpHandler.ResponseBody["/orgunit"] = """
        [
            {
                "orgunitId": 2657,
                "name": "Hufiec Radomsko",
                "primaryEmail": "radomsko@zhp.pl"
            },
            {
                "orgunitId": 6127,
                "name": "Hufiec Ziemi Cieszyńskiej",
                "primaryEmail": "cieszyn@zhp.pl"
            },
            {
                "orgunitId": 5967,
                "name": "Chorągiew Śląska",
                "primaryEmail": "choragiew@dolnoslaska.zhp.pl;biuro@slaska.zhp.pl"
            },
            {
                "orgunitId": 20006,
                "name": "Hufiec ZHP Powiatu Milickiego",
                "primaryEmail": null
            },
            {
                "orgunitId": 2031,
                "name": "Chorągiew Łódzka",
                "primaryEmail": "lodzka@zhp.pl"
            },
            {
        	    "orgunitId": 1416,
        	    "name": "Hufiec Chełm",
        	    "primaryEmail": null,
        	    "extraEmails": "chelm@zhp.pl;biuro@example.zhp.pl"
            }
        ]
        """;
    }

    private TipiRequiredMembersFetcher BuildSubject(string? controlTeamsChannelMail = null)
        => new(
            httpClient,
            new(httpClient, Options.Create(new SafeFromHarmOptions { ControlTeamsChannelMail = controlTeamsChannelMail })));

    [Fact]
    public async Task EmptyResults_Exception()
    {
        httpHandler.ResponseBody["/sfhmembersfortrainig"] = "[]";
        var subject = BuildSubject();

        await subject.Awaiting(s => s.GetMembersRequiredToCertify().ToListAsync())
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SomeResults_MapsProperly()
    {
        httpHandler.ResponseBody["/sfhmembersfortrainig"] = """
        [
            {
            	"memberId": "AA01",
            	"personId": 12345,
            	"firstName": "Jan",
            	"lastName": "Kowalski",
            	"birthdate": 1408744800,
                "memberRoles": "drużynowy",
                "hufiecId": 2657,
                "choragiewId": 2031
            },
            {
            	"memberId": "AA02",
            	"personId": 54321,
            	"firstName": "Anna",
            	"lastName": "Malinowska",
            	"birthdate": 1374962400,
                "memberRoles": "członek zespołu promocji i informacji hufca",
                "hufiecId": 6127,
                "choragiewId": 5967
            },
            {
        	    "memberId": "AB123",
        	    "personId": 111,
        	    "firstName": "Jan",
        	    "lastName": "Kowalski",
        	    "birthdate": -446086800,
                "memberRoles": null,
                "hufiecId": null,
                "choragiewId": 5967
            }
        ]
        """;
        var subject = BuildSubject();

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().BeEquivalentTo(new MemberToCertify[]
        {
            new("Jan", "Kowalski", "AA01", new(2657, "Hufiec Radomsko", "radomsko@zhp.pl"), new(2031, "Chorągiew Łódzka", "lodzka@zhp.pl")),
            new("Anna", "Malinowska", "AA02", new(6127, "Hufiec Ziemi Cieszyńskiej", "cieszyn@zhp.pl"), new(5967, "Chorągiew Śląska", "choragiew@dolnoslaska.zhp.pl")),
            new("Jan", "Kowalski", "AB123", new(5967, "Chorągiew Śląska", "choragiew@dolnoslaska.zhp.pl"), new(5967, "Chorągiew Śląska", "choragiew@dolnoslaska.zhp.pl")),
        });
    }

    [Fact]
    public async Task DuplicateMail_TakesFirst()
    {
        httpHandler.ResponseBody["/sfhmembersfortrainig"] = """
        [
            {
        	    "memberId": "AB123",
        	    "personId": 111,
        	    "firstName": "Jan",
        	    "lastName": "Kowalski",
        	    "birthdate": -446086800,
                "memberRoles": null,
                "hufiecId": null,
                "choragiewId": 5967
            }
        ]
        """;
        var subject = BuildSubject();

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.Supervisor.Email.Should().Be("choragiew@dolnoslaska.zhp.pl");
    }

    [Fact]
    public async Task NullMail_SetsFallback()
    {
        httpHandler.ResponseBody["/sfhmembersfortrainig"] = """
        [
            {
        	    "memberId": "BD1",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
                "memberRoles": null,
                "hufiecId": 20006,
                "choragiewId": 5967
            }
        ]
        """;
        var subject = BuildSubject("fallback@zhp.pl");

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.Supervisor.Email.Should().Be("fallback@zhp.pl");
    }

    [Fact]
    public async Task NullPrimaryMail_SetsSecondaryMail()
    {
        httpHandler.ResponseBody["/sfhmembersfortrainig"] = """
        [
            {
        	    "memberId": "BD1",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
                "memberRoles": null,
                "hufiecId": 1416,
                "choragiewId": 5967
            }
        ]
        """;
        var subject = BuildSubject("fallback@zhp.pl");

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.Supervisor.Email.Should().Be("chelm@zhp.pl");
    }

    [Fact]
    public async Task NullMailNullFallback_DoesntReturnItem()
    {
        httpHandler.ResponseBody["/sfhmembersfortrainig"] = """
        [
            {
        	    "memberId": "BD1",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
                "memberRoles": null,
                "hufiecId": 20006,
                "choragiewId": 5967
            },
            {
        	    "memberId": "BD2",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
                "memberRoles": null,
                "hufiecId": 2657,
                "choragiewId": 5967
            }
        ]
        """;
        var subject = BuildSubject(null);

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.Supervisor.Email.Should().Be("radomsko@zhp.pl");
    }

    [Fact]
    public async Task NullUnit_DoesntReturnItem()
    {
        httpHandler.ResponseBody["/sfhmembersfortrainig"] = """
        [
            {
            	"memberId": "AA01",
            	"personId": 12345,
            	"firstName": "Jan",
            	"lastName": "Kowalski",
            	"birthdate": 1408744800,
                "memberRoles": "drużynowy",
                "hufiecId": null,
                "choragiewId": null
            },
            {
            	"memberId": "AA02",
            	"personId": 54321,
            	"firstName": "Anna",
            	"lastName": "Malinowska",
            	"birthdate": 1374962400,
                "memberRoles": "członek zespołu promocji i informacji hufca",
                "hufiecId": 6127,
                "choragiewId": 5967
            }
        ]
        """;
        var subject = BuildSubject(null);

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.MembershipNumber.Should().Be("AA02");
    }
}
