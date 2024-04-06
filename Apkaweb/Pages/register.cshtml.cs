using Apkaweb.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
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
        public List<SecurityQuestion> SecurityQuestions { get; set; }

        // Handle GET requests
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT id, question FROM questions";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        SecurityQuestions = new List<SecurityQuestion>();

                        while (await reader.ReadAsync())
                        {
                            SecurityQuestions.Add(new SecurityQuestion
                            {
                                Id = reader.GetInt32("id"),
                                QuestionText = reader.GetString("question")
                            });
                        }
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                return RedirectToPage("/Error");
            }
        }

        public async Task<IActionResult> OnPostAsync(string username, string password, string answer, int selectedQuestionId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Username", username);
                    long existingUserCount = (long)await checkCommand.ExecuteScalarAsync();

                    if (existingUserCount > 0)
                        return RedirectToPage("/Register");
                }

                string insertQuery = "INSERT INTO Users (Username, Password,FailedLoginAttempts,IsBlockEnabled,NumberOfAttempts,QuestionId,answer) VALUES (@Username, @Password,0,0,0,@SelectedQuestionId,@Answer)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Answer", answer);
                    command.Parameters.AddWithValue("@SelectedQuestionId", selectedQuestionId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? RedirectToPage("/Index") : RedirectToPage("/Register");
                }
            }
        }
    }
}
