using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Security.Claims;

// Build the app
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSPApi(settings => settings.EnableHelp = true);
builder.Services.AddTransient<IDbConnection>(services =>
{
    var config = services.GetService<IConfiguration>();
    return new SqlConnection(config.GetConnectionString("DefaultConnection"));
});
builder.Services.AddControllers();
var app = builder.Build();

// Run the app
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    // Simulate user logged in
    var claims = new Claim[]
    {
        new Claim(ClaimTypes.Name, "sample-user"),
        new Claim("entity.read", string.Empty),
    };
    var id = new ClaimsIdentity(claims, "demo");
    context.User = new ClaimsPrincipal(id);
    await next();
});
app.UseSPApi();
app.MapControllers();
app.Run();
