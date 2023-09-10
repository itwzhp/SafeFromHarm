namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

internal record User(
    int Id,
    string Email,
    CustomField[] CustomFields);

internal record CustomField(string ShortName, string Value);

