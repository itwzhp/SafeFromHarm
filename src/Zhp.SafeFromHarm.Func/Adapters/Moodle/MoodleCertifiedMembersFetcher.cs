using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle;

internal class MoodleCertifiedMembersFetcher(MoodleClient client, IOptions<MoodleOptions> options) : ICertifiedMembersFetcher
{
    private readonly MoodleOptions options = options.Value;

    public async IAsyncEnumerable<CertifiedMember> GetCertifiedMembers()
    {
        var gradeRequest = client.CallMoodle<GradeReport>(
            MoodleFunctions.gradereport_user_get_grade_items,
            new() { ["courseid"] = options.SfhCourseId })
            .AsTask();

        var userRequest = client.CallMoodle<User[]>(
            MoodleFunctions.core_enrol_get_enrolled_users,
            new() { ["courseid"] = options.SfhCourseId })
            .AsTask();

        await Task.WhenAll(gradeRequest, userRequest);

        var gradeResponse = gradeRequest.Result;
        var userResponse = userRequest.Result;

        var passingMembers = gradeResponse
            .UserGrades
            .Select(g => (g.UserId, Grade: g.GradeItems.FirstOrDefault(f => f.ItemType == "course")))
            .Where(g => g.Grade is not null)
            .Where(g => g.Grade?.GradeRaw != null && g.Grade?.GradeDateTime != null)
            .Select(g => (g.UserId, g.Grade!.GradeDateTime!.Value));

        var eMailsById = userResponse.ToDictionary(u => u.Id, u => u.Email);

        foreach (var (userId, gradeDateTime) in passingMembers)
        {
            if (eMailsById.TryGetValue(userId, out var eMail))
                yield return new(eMail, DateOnly.FromDateTime(gradeDateTime));
        }
    }
}
