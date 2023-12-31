﻿using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyRequiredMembersFetcher : IRequiredMembersFetcher
{
    public IAsyncEnumerable<MemberToCertify> GetMembersRequiredToCertify()
        => new MemberToCertify[]
        {
            new("Jan", "Kowalski", "AA01", "hufiec@zhp.example.com", "Hufiec"),
            new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec"),
            new("Tomasz", "Innyhufiec", "AB01", "drugihufiec@zhp.example.com", "Drugi Hufiec"),
            new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com", "Hufiec"),
        }.ToAsyncEnumerable();
}
