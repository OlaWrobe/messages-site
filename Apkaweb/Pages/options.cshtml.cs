using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Apkaweb.Pages
{
    [Authorize] // Require authentication to access this page
    public class optionsModel : PageModel
    {
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the home page after logout
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostShowMessagesAsync()
        {
            // Redirect to the ShowMessages page
            return RedirectToPage("/ShowMessages");
        }

        public IActionResult OnPostPermissionsAsync()
        {
            // Redirect to the Permissions page
            return RedirectToPage("/Permissions");
        }
    }
}
