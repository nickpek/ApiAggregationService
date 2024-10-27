using ApiAggregation.Clients;
using ApiAggregation.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
// Register individual API clients and each interface with its implementation
builder.Services.AddScoped<IOpenWeatherClient, OpenWeatherClient>();
builder.Services.AddScoped<INewsApiClient, NewsApiClient>();
builder.Services.AddScoped<IApiFootballClient, ApiFootballClient>();
builder.Services.AddHttpClient<IApiFootballClient, ApiFootballClient>();


builder.Services.AddScoped<AggregationService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
