using System;
using System.Net.Http;
using System.Threading.Tasks;
using Shemy.Request;

namespace Shemy.CircuitBreaker.Abstractions
{
    public interface ICircuitBreakerEngine
    {
        Task<HttpResponseMessage> ExecuteAsync(AnshanHttpRequestMessage request,
            Func<Task<HttpResponseMessage>> next);
    }
}