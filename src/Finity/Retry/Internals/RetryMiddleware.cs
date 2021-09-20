using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Finity.Clock;
using Finity.Extensions;
using Finity.Metric;
using Finity.Pipeline.Abstractions;
using Finity.Request;
using Finity.Retry.Configurations;
using Finity.Retry.Exceptions;
using Finity.Shared;
using Microsoft.Extensions.Options;

namespace Finity.Retry.Internals
{
    public class RetryMiddleware : IMiddleware<AnshanHttpRequestMessage, HttpResponseMessage>
    {
        private readonly IClock _clock;
        private readonly IOptionsSnapshot<RetryConfigure> _options;

        public RetryMiddleware(IClock clock, IOptionsSnapshot<RetryConfigure> options)
        {
            _clock = clock;
            _options = options;
        }

        public async Task<HttpResponseMessage> RunAsync(
            AnshanHttpRequestMessage request,
            IPipelineContext context,
            Func<Task<HttpResponseMessage>> next,
            Action<MetricValue> setMetric,
            CancellationToken cancellationToken)
        {
            var firstResponse = await ExecuteFirstTryAsync(next);
            if (!firstResponse.IsSucceed())
            {
                return await ExecuteNextTriesAsync(request, next, cancellationToken);
            }   

            //Report Metrics for the first try
            setMetric(new CounterValue());
            // Metrics.Increment(Metrics.FirstTryCount);
            return firstResponse;
        }

        private async Task<HttpResponseMessage> ExecuteFirstTryAsync(Func<Task<HttpResponseMessage>> next)
        {
            var response = await next();
            return response;
        }

        private async Task<HttpResponseMessage> ExecuteNextTriesAsync(AnshanHttpRequestMessage request,
            Func<Task<HttpResponseMessage>> next,
            CancellationToken cancellationToken)
        {
            var retryConfigure = _options.Get(request.Name);
            for (var i = 0; i < retryConfigure.RetryCount; i++)
            {
                var response = await next();
                if (response.IsSucceed())
                {
                    //Report Metrics for next tries
                    Metrics.Increment(Metrics.NextTryCount);
                    return response;
                }

                if (retryConfigure.SleepDurationRetry > TimeSpan.Zero)
                    await _clock.SleepAsync(retryConfigure.SleepDurationRetry, cancellationToken);
            }

            throw new RetryOutOfRangeException();
        }
    }
}