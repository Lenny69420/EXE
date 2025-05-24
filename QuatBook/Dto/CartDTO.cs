namespace QuatBook.Dto
{
    public class CartDTO
    {
        public int bookId { get; set; }
        public string? Image { get; set; }
        public string BookName { get; set; } = null!;
        public double? Price { get; set; }
        public int Quantity { get; set; }
        public double Ammount => (double)(Price * Quantity);
        public DateTime AddedTime { get; set; } // Thêm trường này

        //Test
    }

    public class CartModel
    {
        public int Quantity { get; set; }
        public double Total { get; set; }

        public List<CartDTO> Items { get; set; } = new List<CartDTO>();
    }
}
