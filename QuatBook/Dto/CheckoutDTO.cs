namespace QuatBook.Dto
{
    public class CheckoutDTO
    {
        public bool giongkhachhang { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; }
    }
}
