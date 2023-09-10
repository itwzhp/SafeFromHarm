using System.Text.Json.Serialization;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

internal record GradeReport([property: JsonPropertyName("usergrades")] UserGrade[] UserGrades);

internal record UserGrade(
    [property: JsonPropertyName("userid")] int UserId, 
    [property: JsonPropertyName("gradeitems")] GradeItem[] GradeItems);

internal record GradeItem(
    [property: JsonPropertyName("itemtype")] string ItemType,
    [property: JsonPropertyName("graderaw")] float GradeRaw,
    [property: JsonPropertyName("gradedategraded")] int GradeDateGraded)
{
    [JsonIgnore]
    public DateTime GradeDateTime => new();
}
