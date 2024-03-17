using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace Apkaweb.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public RegisterModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnPostAsync(string username, string password)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if the username already exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Username", username);
                    long existingUserCount = (long)await checkCommand.ExecuteScalarAsync();

                    if (existingUserCount > 0)
                    {
                        // Username already exists, redirect back to the registration page
                        // You can also display an error message here if you want
                        return RedirectToPage("/Register");
                    }
                }

                // Insert the new user into the database
                string insertQuery = "INSERT INTO Users (Username, Password) VALUES (@Username, @Password)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        // User successfully registered, redirect to the login page
                        return RedirectToPage("/Index"); // Redirect to the login page
                    }
                    else
                    {
                        // Failed to register the user, handle accordingly (e.g., show error message)
                        // You can redirect back to the registration page or display an error message
                        return RedirectToPage("/Register"); // Redirect back to the registration page
                    }
                }
            }
        }
    }
}
