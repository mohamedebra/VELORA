using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://127.0.0.1:5080");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "VELORA.Staff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddSingleton<StoreData>();

var app = builder.Build();
await app.Services.GetRequiredService<StoreData>().InitializeAsync();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/auth/session", (ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true
    ? Results.Ok(new { user = new { email = user.FindFirst(ClaimTypes.Email)?.Value, role = "owner", name = "مدير VELORA" } })
    : Results.Unauthorized());
app.MapPost("/api/auth/staff/login", async (StaffLoginRequest credentials, HttpContext context, IConfiguration configuration) =>
{
    var configuredEmail = configuration["VeloraAdmin:Email"];
    var configuredPassword = configuration["VeloraAdmin:Password"];
    if (string.IsNullOrWhiteSpace(configuredEmail) || string.IsNullOrEmpty(configuredPassword))
        return Results.Problem("لم يتم إعداد بريد وكلمة مرور الداشبورد على الخادم.", statusCode: StatusCodes.Status503ServiceUnavailable);

    var suppliedPassword = Encoding.UTF8.GetBytes(credentials.Password ?? string.Empty);
    var expectedPassword = Encoding.UTF8.GetBytes(configuredPassword);
    if (!string.Equals(credentials.Email?.Trim(), configuredEmail.Trim(), StringComparison.OrdinalIgnoreCase)
        || !CryptographicOperations.FixedTimeEquals(suppliedPassword, expectedPassword))
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, configuredEmail.Trim().ToLowerInvariant()),
        new Claim(ClaimTypes.Email, configuredEmail.Trim()),
        new Claim(ClaimTypes.Role, "owner"),
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
    {
        IsPersistent = true,
        AllowRefresh = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
    });
    return Results.Ok(new { user = new { email = configuredEmail.Trim(), role = "owner", name = "مدير VELORA" } });
});
app.MapPost("/api/auth/staff/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});
app.MapPost("/api/auth/staff/presence/heartbeat", (StaffHeartbeatRequest heartbeat, ClaimsPrincipal user) => Results.Ok(new { section = heartbeat.Section, email = user.FindFirst(ClaimTypes.Email)?.Value })).RequireAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "VELORA API" }));
app.MapGet("/api/products", async (StoreData store) => Results.Ok((await store.GetProducts()).Where(item => item.Status != "hidden")));
app.MapGet("/api/store-settings", async (StoreData store) => Results.Ok(await store.GetStoreSettings()));
app.MapPut("/api/admin/store-settings", async (StoreSettingsUpdate update, StoreData store) =>
{
    var number = new string((update.WhatsAppNumber ?? "").Where(char.IsDigit).ToArray());
    if (number.Length is < 8 or > 15 || number.StartsWith('0'))
        return Results.BadRequest(new { message = "أدخلي رقم واتساب دولي صحيحاً مع مفتاح الدولة، مثل 201009846536." });
    return Results.Ok(await store.SaveWhatsAppNumber(number));
}).RequireAuthorization();
app.MapGet("/api/admin/products", async (StoreData store) => Results.Ok(await store.GetProducts())).RequireAuthorization();
app.MapPut("/api/admin/products", async (List<StoreProduct> products, StoreData store) =>
{
    if (products.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Name)))
        return Results.BadRequest(new { message = "كل منتج يحتاج إلى معرّف واسم." });
    if (products.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != products.Count)
        return Results.BadRequest(new { message = "معرّفات المنتجات يجب أن تكون فريدة." });
    if (products.Any(item => item.Price < 0 || item.DiscountValue < 0 || (item.DiscountType == "percentage" && item.DiscountValue > 100) || (item.DiscountType != "percentage" && item.DiscountType != "fixed") || item.SizeOptions is null || item.FrameOptions is null || item.LightingOptions is null || item.SizeOptions.Any(option => option.Price < 0) || item.FrameOptions.Any(option => option.Price < 0) || item.LightingOptions.Any(option => option.Price < 0)))
        return Results.BadRequest(new { message = "السعر وقيم الخيارات لا يمكن أن تكون سالبة، والخصم النسبي يجب أن يكون من 0 إلى 100." });
    foreach (var item in products)
    {
        var discount = item.DiscountType == "fixed" ? item.DiscountValue : item.Price * item.DiscountValue / 100m;
        item.SalePrice = Math.Max(0, item.Price - Math.Min(item.Price, discount));
    }
    return Results.Ok(await store.ReplaceProducts(products));
}).RequireAuthorization();

app.MapGet("/api/admin/requests", async (StoreData store) => Results.Ok(await store.GetRequests())).RequireAuthorization();
app.MapPost("/api/requests", async (JsonElement request, StoreData store) =>
{
    if (request.ValueKind != JsonValueKind.Object) return Results.BadRequest(new { message = "بيانات الطلب غير صالحة." });
    return Results.Ok(await store.AddRequest(request));
});
app.MapPatch("/api/admin/requests/{id}", async (string id, RequestStatusUpdate update, StoreData store) =>
{
    var result = await store.UpdateRequestStatus(id, update.Status);
    return result is null ? Results.NotFound(new { message = "الطلب غير موجود." }) : Results.Ok(result);
}).RequireAuthorization();

app.Run();

public sealed class StoreProduct
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Category { get; set; } = "mirror";
    public string Type { get; set; } = "arch";
    public decimal Price { get; set; }
    public string DiscountType { get; set; } = "percentage";
    public decimal DiscountValue { get; set; }
    public decimal SalePrice { get; set; }
    public string SizePriceMode { get; set; } = "additional";
    public string Size { get; set; } = "";
    public string Finish { get; set; } = "";
    public string Description { get; set; } = "";
    public string Delivery { get; set; } = "";
    public string Warranty { get; set; } = "";
    public List<ProductSpecification> Specifications { get; set; } = [];
    public List<ProductOption> SizeOptions { get; set; } = [];
    public List<ProductOption> FrameOptions { get; set; } = [];
    public List<ProductOption> LightingOptions { get; set; } = [];
    public string Tone { get; set; } = "#d8c8b6";
    public List<string> Images { get; set; } = [];
    public string Status { get; set; } = "published";
}

public sealed class ProductSpecification
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class ProductOption
{
    public string Label { get; set; } = "";
    public decimal Price { get; set; }
}

public sealed record RequestStatusUpdate(string Status);
public sealed record StoreSettingsUpdate(string? WhatsAppNumber);
public sealed record StoreSettings(string WhatsAppNumber);
public sealed record StaffLoginRequest(string? Email, string? Password);
public sealed record StaffHeartbeatRequest(string? Section);

public sealed class StoreData
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _connectionString;
    private readonly string _databasePath;
    private readonly string _seedPath;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public StoreData(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _databasePath = Path.Combine(environment.ContentRootPath, "App_Data", "velora.db");
        _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Velora")
            ?? configuration.GetConnectionString("Velora")
            ?? throw new InvalidOperationException("لم يتم ضبط ConnectionStrings:Velora للاتصال بـ SQL Server.");
        _seedPath = Path.Combine(environment.ContentRootPath, "Data", "products.seed.json");
    }

    public async Task InitializeAsync()
    {
        await using var connection = await OpenConnection();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
                    CREATE TABLE dbo.Products (Id nvarchar(450) NOT NULL PRIMARY KEY, Data nvarchar(max) NOT NULL, SortOrder int NOT NULL DEFAULT 0);
                IF OBJECT_ID(N'dbo.Requests', N'U') IS NULL
                    CREATE TABLE dbo.Requests (Id nvarchar(450) NOT NULL PRIMARY KEY, Data nvarchar(max) NOT NULL, SortOrder bigint NOT NULL DEFAULT 0);
                IF OBJECT_ID(N'dbo.StoreSettings', N'U') IS NULL
                    CREATE TABLE dbo.StoreSettings (Name nvarchar(100) NOT NULL PRIMARY KEY, Value nvarchar(500) NOT NULL);
                IF NOT EXISTS (SELECT 1 FROM dbo.StoreSettings WHERE Name = N'WhatsAppNumber')
                    INSERT INTO dbo.StoreSettings (Name, Value) VALUES (N'WhatsAppNumber', N'201009846536');
                """;
            await command.ExecuteNonQueryAsync();
        }

        if (await IsTableEmpty(connection, "dbo.Products"))
        {
            var products = await ReadLegacySqliteProducts();
            var legacyProductsPath = Path.Combine(Path.GetDirectoryName(_databasePath)!, "products.json");
            if (products.Count == 0)
                products = File.Exists(legacyProductsPath)
                    ? JsonSerializer.Deserialize<List<StoreProduct>>(await File.ReadAllTextAsync(legacyProductsPath), JsonOptions) ?? []
                    : JsonSerializer.Deserialize<List<StoreProduct>>(await File.ReadAllTextAsync(_seedPath), JsonOptions) ?? [];
            await InsertProducts(connection, products);
        }

        if (await IsTableEmpty(connection, "dbo.Requests"))
        {
            var requests = await ReadLegacySqliteRequests();
            var legacyRequestsPath = Path.Combine(Path.GetDirectoryName(_databasePath)!, "requests.json");
            if (requests.Count == 0 && File.Exists(legacyRequestsPath))
            {
                requests = JsonSerializer.Deserialize<List<JsonObject>>(await File.ReadAllTextAsync(legacyRequestsPath), JsonOptions) ?? [];
            }
            await InsertRequests(connection, requests);
        }
    }

    public async Task<List<StoreProduct>> GetProducts()
    {
        await _gate.WaitAsync();
        try
        {
            await using var connection = await OpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Data FROM dbo.Products ORDER BY SortOrder, Id";
            await using var reader = await command.ExecuteReaderAsync();
            var products = new List<StoreProduct>();
            while (await reader.ReadAsync())
                if (JsonSerializer.Deserialize<StoreProduct>(reader.GetString(0), JsonOptions) is { } product) products.Add(product);
            return products;
        }
        finally { _gate.Release(); }
    }

    public async Task<StoreSettings> GetStoreSettings()
    {
        await using var connection = await OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM dbo.StoreSettings WHERE Name = N'WhatsAppNumber'";
        var value = await command.ExecuteScalarAsync() as string;
        return new StoreSettings(string.IsNullOrWhiteSpace(value) ? "201009846536" : value);
    }

    public async Task<StoreSettings> SaveWhatsAppNumber(string number)
    {
        await _gate.WaitAsync();
        try
        {
            await using var connection = await OpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE dbo.StoreSettings SET Value = @value WHERE Name = N'WhatsAppNumber'; IF @@ROWCOUNT = 0 INSERT INTO dbo.StoreSettings (Name, Value) VALUES (N'WhatsAppNumber', @value);";
            command.Parameters.AddWithValue("@value", number);
            await command.ExecuteNonQueryAsync();
            return new StoreSettings(number);
        }
        finally { _gate.Release(); }
    }

    public async Task<List<StoreProduct>> ReplaceProducts(List<StoreProduct> products)
    {
        await _gate.WaitAsync();
        try
        {
            await using var connection = await OpenConnection();
            await using var transaction = connection.BeginTransaction();
            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM dbo.Products";
                await delete.ExecuteNonQueryAsync();
            }
            await InsertProducts(connection, products, transaction);
            await transaction.CommitAsync();
            return products;
        }
        finally { _gate.Release(); }
    }

    public async Task<List<JsonObject>> GetRequests()
    {
        await _gate.WaitAsync();
        try
        {
            await using var connection = await OpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Data FROM dbo.Requests ORDER BY SortOrder DESC, Id DESC";
            await using var reader = await command.ExecuteReaderAsync();
            var requests = new List<JsonObject>();
            while (await reader.ReadAsync())
                if (JsonNode.Parse(reader.GetString(0)) is JsonObject request) requests.Add(request);
            return requests;
        }
        finally { _gate.Release(); }
    }

    public async Task<JsonObject> AddRequest(JsonElement request)
    {
        await _gate.WaitAsync();
        try
        {
            var item = JsonNode.Parse(request.GetRawText())!.AsObject();
            if (item["id"] is null) item["id"] = $"VR-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            if (item["createdAt"] is null) item["createdAt"] = DateTimeOffset.UtcNow.ToString("O");
            if (item["status"] is null) item["status"] = "جديد";
            await using var connection = await OpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO dbo.Requests (Id, Data, SortOrder) VALUES (@id, @data, @order)";
            command.Parameters.AddWithValue("@id", (string)item["id"]!);
            command.Parameters.AddWithValue("@data", item.ToJsonString(JsonOptions));
            command.Parameters.AddWithValue("@order", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await command.ExecuteNonQueryAsync();
            return item;
        }
        finally { _gate.Release(); }
    }

    public async Task<JsonObject?> UpdateRequestStatus(string id, string status)
    {
        await _gate.WaitAsync();
        try
        {
            await using var connection = await OpenConnection();
            await using var transaction = connection.BeginTransaction();
            JsonObject? item;
            await using (var select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = "SELECT Data FROM dbo.Requests WHERE Id = @id";
                select.Parameters.AddWithValue("@id", id);
                var data = await select.ExecuteScalarAsync() as string;
                item = data is null ? null : JsonNode.Parse(data) as JsonObject;
            }
            if (item is null) { await transaction.RollbackAsync(); return null; }
            item["status"] = status;
            await using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = "UPDATE dbo.Requests SET Data = @data WHERE Id = @id";
                update.Parameters.AddWithValue("@id", id);
                update.Parameters.AddWithValue("@data", item.ToJsonString(JsonOptions));
                await update.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
            return item;
        }
        finally { _gate.Release(); }
    }

    private async Task<List<StoreProduct>> ReadLegacySqliteProducts()
    {
        if (!File.Exists(_databasePath)) return [];
        try
        {
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _databasePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Data FROM Products ORDER BY SortOrder, Id";
            await using var reader = await command.ExecuteReaderAsync();
            var products = new List<StoreProduct>();
            while (await reader.ReadAsync())
                if (JsonSerializer.Deserialize<StoreProduct>(reader.GetString(0), JsonOptions) is { } product) products.Add(product);
            return products;
        }
        catch (SqliteException) { return []; }
    }

    private async Task<List<JsonObject>> ReadLegacySqliteRequests()
    {
        if (!File.Exists(_databasePath)) return [];
        try
        {
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _databasePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Data FROM Requests ORDER BY SortOrder DESC, Id DESC";
            await using var reader = await command.ExecuteReaderAsync();
            var requests = new List<JsonObject>();
            while (await reader.ReadAsync())
                if (JsonNode.Parse(reader.GetString(0)) is JsonObject request) requests.Add(request);
            return requests;
        }
        catch (SqliteException) { return []; }
    }

    private async Task<SqlConnection> OpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<bool> IsTableEmpty(SqlConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {table}) THEN 0 ELSE 1 END";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task InsertProducts(SqlConnection connection, List<StoreProduct> products, SqlTransaction? transaction = null)
    {
        for (var index = 0; index < products.Count; index++)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO dbo.Products (Id, Data, SortOrder) VALUES (@id, @data, @order)";
            command.Parameters.AddWithValue("@id", products[index].Id);
            command.Parameters.AddWithValue("@data", JsonSerializer.Serialize(products[index], JsonOptions));
            command.Parameters.AddWithValue("@order", index);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertRequests(SqlConnection connection, List<JsonObject> requests)
    {
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var id = (string?)request["id"] ?? $"VR-MIGRATED-{index}";
            request["id"] = id;
            await using var command = connection.CreateCommand();
            command.CommandText = "IF NOT EXISTS (SELECT 1 FROM dbo.Requests WHERE Id = @id) INSERT INTO dbo.Requests (Id, Data, SortOrder) VALUES (@id, @data, @order)";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@data", request.ToJsonString(JsonOptions));
            command.Parameters.AddWithValue("@order", requests.Count - index);
            await command.ExecuteNonQueryAsync();
        }
    }
}
