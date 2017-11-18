using System.ComponentModel.DataAnnotations;

namespace AuthorizationSample.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "用户名")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "密码")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "记住我？")]
        public bool RememberMe { get; set; }
    }
}
