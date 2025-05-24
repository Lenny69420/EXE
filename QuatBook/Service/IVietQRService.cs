namespace QuatBook.Service
{
    public interface IVietQRService
    {
        Task<string> GenerateQRCodeAsync(double? amount, string orderId, string addInfo);
    }
}