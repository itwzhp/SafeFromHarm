using Azure.Identity;
using Zhp.SafeFromHarm.Func.Adapters.GraphApi;

namespace Zhp.SafeFromHarm.Tests.Adapters.GraphApi;

public class EntraIdMemberMailAccountCheckerTests
{
    private readonly EntraIdMemberMailAccountChecker subject = new(new(new InteractiveBrowserCredential(), ["https://graph.microsoft.com/.default"]));

    [Fact(Skip = "This is integration test! Don't run automatically")]
    public async Task HasEmailAccount_ExistingUser_True()
    {
        var result = await subject.HasEmailAccount("AL005047071", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact(Skip = "This is integration test! Don't run automatically")]
    public async Task HasEmailAccount_NonExistingUser_False()
    {
        var result = await subject.HasEmailAccount("AAAAABBBBBBCCCC", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact(Skip = "This is integration test! Don't run automatically")]
    public async Task HasEmailAccount_BlockedUser_False()
    {
        var result = await subject.HasEmailAccount("AL003075862", CancellationToken.None);

        result.Should().BeFalse();
    }
}
