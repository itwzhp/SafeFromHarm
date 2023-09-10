using System.Text.Json.Serialization;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

internal record GradeReport(UserGrade[] UserGrades);

internal record UserGrade(
    int UserId, 
    GradeItem[] GradeItems);

internal record GradeItem(
    string ItemType,
    float? GradeRaw,
    long? GradeDateGraded)
{
    [JsonIgnore]
    public DateTime? GradeDateTime => GradeDateGraded.HasValue ? DateTimeOffset.FromUnixTimeSeconds(GradeDateGraded.Value).Date : null;
}
