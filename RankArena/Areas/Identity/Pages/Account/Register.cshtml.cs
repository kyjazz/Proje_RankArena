#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace RankArena.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
            [StringLength(30, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-30 karakter olmalıdır.")]
            [Display(Name = "Kullanıcı Adı")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "E-posta adresi zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Şifre zorunludur.")]
            [StringLength(100, ErrorMessage = "{0} en az {2}, en fazla {1} karakter olmalıdır.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Şifre")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Şifre (Tekrar)")]
            [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            // E-posta zaten var mı?
            var existingByEmail = await _userManager.FindByEmailAsync(Input.Email.Trim());
            if (existingByEmail != null)
            {
                ModelState.AddModelError(string.Empty, "Bu e-posta adresi zaten kayıtlı.");
                return Page();
            }

            var user = CreateUser();

            // Username + Email set
            await _userStore.SetUserNameAsync(user, Input.UserName.Trim(), CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email.Trim(), CancellationToken.None);

            // Projede ConfirmedAccount zorunlu değil; giriş sorunlarını azaltmak için true
            user.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Yeni kullanıcı oluşturuldu.");

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "E-posta Doğrulama",
                    $"Hesabını doğrulamak için <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya tıkla</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
            }

            return Page();
        }

        private static string TranslateIdentityError(IdentityError error)
        {
            return error.Code switch
            {
                "DuplicateUserName" => "Bu kullanıcı adı zaten kullanılıyor.",
                "DuplicateEmail" => "Bu e-posta adresi zaten kayıtlı.",
                "InvalidUserName" => "Geçersiz kullanıcı adı.",
                "InvalidEmail" => "Geçersiz e-posta adresi.",
                "PasswordTooShort" => "Şifre çok kısa.",
                "PasswordRequiresNonAlphanumeric" => "Şifre en az 1 özel karakter içermelidir.",
                "PasswordRequiresDigit" => "Şifre en az 1 rakam içermelidir.",
                "PasswordRequiresLower" => "Şifre en az 1 küçük harf içermelidir.",
                "PasswordRequiresUpper" => "Şifre en az 1 büyük harf içermelidir.",
                _ => error.Description
            };
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"'{nameof(IdentityUser)}' örneği oluşturulamıyor.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("Varsayılan UI e-posta destekli kullanıcı gerektirir.");

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}