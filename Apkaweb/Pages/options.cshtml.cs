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
            await HttpContext.SignOutAsync(); // Sign out the user
            return RedirectToPage("/Index"); // Redirect to the home page after logout
        }

        public IActionResult OnPostShowMessagesAsync() => RedirectToPage("/ShowMessages"); // Redirect to ShowMessages page

        public IActionResult OnPostPermissionsAsync() => RedirectToPage("/Permissions"); // Redirect to Permissions page

        public IActionResult OnPostUserPageAsync() => RedirectToPage("/User"); // Redirect to Permissions page
    }
}