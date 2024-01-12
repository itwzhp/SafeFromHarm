using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain.Services;

namespace Zhp.SafeFromHarm.Func.Functions;

public class GenerateReports(ILogger<GenerateReports> logger, ReportGenerator reportGenerator)
{
    [Function("GenerateReports")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        logger.LogInformation($"Starting function GenerateReports...");

        var body = await JsonSerializer.DeserializeAsync<TriggerContract>(req.Body);
        if (string.IsNullOrWhiteSpace(body?.RecipientFilter))
            return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        var onlySendToEmail = body.RecipientFilter == "*"
            ? null
            : body.RecipientFilter;

        await reportGenerator.SendReports(onlySendToEmail, req.FunctionContext.CancellationToken);

        logger.LogInformation($"Function GenerateReports finished.");
        return req.CreateResponse();
    }

    [Function("GenerateReportsSchedule")]
    public async Task RunSchedule([TimerTrigger("0 43 2 * * 1")] TimerInfo myTimer, FunctionContext context)
    {
        logger.LogInformation($"Starting function GenerateReportsSchedule...");

        await reportGenerator.SendReports(null, context.CancellationToken);

        logger.LogInformation($"Function GenerateReportsSchedule finished.");
    }

    private record TriggerContract(string RecipientFilter);
}
