using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain.Services;

namespace Zhp.SafeFromHarm.Func.Functions;

public class FindMissingRequiredCertifications(ILogger<FindMissingRequiredCertifications> logger, MissingCertificationsNotifier notifier)
{
    [Function("FindMissingRequiredCertifications")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        logger.LogInformation("Starting FindMissingRequiredCertifications");

        var body = await JsonSerializer.DeserializeAsync<TriggerContract>(req.Body);
        if (string.IsNullOrWhiteSpace(body?.RecipientFilter))
            return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        var onlySendToEmail = body.RecipientFilter == "*"
            ? null
            : body.RecipientFilter;

        await notifier.SendNotificationsOnMissingCertificates(onlySendToEmail, req.FunctionContext.CancellationToken);

        logger.LogInformation("FindMissingRequiredCertifications Finished.");
        return req.CreateResponse();
    }

    [Function("FindMissingRequiredCertificationsSchedule")]
    public async Task RunSchedule([TimerTrigger("0 35 2 28 * *")] TimerInfo info, FunctionContext context)
    {
        logger.LogInformation("Starting FindMissingRequiredCertificationsSchedule");

        await notifier.SendNotificationsOnMissingCertificates(null, context.CancellationToken);

        logger.LogInformation("FindMissingRequiredCertificationsSchedule Finished.");
    }

    private record TriggerContract(string RecipientFilter);
}
