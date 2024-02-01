using MySqlConnector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Xml;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection.PortableExecutable;

namespace animalCharacteristics_aspnetCORE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<MySqlConnection>(sp => new MySqlConnection(builder.Configuration.GetConnectionString("Connection")));

            var app = builder.Build();

            app.MapPost("/add-entry", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    // Read the form data from the request
                    var form = await context.Request.ReadFormAsync();
                    var characteristic = form["char-field"].ToString();
                    var animals = form["anim-field"].ToString().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                    try
                    {
                        // Insert the characteristic into the characteristics table
                        string insertCharacteristicQuery = "INSERT INTO characteristics (characteristic) VALUES (@characteristic);";
                        using (var command = new MySqlCommand(insertCharacteristicQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@characteristic", characteristic);
                            dBConnection.Open();
                            command.ExecuteNonQuery();
                            dBConnection.Close();
                        }

                        // Get the id of the characteristic
                        string getCharIdQuery = "SELECT idCharacteristics FROM characteristics WHERE characteristic = @characteristic;";
                        int? idCharacteristic = null;
                        using (var command = new MySqlCommand(getCharIdQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@characteristic", characteristic);
                            dBConnection.Open();

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    idCharacteristic = reader.GetInt32(0);
                                }
                            }

                            dBConnection.Close();
                        }

                        foreach (var animal in animals)
                        {
                            // Get the id of the animal
                            string getAnimalIdQuery = "SELECT idAnimals FROM animals WHERE name = @name;";
                            int? idAnimal = null;
                            using (var command = new MySqlCommand(getAnimalIdQuery, dBConnection))
                            {
                                command.Parameters.AddWithValue("@name", animal);
                                dBConnection.Open();

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        idAnimal = reader.GetInt32(0);
                                    }
                                }

                                dBConnection.Close();
                            }

                            // Only attempt to insert into the junction table if valid ids were found
                            if (idCharacteristic.HasValue && idAnimal.HasValue)
                            {
                                // Insert the relationship into the junction table
                                string insertJunctionQuery = "INSERT INTO junction (idCharacteristics, idAnimals) VALUES (@idCharacteristics, @idAnimals);";
                                using (var command = new MySqlCommand(insertJunctionQuery, dBConnection))
                                {
                                    command.Parameters.AddWithValue("@idCharacteristics", idCharacteristic.Value);
                                    command.Parameters.AddWithValue("@idAnimals", idAnimal.Value);
                                    dBConnection.Open();
                                    command.ExecuteNonQuery();
                                    dBConnection.Close();
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Failed to get the ids for animal '{animal}'. Please check the characteristic and animal values.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception or handle it appropriately
                        Console.WriteLine(ex.Message);
                    }
                }
            });

            app.MapPost("/del-entry", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    // Read the form data from the request
                    var form = await context.Request.ReadFormAsync();
                    var characteristic = form["char-field-page1"].ToString();

                    try
                    {
                        // Get the id of the characteristic
                        string getCharIdQuery = "SELECT idCharacteristics FROM characteristics WHERE characteristic = @characteristic;";
                        int? idCharacteristic = null;
                        using (var command = new MySqlCommand(getCharIdQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@characteristic", characteristic);
                            dBConnection.Open();

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    idCharacteristic = reader.GetInt32(0);
                                }
                            }

                            dBConnection.Close();
                        }

                        // Delete the associated entries from the junction table
                        if (idCharacteristic.HasValue)
                        {
                            string deleteJunctionQuery = "DELETE FROM junction WHERE idCharacteristics = @idCharacteristics;";
                            using (var command = new MySqlCommand(deleteJunctionQuery, dBConnection))
                            {
                                command.Parameters.AddWithValue("@idCharacteristics", idCharacteristic.Value);
                                dBConnection.Open();
                                command.ExecuteNonQuery();
                                dBConnection.Close();
                            }
                        }

                        // Delete the characteristic from the characteristics table
                        string deleteCharacteristicQuery = "DELETE FROM characteristics WHERE characteristic = @characteristic;";
                        using (var command = new MySqlCommand(deleteCharacteristicQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@characteristic", characteristic);
                            dBConnection.Open();
                            command.ExecuteNonQuery();
                            dBConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle any errors that might have occurred
                        Console.WriteLine(ex.Message);
                    }
                }
            });

            app.MapPost("/add-entry-anim", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    // Read the form data from the request
                    var form = await context.Request.ReadFormAsync();
                    var characteristics = form["char-field-page2"].ToString().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var animal = form["anim-field-page2"].ToString();

                    try
                    {
                        // Insert the animal into the animals table
                        string insertAnimalQuery = "INSERT INTO animals (name) VALUES (@name);";
                        using (var command = new MySqlCommand(insertAnimalQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@name", animal);
                            dBConnection.Open();
                            command.ExecuteNonQuery();
                            dBConnection.Close();
                        }

                        // Get the id of the animal
                        string getAnimalIdQuery = "SELECT idAnimals FROM animals WHERE name = @name;";
                        int? idAnimal = null;
                        using (var command = new MySqlCommand(getAnimalIdQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@name", animal);
                            dBConnection.Open();

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    idAnimal = reader.GetInt32(0);
                                }
                            }

                            dBConnection.Close();
                        }

                        foreach (var characteristic in characteristics)
                        {
                            // Get the id of the characteristic
                            string getCharIdQuery = "SELECT idCharacteristics FROM characteristics WHERE characteristic = @characteristic;";
                            int? idCharacteristic = null;
                            using (var command = new MySqlCommand(getCharIdQuery, dBConnection))
                            {
                                command.Parameters.AddWithValue("@characteristic", characteristic);
                                dBConnection.Open();

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        idCharacteristic = reader.GetInt32(0);
                                    }
                                }

                                dBConnection.Close();
                            }

                            // Only attempt to insert into the junction table if valid ids were found
                            if (idCharacteristic.HasValue && idAnimal.HasValue)
                            {
                                // Insert the relationship into the junction table
                                string insertJunctionQuery = "INSERT INTO junction (idCharacteristics, idAnimals) VALUES (@idCharacteristics, @idAnimals);";
                                using (var command = new MySqlCommand(insertJunctionQuery, dBConnection))
                                {
                                    command.Parameters.AddWithValue("@idCharacteristics", idCharacteristic.Value);
                                    command.Parameters.AddWithValue("@idAnimals", idAnimal.Value);
                                    dBConnection.Open();
                                    command.ExecuteNonQuery();
                                    dBConnection.Close();
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Failed to get the ids for animal '{animal}' and characteristic '{characteristic}'. Please check the characteristic and animal values.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception or handle it appropriately
                        Console.WriteLine(ex.Message);
                    }
                }
            });

            app.MapPost("/del-entry-anim", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    // Read the form data from the request
                    var form = await context.Request.ReadFormAsync();
                    var animal = form["anim-field-page2"].ToString();

                    try
                    {
                        // Get the id of the characteristic
                        string getAnimIdQuery = "SELECT idAnimals FROM animals WHERE name = @name;";
                        int? idAnimal = null;
                        using (var command = new MySqlCommand(getAnimIdQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@name", animal);
                            dBConnection.Open();

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    idAnimal = reader.GetInt32(0);
                                }
                            }

                            dBConnection.Close();
                        }

                        // Delete the associated entries from the junction table
                        if (idAnimal.HasValue)
                        {
                            string deleteJunctionQuery = "DELETE FROM junction WHERE idAnimals = @idAnimals;";
                            using (var command = new MySqlCommand(deleteJunctionQuery, dBConnection))
                            {
                                command.Parameters.AddWithValue("@idAnimals", idAnimal.Value);
                                dBConnection.Open();
                                command.ExecuteNonQuery();
                                dBConnection.Close();
                            }
                        }

                        // Delete the characteristic from the characteristics table
                        string deleteCharacteristicQuery = "DELETE FROM animals WHERE name = @name;";
                        using (var command = new MySqlCommand(deleteCharacteristicQuery, dBConnection))
                        {
                            command.Parameters.AddWithValue("@name", animal);
                            dBConnection.Open();
                            command.ExecuteNonQuery();
                            dBConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle any errors that might have occurred
                        Console.WriteLine(ex.Message);
                    }
                }
            });

            app.MapGet("/", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    using (var command = new MySqlCommand("SELECT * FROM characteristics", dBConnection))
                    {
                        dBConnection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            var data = new List<Dictionary<string, object>>();

                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }

                                data.Add(row);
                            }

                            await context.Response.WriteAsJsonAsync(data);
                        }

                        dBConnection.Close();
                    }

                }
            });

            app.MapGet("/animals", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    using (var command = new MySqlCommand("SELECT * FROM animals", dBConnection))
                    {
                        dBConnection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            var data = new List<Dictionary<string, object>>();

                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }

                                data.Add(row);
                            }

                            await context.Response.WriteAsJsonAsync(data);
                        }

                        dBConnection.Close();
                    }
                }
            });

            app.MapPost("/getRelatedAnimals", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    // Extract the characteristic from the request body
                    var request = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();
                    var characteristic = request["characteristic"];
                    string query = "SELECT characteristics.characteristic, animals.name\r\nFROM characteristics\r\nINNER JOIN junction ON characteristics.idCharacteristics = junction.idCharacteristics\r\nINNER JOIN animals ON junction.idAnimals = animals.idAnimals\r\nWHERE characteristics.characteristic = @characteristic;";

                    // Query the junction table to find all animals related to the characteristic
                    using (var command = new MySqlCommand(query, dBConnection))
                    {
                        command.Parameters.AddWithValue("@characteristic", characteristic);
                        dBConnection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            var data = new List<Dictionary<string, object>>();

                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }

                                data.Add(row);
                            }

                            await context.Response.WriteAsJsonAsync(data);
                        }

                        dBConnection.Close();
                    }
                }
            });

            app.MapPost("/getRelatedChars", async context =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var dBConnection = services.GetRequiredService<MySqlConnection>();

                    // Extract the characteristic from the request body
                    var request = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();
                    var name = request["name"];
                    string query = "SELECT characteristics.characteristic, animals.name\r\nFROM animals\r\nINNER JOIN junction ON animals.idAnimals = junction.idAnimals\r\nINNER JOIN characteristics ON junction.idCharacteristics = characteristics.idCharacteristics\r\nWHERE animals.name = @name;";

                    // Query the junction table to find all animals related to the characteristic
                    using (var command = new MySqlCommand(query, dBConnection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        dBConnection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            var data = new List<Dictionary<string, object>>();

                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }

                                data.Add(row);
                            }

                            await context.Response.WriteAsJsonAsync(data);
                        }

                        dBConnection.Close();
                    }
                }
            });

            FileServerOptions fileServerOptions = new FileServerOptions();
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();

            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("index.html");
            app.UseFileServer(fileServerOptions);

            app.Run();
        }

    }

}


