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

        //Logga inmatningen från alla fält
        _logger.LogInformation(
            "Inmatning mottagen: Namn={Name}, Ålder={AgeInput}, Email={Email}",
            name, ageString, email);

        // Namnvalidering
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 50)
        {
            _logger.LogWarning("Namnet är för kort eller för långt. Input: {Name}", name);
            return new BadRequestObjectResult("Namnet måste vara minst 2 tecken och max 50 tecken.");
        }

        if (!name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\''))
        {
            _logger.LogWarning("Namnet innehåller otillåtna tecken. Input: {Name}", name);
            return new BadRequestObjectResult("Namnet får bara innehålla bokstäver, mellanslag, bindestreck och apostrof.");
        }

        if (!name.Any(char.IsLetter))
        {
            _logger.LogWarning("Namnet saknar bokstäver. Input: {Name}", name);
            return new BadRequestObjectResult("Namnet måste innehålla minst en bokstav.");
        }

        if (name.StartsWith(" ") || name.EndsWith(" ") || name.StartsWith("-") || name.EndsWith("-"))
        {
            _logger.LogWarning("Namnet börjar eller slutar fel. Input: {Name}", name);
            return new BadRequestObjectResult("Namnet får inte börja eller sluta med mellanslag eller bindestreck.");
        }

        // Åldervalidering
        if (!int.TryParse(ageString, out int age))
        {
            _logger.LogWarning("Ålder saknar siffror. Input: {AgeString}", ageString);
            return new BadRequestObjectResult("Åldern måste vara ett tal.");
        }

        if (age <= 0 || age > 100)
        {
            _logger.LogWarning("Åldern är utanför tillåtet intervall. Input: {Age}", age);
            return new BadRequestObjectResult("Åldern måste vara mellan 1 och 100.");
        }

        // E-postvalidering
        email = email.Trim();

        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out _))
        {
            _logger.LogWarning("Ogiltig e-postadress. Input: {Email}", email);
            return new BadRequestObjectResult("Ogiltig e-postadress.");
        }
        
        var connStr = Environment.GetEnvironmentVariable("SqlConnectionString");
        // Kontrollera även om connection string finns.
        if (string.IsNullOrWhiteSpace(connStr))
        {
            _logger.LogError("Connection string saknas.");
            return new ObjectResult("Kunde inte spara till databasen.")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        try
        {   
                // Spara  besökaren i databasen
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();
                
                const string sql = "INSERT INTO Visitors (Name, Age, Email) VALUES (@Name, @Age, @Email)";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Age", age);
                cmd.Parameters.AddWithValue("@Email", email);

                await cmd.ExecuteNonQueryAsync();
        }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // Om e-postadressen redan existerar i databasen och den bryter mot UNIQUE constraint så kastas ett fel
                _logger.LogWarning(ex, "E-post redan registrerad: {Email}", email);
                return new ConflictObjectResult("E-postadressen är redan registrerad.");
            }
            catch (Exception ex)
            {
                // Övriga fel relaterat till databasen fångas
                _logger.LogError(ex, "Fel vid databasinsert.");
                return new ObjectResult("Kunde inte spara till databasen.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

        _logger.LogInformation("Ny besökare registrerad: {Name}", name);
        return new OkObjectResult($"Välkommen, {name}! Din registrering är sparad.");
    }
}
