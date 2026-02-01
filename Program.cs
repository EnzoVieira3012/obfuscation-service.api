using Application.Interfaces.Crypto;
using Infrastructure.Crypto;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

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
        c.RoutePrefix = string.Empty; // Para exibir Swagger na raiz em produção
    });
}

app.MapControllers();
app.Run();