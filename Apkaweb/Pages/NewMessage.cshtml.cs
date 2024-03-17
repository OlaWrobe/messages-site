using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Apkaweb.Pages
{
    public class NewMessageModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public NewMessageModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string NewMessageName { get; set; }

        public IActionResult OnPostSendMessage()
        {
            // TO DO: zapisywanie/przechowywanie/prekazywanie wiadomoœci
            // TO DO: ? czyszczenie pola tekstowego po wys³aniu wiadomoœci
            return RedirectToPage("/NewMessage");
        }

        public IActionResult OnPostOptions()
        {
            return RedirectToPage("/Options"); // Przekierowanie do strony g³ównej po przes³aniu wiadomoœci
        }
    }
}
