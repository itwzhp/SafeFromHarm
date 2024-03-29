﻿using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;
using Zhp.SafeFromHarm.Func.Adapters.Moodle;

namespace Zhp.SafeFromHarm.Tests.Adapters.Moodle;

public class MoodleCertifiedMembersFetcherTests
{
    private readonly ICertifiedMembersFetcher subject = new MoodleCertifiedMembersFetcher(MoodleTestFactory.MoodleClient, MoodleTestFactory.MoodleOptions);

    [Fact]
    public async Task GetCertifiedMembers_ReturnsProperValues()
    {
        var result = subject.GetCertifiedMembers();

        (await result.ToListAsync()).Should().BeEquivalentTo(new Certification[]
        {
            new("jan.kowalski@zhp.example.com", new(2023, 8, 29)),
            new("katarzyna.nazwisko@zhp.example.com", new(2023, 9, 8)),
            new("anna.anonimowa@zhp.example.com", new(2023, 9, 2))
        });
    }
}
