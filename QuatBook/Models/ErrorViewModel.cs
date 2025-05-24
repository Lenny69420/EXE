namespace QuatBook.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        //ko co 
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
