using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Claims;

namespace Apkaweb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if the user is already authenticated
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Options");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string login, string password)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                bool isBlocked = await IsAccountBlocked(connection, login);

                if (isBlocked)
                {
                    return RedirectToPage("/LockedAccount", new { username = login });
                }

                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", login);
                    command.Parameters.AddWithValue("@Password", password);
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                    if (count > 0)
                    {

                        await ResetFailedLoginAttempts(connection, login);


                        var claims = new[]
                        {
                    new Claim(ClaimTypes.Name, login),
                    new Claim(ClaimTypes.Role, "User")
                };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        return RedirectToPage("/Options");
                    }
                    else
                    {
                        await IncrementFailedLoginAttempts(connection, login);

                    }
                }
            }
            return Page();
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


        private async Task<bool> IsAccountBlocked(MySqlConnection connection, string username)
        {
            string query = "SELECT IsBlockEnabled, FailedLoginAttempts, NumberOfAttempts FROM Users WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        int isBlockEnabled = reader.GetInt32("IsBlockEnabled");
                        int failedLoginAttempts = reader.GetInt32("FailedLoginAttempts");
                        int numberOfAttempts = reader.GetInt32("NumberOfAttempts");
                        return isBlockEnabled == 1 && (numberOfAttempts > 0 && failedLoginAttempts >= numberOfAttempts);
                    }

                    else
                    {
                        // Handle the case where the user is not found
                        return false;
                    }
                }
            }
        }

        private async Task IncrementFailedLoginAttempts(MySqlConnection connection, string username)
        {
            string query = "UPDATE Users SET FailedLoginAttempts = FailedLoginAttempts + 1 WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
