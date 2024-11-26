using System.ComponentModel.DataAnnotations;

namespace Common.Web.Model
{
    public class LoginVm
    {
        [Required]
        [DataType(DataType.Text)]
        public string LoginId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string Domain { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
