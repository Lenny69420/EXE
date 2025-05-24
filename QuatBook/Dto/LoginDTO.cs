using System.ComponentModel.DataAnnotations;

namespace QuatBook.Dto
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Chưa nhập tên đăng nhập")]
        [MaxLength(20, ErrorMessage = "Tối đa 20 kí tự")]
        public string Username { get; set; }
//hello
        [Required(ErrorMessage = "Chưa nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
