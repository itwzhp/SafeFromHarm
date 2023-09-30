using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Services;

namespace Zhp.SafeFromHarm.Func.Functions
{
    public class FindMissingRequiredCertifications
    {
        private readonly ILogger _logger;
        private readonly MissingCertificationsNotifier notifier;

        public FindMissingRequiredCertifications(ILoggerFactory loggerFactory, MissingCertificationsNotifier notifier)
        {
            _logger = loggerFactory.CreateLogger<FindMissingRequiredCertifications>();
            this.notifier = notifier;
        }

        [Function("FindMissingRequiredCertifications")]
        public async Task Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Starting FindMissingRequiredCertifications");

            await notifier.SendNotificationsOnMissingCertificates(req.FunctionContext.CancellationToken);

            _logger.LogInformation("FindMissingRequiredCertifications Finished.");
        }
    }
}
