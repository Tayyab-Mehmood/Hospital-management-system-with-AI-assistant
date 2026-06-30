using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- LifePulse Database & CORS Configurations ---
builder.Services.AddDbContext<LifePulse.API.Data.LifePulseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorGrid", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
// ------------------------------------------------

// Add services to the container.
builder.Services.AddControllers();

// Register named HttpClient for Gemini API calls (used by ChatbotController)
builder.Services.AddHttpClient("GeminiClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Enable modern OpenAPI generation
builder.Services.AddOpenApi();

// Inject traditional Swagger UI Explorer services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// We enable these for all environments so you can test effortlessly for your project
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LifePulse API v1");
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors("AllowBlazorGrid");

app.MapControllers();

app.Run();
