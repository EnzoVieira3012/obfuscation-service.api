using Application.Interfaces.Crypto;
using Infrastructure.Crypto;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:4200",
                "https://obfuscation-serviceweb.vercel.app/"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IEncryptedIdService, EncryptedIdService>();

var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
