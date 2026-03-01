#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace RankArena.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ResetPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "E-posta adresi zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Şifre zorunludur.")]
            [StringLength(100, ErrorMessage = "{0} en az {2}, en fazla {1} karakter olmalıdır.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Yeni Şifre")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Yeni Şifre (Tekrar)")]
            [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
            public string ConfirmPassword { get; set; }

            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Şifre sıfırlama kodu gereklidir.");
            }

            Input = new InputModel
            {
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Güvenlik: Kullanıcı var mı yok mu belli etme
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                var message = error.Code switch
                {
                    "PasswordTooShort" => "Şifre çok kısa.",
                    "PasswordRequiresNonAlphanumeric" => "Şifre en az 1 özel karakter içermelidir.",
                    "PasswordRequiresDigit" => "Şifre en az 1 rakam içermelidir.",
                    "PasswordRequiresLower" => "Şifre en az 1 küçük harf içermelidir.",
                    "PasswordRequiresUpper" => "Şifre en az 1 büyük harf içermelidir.",
                    "InvalidToken" => "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.",
                    _ => error.Description
                };
                ModelState.AddModelError(string.Empty, message);
            }

            return Page();
        }
    }
}