using Apkaweb.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apkaweb.Pages
{
    public class ShowMessagesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ShowMessagesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Message> Messages { get; set; } = new List<Message>();

        public async Task OnGetAsync()
        {
            await LoadMessagesAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int messageId)
        {
            // Add your logic to delete message here
            // You may also check permissions before deleting the message
            // Example:
            // if (!CanDeleteMessage(userId))
            // {
            //     return Forbid(); // Return forbidden status if user doesn't have permission
            // }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string deleteQuery = "DELETE FROM Messages WHERE Id = @MessageId";
                using (var command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    // Refresh messages after deletion
                    await LoadMessagesAsync();

                    return RedirectToPage("/ShowMessages");
                }
            }
        }

        public bool CanDeleteMessage(string userId)
        {
            string loggedInUserId = User.Identity.Name;

            if(loggedInUserId == userId) { return true; }

            if (string.IsNullOrEmpty(loggedInUserId))
            {
                return false; // No logged-in user, cannot delete
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Permissions WHERE OwnerUsername = @Owner AND TargetUsername = @Target";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Owner", loggedInUserId);
                    command.Parameters.AddWithValue("@Target", userId);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    return count > 0;
                }
            }
        }

        private async Task LoadMessagesAsync()
        {
            Messages.Clear();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM Messages"; // Adjust the query as per your table schema
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var message = new Message
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Content = reader.GetString(reader.GetOrdinal("Content")),
                                UserId = reader.GetString(reader.GetOrdinal("UserId"))
                            };
                            Messages.Add(message);
                        }
                    }
                }
            }
        }
    }
}
