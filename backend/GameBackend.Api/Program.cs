using GameBackend.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddGameBackend(builder.Configuration);

WebApplication app = builder.Build();

app.UseApiExceptionHandling();

if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Game Backend API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

await app.ApplyDatabaseMigrationsAsync();
await app.RunAsync();

public partial class Program;
