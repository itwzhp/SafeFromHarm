using System.Text;

namespace Zhp.SafeFromHarm.Domain.Model;

public record Member(string FirstName, string LastName, string MembershipNumber)
{
    public string FirstName { get; } = FirstName.Trim();
    public string LastName { get; } = LastName.Trim();
    public string MembershipNumber { get; } = CleanNumber(MembershipNumber);

    private static string CleanNumber(string number)
    {
        var sb = new StringBuilder(number.Length);
        foreach (var c in number)
        {
            if (char.IsDigit(c))
                sb.Append(c);
            else if (char.IsLetter(c))
                sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    public override string ToString() => $"{FirstName} {LastName} ({MembershipNumber})";

    public virtual bool Equals(Member? obj)
    {
        if (obj is not Member other)
            return false;

        return string.Equals(FirstName, other.FirstName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(LastName, other.LastName, StringComparison.OrdinalIgnoreCase)
            && MembershipNumber == other.MembershipNumber;
    }

    public override int GetHashCode()
        => HashCode.Combine(FirstName.GetHashCode(StringComparison.OrdinalIgnoreCase), LastName.GetHashCode(StringComparison.OrdinalIgnoreCase), MembershipNumber);
}
