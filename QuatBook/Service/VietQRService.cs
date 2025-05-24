using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace QuatBook.Service
{
    public class VietQRService : IVietQRService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _accountNo;
        private readonly string _accountName;
        private readonly int _acqId;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.vietqr.io/v2/generate";

        public VietQRService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _accountNo = _configuration["VietQR:AccountNo"];
            _accountName = _configuration["VietQR:AccountName"];
            _acqId = int.Parse(_configuration["VietQR:AcqId"]);
            _clientId = _configuration["VietQR:ClientId"];
            _apiKey = _configuration["VietQR:ApiKey"];
        }

        public async Task<string> GenerateQRCodeAsync(double? amount, string orderId, string addInfo)
        {
            // Validate amount
            if (!amount.HasValue || amount.Value <= 0)
            {
                throw new ArgumentException("Amount must be a positive value.", nameof(amount));
            }

            var requestBody = new
            {
                accountNo = _accountNo,
                accountName = _accountName,
                acqId = _acqId,
                amount = (int)amount.Value, // Cast to int, assuming VietQR expects VND as an integer
                addInfo = addInfo ?? $"Thanh toan don hang {orderId}",
                format = "text" // Returns QR code as text; can use "compact" for image
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("x-client-id", _clientId);
            request.Headers.Add("x-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode(); // Throws if HTTP status isn’t 2xx

            var responseContent = await response.Content.ReadAsStringAsync();
            var vietQrResponse = JsonConvert.DeserializeObject<VietQRResponse>(responseContent);

            if (vietQrResponse == null || vietQrResponse.Data == null || string.IsNullOrEmpty(vietQrResponse.Data.QrDataURL))
            {
                throw new Exception($"Invalid VietQR response: {responseContent}");
            }

            if (vietQrResponse.Code != "00")
            {
                throw new Exception($"Failed to generate QR code: {vietQrResponse.Desc}");
            }

            return vietQrResponse.Data.QrDataURL;
        }
    }

    public class VietQRResponse
    {
        public string Code { get; set; }
        public string Desc { get; set; }
        public VietQRData Data { get; set; }
    }

    public class VietQRData
    {
        public string QrCode { get; set; }
        public string QrDataURL { get; set; }
    }
}