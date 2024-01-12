using System.Security.Cryptography;
using Zhp.SafeFromHarm.Domain.Helpers;

namespace Zhp.SafeFromHarm.Tests.Services;

public class PasswordGeneratorTests
{
    private const int iterations = 1000;
    private readonly PasswordGenerator subject = new(RandomNumberGenerator.Create());

    [Fact]
    public void PasswordShouldContainSpecialChar()
    {
        for(int i = 0; i < iterations; i++)
        {
            var password = subject.GeneratePassword();
            password.Should().ContainAny("*", "_", "-", "+", "=");
        }
    }

    [Fact]
    public void PasswordShouldContainDigit()
    {
        for (int i = 0; i < iterations; i++)
        {
            var password = subject.GeneratePassword();
            password.Any(char.IsDigit).Should().BeTrue();
        }
    }

    [Fact]
    public void PasswordShouldContainLowercase()
    {
        for (int i = 0; i < iterations; i++)
        {
            var password = subject.GeneratePassword();
            password.Any(char.IsLower).Should().BeTrue();
        }
    }

    [Fact]
    public void PasswordShouldContainUppercase()
    {
        for (int i = 0; i < iterations; i++)
        {
            var password = subject.GeneratePassword();
            password.Any(char.IsUpper).Should().BeTrue();
        }
    }
}
