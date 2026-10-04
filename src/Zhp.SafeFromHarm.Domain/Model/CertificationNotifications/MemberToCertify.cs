namespace Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

/// <summary>
/// Informacje o członku ZHP, który powinien być certyfikowany
/// </summary>
/// <param name="Supervisor">Jednostka bezpośrednio nadzorująca certyfikację (hufiec, chorągiew lub GK)</param>
/// <param name="Department">Jednostka, w której działa pełnomocnik SFH - Chorągiew lub GK-a</param>
/// <param name="AllocationUnitName">Jednoska, do której członek ma bezpośredni przydział</param>
/// <param name="CertificateValidUntil">Data ważności certyfikatu (włącznie) albo null, jeśli członek nie ma ważnego certyfikatu</param>
public record MemberToCertify(string FirstName, string LastName, string MembershipNumber, Unit Supervisor, Unit Department, string AllocationUnitName, DateOnly? CertificateValidUntil)
{
    public bool IsCertified => CertificateValidUntil is not null;
}
