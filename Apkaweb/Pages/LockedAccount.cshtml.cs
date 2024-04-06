using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Security.Claims;

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
                        DateTime succesAttemptDate = DateTime.Now;

                        string updateQuery = "UPDATE Users SET SuccesAttemptDate = @SuccesAttemptDate WHERE Username = @Username";
                        using (var updateCommand = new MySqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@SuccesAttemptDate", succesAttemptDate);
                            updateCommand.Parameters.AddWithValue("@Username", username);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
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
                        await IncrementFailedLoginAttempts(connection, username);

                        DateTime failedAttemptDate = DateTime.Now;
                        string updateQuery = "UPDATE Users SET FailedAttemptDate = @FailedAttemptDate WHERE Username = @Username";
                        using (var updateCommand = new MySqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@FailedAttemptDate", failedAttemptDate);
                            updateCommand.Parameters.AddWithValue("@Username", username);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                        return RedirectToPage("/Index");
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
        
        private async Task ResetFailedLoginAttempts(MySqlConnection connection, string username)
        {
            string query = "UPDATE Users SET PreviousAttempts = FailedLoginAttempts, FailedLoginAttempts = 0 WHERE Username = @Username"; ;
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
