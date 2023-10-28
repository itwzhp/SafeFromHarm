﻿namespace Zhp.SafeFromHarm.Domain.Ports;

public interface ISummarySender
{
    Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter, IReadOnlyCollection<(string Email, string UnitName)> failedRecipients);
}