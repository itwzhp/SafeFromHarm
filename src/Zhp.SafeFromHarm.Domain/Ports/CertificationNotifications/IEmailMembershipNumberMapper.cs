namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface IEmailMembershipNumberMapper
{
    ValueTask<string?> GetMembershipNumberForEmail(string email);
}
