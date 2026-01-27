using System.Reflection;
using Flightfront.Application.Interfaces;
using Flightfront.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient<IMetarService, CheckWxMetarService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "FlightFront METAR API",
            Version = "v1",
            Description = "API for retrieving and parsing METAR weather data for airports",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact { Name = "FlightFront" },
        }
    );

    // Include XML comments if the file exists
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAngularApp",
        builder => builder.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader()
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
