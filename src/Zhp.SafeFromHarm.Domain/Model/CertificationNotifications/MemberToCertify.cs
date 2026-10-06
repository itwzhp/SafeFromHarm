namespace Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

/// <summary>
/// Informacje o członku ZHP, który powinien być certyfikowany
/// </summary>
/// <param name="Supervisor">Jednostka bezpośrednio nadzorująca certyfikację (hufiec, chorągiew lub GK)</param>
/// <param name="Department">Jednostka, w której działa pełnomocnik SFH - Chorągiew lub GK-a</param>
/// <param name="AllocationUnitName">Jednoska, do której członek ma bezpośredni przydział</param>
/// <param name="CertificateValidUntil">Data ważności certyfikatu (włącznie) albo null, jeśli członek nigdy nie uzyskał certyfikatu. Data z przeszłości oznacza certyfikat wygasły.</param>
public record MemberToCertify(string FirstName, string LastName, string MembershipNumber, Unit Supervisor, Unit Department, string AllocationUnitName, DateOnly? CertificateValidUntil)
{
    /// <summary>
    /// Stan certyfikacji na dzień, na który zbudowano raport. Ustawiany wyłącznie przez <see cref="CertificationReport"/> -
    /// członek prosto z <see cref="Ports.CertificationNotifications.IRequiredMembersFetcher"/> ma jeszcze <see cref="CertificationStatus.None"/>.
    /// </summary>
    public CertificationStatus Status { get; internal init; } = CertificationStatus.None;

    public bool IsCertified => Status is CertificationStatus.Valid;
}
