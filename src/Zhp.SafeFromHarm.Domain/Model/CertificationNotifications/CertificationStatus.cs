namespace Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

/// <summary>
/// Stan certyfikacji członka ustalony na konkretny dzień. Wylicza go wyłącznie <see cref="CertificationReport"/>.
/// </summary>
public enum CertificationStatus
{
    /// <summary>Członek nie ma certyfikatu</summary>
    None,

    /// <summary>Członek ma certyfikat, ale stracił on ważność</summary>
    Expired,

    /// <summary>Członek ma ważny certyfikat</summary>
    Valid,
}
