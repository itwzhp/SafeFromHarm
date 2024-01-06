using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zhp.SafeFromHarm.Domain.Model.AccountCreation;
using Zhp.SafeFromHarm.Domain.Services;

namespace Zhp.SafeFromHarm.Func.Functions;

public class CreateAccounts(ILogger<CreateAccounts> logger, AccountCreator creator)
{
    [Function("CreateAccounts")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        logger.LogInformation("Starting CreateAccounts");

        var principal = req.Headers.GetValues("X-MS-CLIENT-PRINCIPAL-NAME").FirstOrDefault()
            ?? throw new Exception("Missing X-MS-CLIENT-PRINCIPAL-NAME header");

        var body = await JsonSerializer.DeserializeAsync<CreateAccountsContract>(req.Body)
            ?? throw new Exception("Null body");

        if(!body.RequestorEmail.Equals(principal, StringComparison.OrdinalIgnoreCase))
            throw new Exception("RequestorEmail does not match principal");

        var result = await creator.CreateAccounts(body.Members, body.RequestorEmail, req.FunctionContext.CancellationToken);

        logger.LogInformation("CreateAccounts Finished.");

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    private record CreateAccountsContract(Member[] Members, string RequestorEmail);
}
