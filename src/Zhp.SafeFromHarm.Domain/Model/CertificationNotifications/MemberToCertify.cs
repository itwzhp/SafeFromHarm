namespace Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

/// <summary>
/// Informacje o członku ZHP, który powinien być certyfikowany
/// </summary>
/// <param name="Supervisor">Jednostka bezpośrednio nadzorująca certyfikację (hufiec, chorągiew lub GK)</param>
/// <param name="Department">Jednostka, w której działa pełnomocnik SFH - Chorągiew lub GK-a</param>
/// <param name="Allocation">Jednoska, do której członek ma bezpośredni przydział</param>
public record MemberToCertify(string FirstName, string LastName, string MembershipNumber, Unit Supervisor, Unit Department, string AllocationUnitName);