using System.ComponentModel.DataAnnotations;

namespace QuatBook.Dto
{
    public class ProfileDTO
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Username must be between 5 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [MaxLength(10, ErrorMessage = "Phone number must be 10 characters")]
        [RegularExpression(@"0[9875]\d{8}", ErrorMessage = "Phone number must be numeric VN")]
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        public int RoleId { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public bool? Gender { get; set; }  // Male = true, Female = false

        [Required(ErrorMessage = "Birth date is required.")]
        [DataType(DataType.Date)]
        public DateOnly? Birth { get; set; }

        public string? Image { get; set; } // Không có [Required], vì ảnh là tùy chọn
    }
}