using System.Security.Cryptography;
using System.Text;

namespace Zhp.SafeFromHarm.Domain.Helpers;

public class PasswordGenerator(RandomNumberGenerator generator)
{
    public string GeneratePassword()
    {
        const int lower = 3;
        const int upper = 2;
        const int special = 1;
        const int digit = 3;
        const int full = lower + upper + special + digit;

        Span<byte> noise = stackalloc byte[full];

        generator.GetBytes(noise);

        var password = new StringBuilder(full);

        password.Append(GetPart("abcdefghijkmnopqrstuvwxyz", noise[..lower]));
        noise = noise[lower..];

        password.Append(GetPart("ABCDEFGHJKLMNPQRSTUVWXYZ", noise[..upper]));
        noise = noise[upper..];

        password.Append(GetPart("-_=+*", noise[..special]));
        noise = noise[special..];

        password.Append(GetPart("123456789", noise));

        return password.ToString();
    }

    public static string GetPart(string sourceChars, ReadOnlySpan<byte> noise)
    {
        var part = new StringBuilder(noise.Length);

        foreach (var b in noise)
        {
            var randomValue = b % sourceChars.Length;
            part.Append(sourceChars[randomValue]);
        }

        return part.ToString();
    }
}
