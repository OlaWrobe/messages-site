using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Apkaweb.Pages
{ 
    [Authorize] // Require authentication to access this page

    public class optionsModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Index"); // Redirect to the home page after logout
        }

    }
    /* public class optionsModel : PageModel
     {
         private readonly IConfiguration _configuration;

         public optionsModel(IConfiguration configuration)
         {
             _configuration = configuration;
         }

         [BindProperty]
         public string optionsName { get; set; }

         public IActionResult OnPostLogout()
         {

             // Handle joining chat action here
             return RedirectToPage("/Index");
         }

         public IActionResult OnPostOptions()
         {
             // Handle logout action here
             return RedirectToPage("/Options");
         }
     }*/
}
