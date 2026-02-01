using Application.Interfaces.Crypto;
using Infrastructure.Crypto;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// DEBUG: Verifique a chave
Console.WriteLine("=== INICIALIZANDO API DE OBFUSCAÇÃO ===");
var secret = builder.Configuration["ENCRYPTED_ID_SECRET"] 
             ?? Environment.GetEnvironmentVariable("ENCRYPTED_ID_SECRET");
             
if (string.IsNullOrEmpty(secret))
{
    Console.WriteLine("❌ ERRO: ENCRYPTED_ID_SECRET não encontrado!");
}
else
{
    Console.WriteLine($"✅ Chave carregada: {secret.Substring(0, Math.Min(8, secret.Length))}...");
    Console.WriteLine($"   Tamanho: {secret.Length} caracteres");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IEncryptedIdService, EncryptedIdService>();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Endpoint de saúde
app.MapGet("/", () => "Obfuscation Service API - Compatível com Ailos");
app.MapGet("/health", () => Results.Ok(new 
{ 
    Status = "Healthy", 
    Service = "Obfuscation API",
    CompatibleWith = "Ailos EncryptedId",
    Timestamp = DateTime.UtcNow
}));

app.Run();
