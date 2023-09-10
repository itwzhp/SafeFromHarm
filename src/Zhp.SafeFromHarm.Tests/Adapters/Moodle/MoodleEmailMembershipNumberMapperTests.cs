using FluentAssertions;
using Zhp.SafeFromHarm.Domain.Ports;
using Zhp.SafeFromHarm.Func.Adapters.Moodle;

namespace Zhp.SafeFromHarm.Tests.Adapters.Moodle;

public class MoodleEmailMembershipNumberMapperTests
{
    private readonly IEmailMembershipNumberMapper subject = new MoodleEmailMembershipNumberMapper(MoodleTestFactory.MoodleClient, MoodleTestFactory.MoodleOptions);

    [Theory]
    [InlineData("jan.kowalski@zhp.example.com", "AA01")]
    [InlineData("katarzyna.nazwisko@zhp.example.com", "AA02")]
    [InlineData("anna.anonimowa@zhp.example.com", "AA03")]
    [InlineData("jan.nowak@zhp.example.com", "AA04")]
    [InlineData("kamil.slimak@zhp.example.com", "AA04")]
    [InlineData("tomasz.nullowy@zhp.example.com", null)]
    [InlineData("nieznaleziony@zhp.example.com", null)]
    public async Task GetMembershipNumberForEmail_ReturnProperData(string email, string? expectedResult)
    {
        var result = await subject.GetMembershipNumberForEmail(email);

        result.Should().Be(expectedResult);
    }
}
