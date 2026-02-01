using Application.Interfaces.Crypto;
using Infrastructure.Crypto;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// DEBUG: Verifique se a chave está sendo carregada
Console.WriteLine("=== INICIALIZANDO API ===");
var secret = builder.Configuration["ENCRYPTED_ID_SECRET"];
if (string.IsNullOrEmpty(secret))
{
    Console.WriteLine("ERRO: ENCRYPTED_ID_SECRET não encontrado na configuração!");
    
    // Tenta carregar do ambiente alternativo
    secret = Environment.GetEnvironmentVariable("ENCRYPTED_ID_SECRET");
    if (string.IsNullOrEmpty(secret))
    {
        Console.WriteLine("ERRO: ENCRYPTED_ID_SECRET também não encontrado nas variáveis de ambiente!");
    }
    else
    {
        Console.WriteLine($"Chave encontrada nas variáveis de ambiente: {secret.Substring(0, Math.Min(secret.Length, 10))}...");
    }
}
else
{
    Console.WriteLine($"Chave encontrada na configuração: {secret.Substring(0, Math.Min(secret.Length, 10))}...");
}
Console.WriteLine("=== FIM DEBUG INICIAL ===");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    options.AddPolicy("ProductionCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://obfuscation-serviceweb.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IEncryptedIdService, EncryptedIdService>();

var app = builder.Build();

// Aplicar CORS baseado no ambiente
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseCors("ProductionCors");
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Obfuscation API");
        c.RoutePrefix = string.Empty;
    });
}

// Adicione este middleware para logs de todas as requisições
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {context.Request.Method} {context.Request.Path}");
    await next();
});

app.MapControllers();

// Endpoint de saúde/teste
app.MapGet("/health", () => 
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Health check");
    return Results.Ok(new { 
        Status = "Healthy", 
        Timestamp = DateTime.UtcNow,
        SecretConfigured = !string.IsNullOrEmpty(builder.Configuration["ENCRYPTED_ID_SECRET"])
    });
});

app.Run();
