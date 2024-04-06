using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Apkaweb.Pages
{
    [Authorize] // autoryzacja
    public class OptionsModel : PageModel
    {
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostShowMessagesAsync() => RedirectToPage("/ShowMessages");

        public IActionResult OnPostPermissionsAsync() => RedirectToPage("/Permissions");

        public IActionResult OnPostUserPageAsync() => RedirectToPage("/User");
    }
}
