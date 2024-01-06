using System.Text.Json.Serialization;

namespace Zhp.SafeFromHarm.Domain.Model.AccountCreation;

public record AccountCreationResult(Member Member, string? Password, AccountCreationResult.ResultType Result)
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResultType
    {
        Success,
        MemberNotInTipi,
        MemberHasMs365,
        MemberAlreadyHasMoodle,
        OtherError
    }
}
