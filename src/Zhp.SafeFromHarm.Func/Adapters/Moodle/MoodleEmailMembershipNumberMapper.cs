using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain.Ports;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle;

internal class MoodleEmailMembershipNumberMapper(MoodleClient client, IOptions<MoodleOptions> options) : IEmailMembershipNumberMapper
{
    private readonly MoodleOptions options = options.Value;
    private IReadOnlyDictionary<string, string?>? numbersByMail;

    public async ValueTask<string?> GetMembershipNumberForEmail(string email)
    {
        numbersByMail ??= await BuildMap();

        return numbersByMail.GetValueOrDefault(email);
    }

    private async Task<IReadOnlyDictionary<string, string?>> BuildMap()
    {
        var result = await client.CallMoodle<User[]>(MoodleFunctions.core_enrol_get_enrolled_users, ("courseid", options.SfhCourseId.ToString()));
        return result.ToDictionary(
            u => u.Email,
            u => u.CustomFields?.FirstOrDefault(f => f.ShortName == "numer_ewidencyjny")?.Value);
    }
}
