﻿namespace Zhp.SafeFromHarm.Domain;

public class SafeFromHarmOptions
{
    public int CertificateExpiryDays { get; init; } = 365 * 3 + 1;
}