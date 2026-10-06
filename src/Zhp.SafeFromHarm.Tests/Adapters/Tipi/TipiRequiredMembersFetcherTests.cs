using Microsoft.Extensions.Options;
using System.Text.Json;
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
        httpHandler.ResponseBody["/sfh/orgunit"] = """
        [
            {
                "orgunitId": 2657,
                "name": "Hufiec Radomsko",
                "email": "radomsko@zhp.pl"
            },
            {
                "orgunitId": 6127,
                "name": "Hufiec Ziemi Cieszyńskiej",
                "email": "cieszyn@zhp.pl"
            },
            {
                "orgunitId": 5967,
                "name": "Chorągiew Śląska",
                "email": "choragiew@dolnoslaska.zhp.pl"
            },
            {
                "orgunitId": 20006,
                "name": "Hufiec ZHP Powiatu Milickiego",
                "email": null
            },
            {
                "orgunitId": 2031,
                "name": "Chorągiew Łódzka",
                "email": "lodzka@zhp.pl"
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
        httpHandler.ResponseBody["/sfh/members-for-training"] = "[]";
        var subject = BuildSubject();

        await subject.Awaiting(s => s.GetMembersRequiredToCertify().ToListAsync())
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SomeResults_MapsProperly()
    {
        httpHandler.ResponseBody["/sfh/members-for-training"] = """
        [
            {
            	"memberId": "AA01",
            	"personId": 12345,
            	"firstName": "Jan",
            	"lastName": "Kowalski",
            	"birthdate": 1408744800,
                "allocationUnitName": "10 Jakaś Wodna Drużyna Harcerska",
                "allocationUnitId": 1111,
                "memberRoles": "drużynowy",
                "hufiecId": 2657,
                "choragiewId": 2031,
                "certificateValidUntil": "2027-10-04"
            },
            {
            	"memberId": "AA02",
            	"personId": 54321,
            	"firstName": "Anna",
            	"lastName": "Malinowska",
            	"birthdate": 1374962400,
                "allocationUnitName": "Hufiec Ziemi Cieszyńskiej",
                "allocationUnitId": 6127,
                "memberRoles": "członek zespołu promocji i informacji hufca",
                "hufiecId": 6127,
                "choragiewId": 5967,
                "certificateValidUntil": null
            },
            {
        	    "memberId": "AB123",
        	    "personId": 111,
        	    "firstName": "Jan",
        	    "lastName": "Kowalski",
        	    "birthdate": -446086800,
                "allocationUnitName": "Chorągiew Śląska",
                "allocationUnitId": 5967,
                "memberRoles": null,
                "hufiecId": null,
                "choragiewId": 5967,
                "certificateValidUntil": null
            }
        ]
        """;
        var subject = BuildSubject();

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync(TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new MemberToCertify[]
        {
            new("Jan",
                "Kowalski",
                "AA01",
                new(2657, "Hufiec Radomsko", "radomsko@zhp.pl"),
                new(2031, "Chorągiew Łódzka", "lodzka@zhp.pl"),
                "10 Jakaś Wodna Drużyna Harcerska",
                new(2027, 10, 4)),
            new("Anna",
                "Malinowska",
                "AA02",
                new(6127, "Hufiec Ziemi Cieszyńskiej", "cieszyn@zhp.pl"),
                new(5967, "Chorągiew Śląska", "choragiew@dolnoslaska.zhp.pl"),
                "Hufiec Ziemi Cieszyńskiej",
                null),
            new("Jan",
                "Kowalski",
                "AB123",
                new(5967, "Chorągiew Śląska", "choragiew@dolnoslaska.zhp.pl"),
                new(5967, "Chorągiew Śląska", "choragiew@dolnoslaska.zhp.pl"),
                "Chorągiew Śląska",
                null)
        });
    }

    [Fact]
    public async Task MissingCertificateValidUntil_Exception()
    {
        httpHandler.ResponseBody["/sfh/members-for-training"] = """
        [
            {
                "memberId": "AA01",
                "firstName": "Jan",
                "lastName": "Kowalski",
                "allocationUnitName": "10 Jakaś Wodna Drużyna Harcerska",
                "hufiecId": 2657,
                "choragiewId": 2031
            }
        ]
        """;
        var subject = BuildSubject();

        await subject.Awaiting(s => s.GetMembersRequiredToCertify().ToListAsync())
            .Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task NullMail_SetsFallback()
    {
        httpHandler.ResponseBody["/sfh/members-for-training"] = """
        [
            {
        	    "memberId": "BD1",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
                "allocationUnitName": "Hufiec Ziemi Cieszyńskiej",
                "allocationUnitId": 6127,
                "memberRoles": null,
                "hufiecId": 20006,
                "choragiewId": 5967,
                "certificateValidUntil": null
            }
        ]
        """;
        var subject = BuildSubject("fallback@zhp.pl");

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync(TestContext.Current.CancellationToken);

        result.Should().ContainSingle().Which.Supervisor.Email.Should().Be("fallback@zhp.pl");
    }

    [Fact]
    public async Task NullMailNullFallback_DoesntReturnItem()
    {
        httpHandler.ResponseBody["/sfh/members-for-training"] = """
        [
            {
        	    "memberId": "BD1",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
                "allocationUnitName": "Hufiec Ziemi Cieszyńskiej",
                "allocationUnitId": 6127,
                "memberRoles": null,
                "hufiecId": 20006,
                "choragiewId": 5967,
                "certificateValidUntil": null
            },
            {
        	    "memberId": "BD2",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
                "allocationUnitName": "Hufiec Ziemi Cieszyńskiej",
                "allocationUnitId": 6127,
                "memberRoles": null,
                "hufiecId": 2657,
                "choragiewId": 5967,
                "certificateValidUntil": null
            }
        ]
        """;
        var subject = BuildSubject(null);

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync(TestContext.Current.CancellationToken);

        result.Should().ContainSingle().Which.Supervisor.Email.Should().Be("radomsko@zhp.pl");
    }

    [Fact]
    public async Task NullUnit_DoesntReturnItem()
    {
        httpHandler.ResponseBody["/sfh/members-for-training"] = """
        [
            {
            	"memberId": "AA01",
            	"personId": 12345,
            	"firstName": "Jan",
            	"lastName": "Kowalski",
            	"birthdate": 1408744800,
                "allocationUnitName": "Hufiec Ziemi Cieszyńskiej",
                "allocationUnitId": 6127,
                "memberRoles": "drużynowy",
                "hufiecId": null,
                "choragiewId": null,
                "certificateValidUntil": null
            },
            {
            	"memberId": "AA02",
            	"personId": 54321,
            	"firstName": "Anna",
            	"lastName": "Malinowska",
            	"birthdate": 1374962400,
                "allocationUnitName": "Hufiec Ziemi Cieszyńskiej",
                "allocationUnitId": 6127,
                "memberRoles": "członek zespołu promocji i informacji hufca",
                "hufiecId": 6127,
                "choragiewId": 5967,
                "certificateValidUntil": null
            },
            {
            	"memberId": "AA03",
            	"personId": 12345,
            	"firstName": "Jan",
            	"lastName": "Kowalski2",
            	"birthdate": 1408744800,
                "allocationUnitName": null,
                "allocationUnitId": null,
                "memberRoles": "drużynowy",
                "hufiecId": null,
                "choragiewId": null,
                "certificateValidUntil": null
            }
        ]
        """;
        var subject = BuildSubject(null);

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync(TestContext.Current.CancellationToken);

        result.Should().ContainSingle().Which.MembershipNumber.Should().Be("AA02");
    }
}
