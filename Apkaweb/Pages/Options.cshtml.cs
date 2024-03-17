using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Apkaweb.Pages
{
    public class OptionsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public OptionsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string OptionsName { get; set; }

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

        public IActionResult OnPostNewMessage()
        {
            // Handle logout action here
            return RedirectToPage("/NewMessage");
        }
    }
}

