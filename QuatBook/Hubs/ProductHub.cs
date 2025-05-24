using Microsoft.AspNetCore.SignalR;

namespace QuatBook.Hubs
{
    public class ProductHub : Hub
    {
        public async Task UpdateProductList()
        {
            // Gửi thông báo để client tự cập nhật danh sách
            await Clients.All.SendAsync("RefreshProducts");
        }
    }
}
