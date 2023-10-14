using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain.Services;

namespace Zhp.SafeFromHarm.Func.Functions;

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
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Starting FindMissingRequiredCertifications");

        var body = await JsonSerializer.DeserializeAsync<TriggerContract>(req.Body);
        if (string.IsNullOrWhiteSpace(body?.RecipientFilter))
            return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        var onlySendToEmail = body.RecipientFilter == "*"
            ? null
            : body.RecipientFilter;

        await notifier.SendNotificationsOnMissingCertificates(onlySendToEmail, req.FunctionContext.CancellationToken);

        _logger.LogInformation("FindMissingRequiredCertifications Finished.");
        return req.CreateResponse();
    }

    private record TriggerContract(string RecipientFilter);
}
