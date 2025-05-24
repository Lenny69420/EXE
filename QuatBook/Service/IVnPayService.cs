using QuatBook.Dto;

namespace QuatBook.Service
{
    public interface IVnPayService
    {
        // gửi thông tin thanh toán đến VNPay
        string CreatePaymentUrl(HttpContext httpContext, VnPaymentRequestModel model);

        // xử lý thông tin trả về từ VNPay
        VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
