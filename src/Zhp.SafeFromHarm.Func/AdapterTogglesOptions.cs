namespace Zhp.SafeFromHarm.Func;

internal class AdapterTogglesOptions
{
    // Account creation
    public string AccountCreator { get; init; } = string.Empty;

    public string MemberMailAccountChecker { get; init; } = string.Empty;

    public string MembersFetcher { get; init; } = string.Empty;

    public string[] AccountCreationResultPublishers { get; init; } = Array.Empty<string>();

    // Certification notifications

    public string CertifiedMembersFetcher { get; init; } = string.Empty;

    public string EmailMembershipNumberMapper { get; init; } = string.Empty;

    public string RequiredMembersFetcher { get; init; } = string.Empty;

    public string NotificationSender { get; init; } = string.Empty;
}
