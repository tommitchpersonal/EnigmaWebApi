using NumberRandomizer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IEnigmaWrapperFactory, EnigmaWrapperFactory>();
builder.Services.AddSingleton<IRandomArrayGenerator, RandomArrayGenerator>();
builder.Services.AddSingleton<IRandomSettingsGenerator, RandomSettingsGenerator>();
builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();

builder.Services.AddControllers();

builder.Services.AddAuthentication("BasicAuthentication").AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>  
{  
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Enigma Web API", Version = "v1" });  
    c.AddSecurityDefinition("basic", new OpenApiSecurityScheme  
    {  
        Name = "Authorization",  
        Type = SecuritySchemeType.Http,  
        Scheme = "basic",  
        In = ParameterLocation.Header,  
        Description = "Basic Authorization header using the Bearer scheme."  
    });  
    c.AddSecurityRequirement(new OpenApiSecurityRequirement  
    {  
        {  
            new OpenApiSecurityScheme  
            {  
                Reference = new OpenApiReference  
                {  
                    Type = ReferenceType.SecurityScheme,  
                    Id = "basic"  
                }  
            },  
            new string[] {}  
        }  
    });  
}); 

var username = builder.Configuration["Credentials:Username"];
var password = builder.Configuration["Credentials:Password"];

if (username == null || password == null)
{
    return;
}

builder.Services.Configure<Credentials>(opt => 
{
    opt.Username = username;
    opt.Password = password;
});

var app = builder.Build();

app.UseWebSockets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
