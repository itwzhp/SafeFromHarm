﻿using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Func.Adapters.Tipi;

namespace Zhp.SafeFromHarm.Tests.Adapters.Tipi;

public class TipiRequiredMembersFetcherTests
{
    private readonly TestHandler httpHandler = new();

    private TipiRequiredMembersFetcher BuildSubject(string? controlTeamsChannelMail = null)
        => new(
            new(httpHandler) { BaseAddress = new("https://example.zhp.pl") },
            Options.Create(new SafeFromHarmOptions { ControlTeamsChannelMail = controlTeamsChannelMail }));

    [Fact]
    public async Task EmptyResults_Exception()
    {
        httpHandler.ResponseBody = "[]";
        var subject = BuildSubject();

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
        var subject = BuildSubject();

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().BeEquivalentTo(new MemberToCertify[]
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
        var subject = BuildSubject();

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.SupervisorEmail.Should().Be("choragiew@dolnoslaska.zhp.pl");
    }

    [Fact]
    public async Task NullMail_SetsFallback()
    {
        httpHandler.ResponseBody = """
        [
            {
        	    "memberId": "BD1",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
        	    "allocationUnitName": "Hufiec ZHP Powiatu Milickiego",
        	    "allocationUnitContactEmails": null,
        	    "memberRoles": null
            }
        ]
        """;
        var subject = BuildSubject("fallback@zhp.pl");

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.SupervisorEmail.Should().Be("fallback@zhp.pl");
    }

    [Fact]
    public async Task NullMailNullFallback_DoesntReturnItem()
    {
        httpHandler.ResponseBody = """
        [
            {
        	    "memberId": "BD1",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
        	    "allocationUnitName": "Hufiec ZHP Powiatu Milickiego",
        	    "allocationUnitContactEmails": null,
        	    "memberRoles": null
            },
            {
        	    "memberId": "BD2",
        	    "personId": 370645,
        	    "firstName": "Anna",
        	    "lastName": "Kowalska",
        	    "birthdate": 1012518000,
        	    "allocationUnitName": "Hufiec ZHP Powiatu Milickiego",
        	    "allocationUnitContactEmails": "hufiec@zhp.example.com",
        	    "memberRoles": null
            }
        ]
        """;
        var subject = BuildSubject(null);

        var result = await subject.GetMembersRequiredToCertify().ToArrayAsync();

        result.Should().ContainSingle().Which.SupervisorEmail.Should().Be("hufiec@zhp.example.com");
    }
}
