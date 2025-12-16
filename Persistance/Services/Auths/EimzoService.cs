using Application.Interfaces.Auths;
using Application.Models.Dtos.Auths;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Auths
{
    public class EimzoService(HttpClient httpClient) : IEimzoService
    {
        private readonly HttpClient _httpClient = httpClient;
        public async Task<EimzoVerificationResult> VerifyAsync(string signedData, CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsJsonAsync(
            "/backend/auth",
            new LoginViaEimzoDto { SignedData = signedData },
            cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<EimzoVerificationResult>(cancellationToken)
                ?? throw new Exception("Invalid E-Imzo response");
        }
    }
}
