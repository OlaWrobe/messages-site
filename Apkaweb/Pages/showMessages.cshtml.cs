using Apkaweb.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;

namespace Apkaweb.Pages
{
    public class ShowMessagesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ShowMessagesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        [Required(ErrorMessage = "Message content is required.")]
        [StringLength(255, ErrorMessage = "Message cannot be longer than 255 characters.")]
        public string NewMessage { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();

        public async Task OnGetAsync()
        {
            await LoadMessagesAsync();
        }

        public bool CanDeleteOrEditMessage(string loggedInUserId, string ownerUserId, int messageId, string action)
        {
            if (loggedInUserId == ownerUserId)
            {
                return true;
            }

            // sprawdzanie uprawnień zalogowanego użytkownika na podstawie wpisów w bazie w tabeli permissions
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT COUNT(*)
            FROM Permissions
            WHERE OwnerUsername = @Owner
                AND TargetUsername = @Target";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Owner", ownerUserId);
                    command.Parameters.AddWithValue("@Target", loggedInUserId);

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

                string query = "SELECT * FROM Messages";
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadMessagesAsync();
                return Page();
            }

            string userId = User.Identity.Name;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string insertQuery = "INSERT INTO Messages (Content, UserId) VALUES (@Content, @UserId)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Content", NewMessage);
                    command.Parameters.AddWithValue("@UserId", userId);

                    await command.ExecuteNonQueryAsync();

                    await LoadMessagesAsync();

                    return RedirectToPage("/ShowMessages");
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int messageId)
        {
            string loggedInUserId = User.Identity.Name; // pobranie nazwy zalogowanego użytkownika

            // pobranie właściciela wiadomości
            string ownerUserId;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string selectOwnerQuery = "SELECT UserId FROM Messages WHERE Id = @MessageId";
                using (var command = new MySqlCommand(selectOwnerQuery, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);
                    ownerUserId = await command.ExecuteScalarAsync() as string;
                }
            }

            // sprawdzenie uprawnień zalogowanego użytkownika do usuwania wiadomości
            if (CanDeleteOrEditMessage(loggedInUserId, ownerUserId, messageId, "delete"))
            {
                // usunięcie wiadomości
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string deleteQuery = "DELETE FROM Messages WHERE Id = @MessageId";
                    using (var command = new MySqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MessageId", messageId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            return RedirectToPage("/ShowMessages");
        }

        public async Task<IActionResult> OnPostEditAsync(int messageId, string newContent)
        {
            string loggedInUserId = User.Identity.Name; // pobranie nazwy zalogowanego użytkownika

            // pobranie właściciela wiadomości
            string ownerUserId;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string selectOwnerQuery = "SELECT UserId FROM Messages WHERE Id = @MessageId";
                using (var command = new MySqlCommand(selectOwnerQuery, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);
                    ownerUserId = await command.ExecuteScalarAsync() as string;
                }
            }

            // sprawdzenie uprawnień zalogowanego użytkownika do edycji wiadomości
            if (CanDeleteOrEditMessage(loggedInUserId, ownerUserId, messageId, "edit"))
            {
                // aktualizacja treści wiadomości
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string updateQuery = "UPDATE Messages SET Content = @NewContent WHERE Id = @MessageId";
                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@NewContent", newContent);
                        command.Parameters.AddWithValue("@MessageId", messageId);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            return RedirectToPage("/ShowMessages");
        }

        private async Task<Message> GetMessageByIdAsync(int messageId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM Messages WHERE Id = @MessageId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var message = new Message
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Content = reader.GetString(reader.GetOrdinal("Content")),
                                UserId = reader.GetString(reader.GetOrdinal("UserId"))
                            };
                            return message;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
    }
}
