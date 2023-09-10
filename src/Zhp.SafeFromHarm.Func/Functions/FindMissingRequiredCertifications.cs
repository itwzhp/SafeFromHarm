using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Functions
{
    public class FindMissingRequiredCertifications
    {
        private readonly ILogger _logger;
        private readonly ICertifiedMembersFetcher certifiedMembersFetcher;
        private readonly IEmailMembershipNumberMapper emailNumberMapper;

        public FindMissingRequiredCertifications(ILoggerFactory loggerFactory, ICertifiedMembersFetcher certifiedMembersFetcher, IEmailMembershipNumberMapper emailNumberMapper)
        {
            _logger = loggerFactory.CreateLogger<FindMissingRequiredCertifications>();
            this.certifiedMembersFetcher = certifiedMembersFetcher;
            this.emailNumberMapper = emailNumberMapper;
        }

        [Function("FindMissingRequiredCertifications")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Starting FindMissingRequiredCertifications");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            await response.WriteStringAsync(string.Join("\n", (await certifiedMembersFetcher.GetCertifiedMembers().ToArrayAsync()).AsEnumerable()));

            _logger.LogInformation("Starting Finished.");

            return response;
        }
    }
}
