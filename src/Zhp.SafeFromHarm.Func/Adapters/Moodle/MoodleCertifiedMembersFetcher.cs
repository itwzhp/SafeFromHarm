using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle;

internal class MoodleCertifiedMembersFetcher : ICertifiedMembersFetcher
{
    private readonly MoodleClient client;
    private readonly MoodleOptions options;

    public MoodleCertifiedMembersFetcher(MoodleClient client, IOptions<MoodleOptions> options)
    {
        this.client = client;
        this.options = options.Value;
    }

    public async IAsyncEnumerable<CertifiedMember> GetCertifiedMembers()
    {
        var response = await client.CallMoodle<GradeReport>(
            MoodleFunctions.gradereport_user_get_grade_items,
            new Dictionary<string, string> { ["courseid"] = options.SfhCourseId.ToString() });

        yield break;
    }
}
