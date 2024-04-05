using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Apkaweb.Pages
{
    public class LockedAccountModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string Username { get; set; }
        public string SecurityQuestion { get; set; }

        public LockedAccountModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync(string username)
        {
            Username = username;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT q.question FROM questions q JOIN Users u ON u.QuestionId = q.id WHERE u.Username = @Username";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    SecurityQuestion = (string)await command.ExecuteScalarAsync();
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string username, string password, string answer)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password AND Answer = @Answer";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Answer", answer);
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                    if (count > 0)
                    {
                        await ResetFailedLoginAttempts(connection, username);

                        var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, username),
                            new Claim(ClaimTypes.Role, "User")
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        return RedirectToPage("/Options");
                    }
                    else
                    {
                        return RedirectToPage("/Index");
                    }
                }
            }
        }

        private async Task ResetFailedLoginAttempts(MySqlConnection connection, string username)
        {
            string query = "UPDATE Users SET FailedLoginAttempts = 0 WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}