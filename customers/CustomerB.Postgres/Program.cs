using CustomerB.Postgres;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("CUSTOMER_B_POSTGRES")
    ?? "Host=localhost;Port=5432;Database=customer_b;Username=postgres;Password=postgres";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

const string createTable = """
CREATE TABLE IF NOT EXISTS rentals (
    booking_number text PRIMARY KEY,
    registration_number text NOT NULL,
    customer_id text NOT NULL,
    category text NOT NULL,
    pickup_time timestamptz NOT NULL,
    pickup_odometer integer NOT NULL,
    return_time timestamptz NULL,
    return_odometer integer NULL,
    final_price numeric(18,2) NULL
);
""";

await using (var command = new NpgsqlCommand(createTable, connection))
    await command.ExecuteNonQueryAsync();

Console.WriteLine("Customer B - PostgreSQL storage");
Console.WriteLine("This application owns its relational persistence model and can call the SaaS API independently.");

var api = new RentalApiClient("http://localhost:5000", "tenant-b", "secret-b");

const string insert = """
INSERT INTO rentals (booking_number, registration_number, customer_id, category, pickup_time, pickup_odometer)
VALUES ($1, $2, $3, $4, $5, $6)
ON CONFLICT (booking_number) DO NOTHING;
""";

await using var insertCommand = new NpgsqlCommand(insert, connection);
insertCommand.Parameters.AddWithValue("B-2001");
insertCommand.Parameters.AddWithValue("XYZ789");
insertCommand.Parameters.AddWithValue("customer-b-1");
insertCommand.Parameters.AddWithValue("Truck");
insertCommand.Parameters.AddWithValue(DateTimeOffset.UtcNow);
insertCommand.Parameters.AddWithValue(25000);
await insertCommand.ExecuteNonQueryAsync();

// Live integration against the SaaS API would obtain a JWT through /oauth/token
// and send it as Authorization: Bearer <access_token>.
// var response = await api.RegisterPickupAsync(...);
