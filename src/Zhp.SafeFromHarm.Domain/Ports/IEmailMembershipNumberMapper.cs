namespace Zhp.SafeFromHarm.Domain.Ports;

public interface IEmailMembershipNumberMapper
{
    ValueTask<string?> GetMembershipNumberForEmail(string email);
}
