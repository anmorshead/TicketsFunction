using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace TicketsFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([QueueTrigger("purchases", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            string json = message.MessageText;


            //
            //Deserialize the message JSON into a purchase object
            //


            //make it not case-sensitive
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            //Deserialize the message JSON into a contact object
            Purchase? purchase = JsonSerializer.Deserialize<Purchase>(json, options);

            if (purchase == null)
            {
                _logger.LogError("Failed to deserialize the message inot a Contact object");
                return;
            }

            _logger.LogInformation($"Contact: {purchase.Name} {purchase.Name}");


            //
            // Add contact to database
            //

            // get connection string from app settings
            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }

            // Using statement ensures the connection is closed when done
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync(); // Note the ASYNC

                string query = "INSERT INTO Purchases (ConcertID, Email, Name, Phone, Quantity, CreditCard, Expiration, SecurityCode, Address, City, Province, PostalCode, Country) " +
                               "VALUES (@ConcertID, @Email, @Name, @Phone, @Quantity, @CreditCard, @Expiration, @SecurityCode, @Address, @City, @Province, @PostalCode, @Country)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ConcertID", purchase.ConcertID);
                    cmd.Parameters.AddWithValue("@Email", purchase.Email);
                    cmd.Parameters.AddWithValue("@Name", purchase.Name);
                    cmd.Parameters.AddWithValue("@Phone", purchase.Phone);
                    cmd.Parameters.AddWithValue("@Quantity", purchase.Quantity);
                    cmd.Parameters.AddWithValue("@CreditCard", purchase.CreditCard);  
                    cmd.Parameters.AddWithValue("@Expiration", purchase.Expiration);
                    cmd.Parameters.AddWithValue("@SecurityCode", purchase.SecurityCode);  
                    cmd.Parameters.AddWithValue("@Address", purchase.Address);
                    cmd.Parameters.AddWithValue("@City", purchase.City);
                    cmd.Parameters.AddWithValue("@Province", purchase.Province);
                    cmd.Parameters.AddWithValue("@PostalCode", purchase.PostalCode);
                    cmd.Parameters.AddWithValue("@Country", purchase.Country);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

        }
    }
}
