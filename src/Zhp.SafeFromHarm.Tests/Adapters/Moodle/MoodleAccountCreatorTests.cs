using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Func.Adapters.Moodle;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;

namespace Zhp.SafeFromHarm.Tests.Adapters.Moodle;

public class MoodleAccountCreatorTests
{
    private readonly MoodleAccountCreator subject;
    private readonly MoodleTestHttpHandler handler = new();

    public MoodleAccountCreatorTests()
    {
        subject = new MoodleAccountCreator(
            new(
                new(handler) { BaseAddress = new("https://example.zhp.pl") },
                Options.Create(new MoodleOptions())),
            Options.Create(new SafeFromHarmOptions { MoodleAccountMailFakeDomain = "example.zhp.pl"}));
    }

    [Fact]
    public async Task AccountCreated_ValidResult()
    {
        handler.Scenario = "NewUser";

        var result = await subject.CreateAccount(new("Jan", "Kowalski", "AAA01"), "TajneHaslo");

        result.Should().Be(Domain.Model.AccountCreationResult.ResultType.Success);
    }

    [Fact]
    public async Task AccountAlreadyExisting_ValidResult()
    {
        handler.Scenario = "AlreadyExists";

        var result = await subject.CreateAccount(new("Jan", "Kowalski", "AAA01"), "TajneHaslo");

        result.Should().Be(Domain.Model.AccountCreationResult.ResultType.MemberAlreadyHasMoodle);
    }
}
