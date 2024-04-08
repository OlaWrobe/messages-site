using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Apkaweb.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ChangePasswordModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string CurrentPassword { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", User.Identity.Name); // Using current user
                        command.Parameters.AddWithValue("@Password", CurrentPassword);
                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            query = "UPDATE Users SET Password = @Password WHERE Username = @Username";
                            using (var updateCommand = new MySqlCommand(query, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@Password", NewPassword);
                                updateCommand.Parameters.AddWithValue("@Username", User.Identity.Name); // Using current user
                                await updateCommand.ExecuteNonQueryAsync();
                            }

                            return RedirectToPage("/User");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Incorrect current password.");
                            return Page();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                ModelState.AddModelError(string.Empty, "An error occurred while processing your request.");
                return Page();
            }
        }
    }
}
