using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Individuella;

public class HttpRegisterVisitor
{
    private readonly ILogger<HttpRegisterVisitor> _logger;

    public HttpRegisterVisitor(ILogger<HttpRegisterVisitor> logger)
    {
        _logger = logger;
    }

    [Function("HttpRegisterVisitor")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("HttpRegisterVisitor körs.");

        var form = await req.ReadFormAsync();

        // Hämta värden
        var name = form["name"].ToString();
        var ageString = form["age"].ToString();
        var email = form["email"].ToString();

        // Namnvalidering
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 50)
            return new BadRequestObjectResult("Namnet måste vara minst 2 tecken och max 50 tecken.");
        if (!name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\''))
            return new BadRequestObjectResult("Namnet får bara innehålla bokstäver, mellanslag, bindestreck och apostrof.");
        if (!name.Any(char.IsLetter))
            return new BadRequestObjectResult("Namnet måste innehålla minst en bokstav.");
        if (name.StartsWith(" ") || name.EndsWith(" ") || name.StartsWith("-") || name.EndsWith("-"))
            return new BadRequestObjectResult("Namnet får inte börja eller sluta med mellanslag eller bindestreck.");

        // Åldervalidering
        if (!int.TryParse(ageString, out int age))
            return new BadRequestObjectResult("Åldern måste vara ett tal.");
        if (age <= 0 || age > 100)
            return new BadRequestObjectResult("Åldern måste vara mellan 1 och 100.");

        // E-postvalidering
        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out _))
            return new BadRequestObjectResult("Ogiltig e-postadress.");

        // Spara till databasen
        var connStr = Environment.GetEnvironmentVariable("SqlConnectionString");
        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            const string sql = "INSERT INTO Visitors (Name, Age, Email) VALUES (@Name, @Age, @Email)";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Age", age);
            cmd.Parameters.AddWithValue("@Email", email);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid databasinsert.");
            return new BadRequestObjectResult("Kunde inte spara till databasen.");
        }

        return new OkObjectResult($"Välkommen, {name}! Din registrering är sparad.");
    }
}
