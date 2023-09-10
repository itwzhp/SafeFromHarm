using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain.Ports;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle;

internal class MoodleEmailMembershipNumberMapper : IEmailMembershipNumberMapper
{
    private readonly MoodleClient client;
    private readonly MoodleOptions options;
    private IReadOnlyDictionary<string, string?>? numbersByMail;

    public MoodleEmailMembershipNumberMapper(MoodleClient client, IOptions<MoodleOptions> options)
    {
        this.client = client;
        this.options = options.Value;
    }

    public async ValueTask<string?> GetMembershipNumberForEmail(string email)
    {
        numbersByMail ??= (await client.CallMoodle<User[]>(MoodleFunctions.core_enrol_get_enrolled_users, ("courseid", options.SfhCourseId.ToString())))
            .ToDictionary(u => u.Email, u => u.CustomFields.FirstOrDefault(f => f.ShortName == "numer_ewidencyjny")?.Value);

        return numbersByMail.GetValueOrDefault(email);
    }
}
