using Zhp.SafeFromHarm.Domain.Model;

namespace Zhp.SafeFromHarm.Tests.Model;

public class MemberTests
{
    [Theory]
    [InlineData("Jan", "jan")]
    [InlineData("Jan   ", "   jan")]
    [InlineData("Jan", "jAN")]
    public void Equality_Firstname_Equals(string a, string b)
    {
        new Member(a, "Kowalski", "AAA01").Should().Be(new Member(b, "Kowalski", "AAA01"));
    }

    [Theory]
    [InlineData("Jan", "Jań")]
    [InlineData("Jan", "Jan Kanty")]
    public void Equality_Firstname_NotEquals(string a, string b)
    {
        new Member(a, "Kowalski", "AAA01").Should().NotBe(new Member(b, "Kowalski", "AAA01"));
    }

    [Theory]
    [InlineData("Kowalski", "kowalski")]
    [InlineData("Kowalski   ", "   kowalski")]
    [InlineData("Kowalski", "  koWaLski")]
    public void Equality_Lastname_Equals(string a, string b)
    {
        new Member("Jan", a, "AAA01").Should().Be(new Member("Jan", b, "AAA01"));
    }

    [Theory]
    [InlineData("Kowalski", "Nowak")]
    [InlineData("Kowalski", "Kowalski-Nowak")]
    public void Equality_Lastname_NotEquals(string a, string b)
    {
        new Member(a, "Kowalski", "AAA01").Should().NotBe(new Member(b, "Kowalski", "AAA01"));
    }

    [Theory]
    [InlineData("AA1234", "  AA1234  ")]
    [InlineData("AA111", "aa111")]
    [InlineData("AA-111", "aa111")]
    [InlineData("AA 111", "aa111")]
    public void Equality_MembershipNumber_Equals(string a, string b)
    {
        new Member("Jan", "Kowalski", a).Should().Be(new Member("Jan", "Kowalski", b));
    }

    [Theory]
    [InlineData("AA1234", "AA12345")]
    [InlineData("AA1234", "AA1235")]
    [InlineData("OO123", "00123")]
    public void Equality_MembershipNumber_NotEquals(string a, string b)
    {
        new Member("Jan", "Kowalski", a).Should().NotBe(new Member("Jan", "Kowalski", b));
    }
}
