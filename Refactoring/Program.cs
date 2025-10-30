using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.IdentityModel.Tokens; 
using System.Text; 
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using Auth.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// builder.Services.AddHostedService<SolutionDistributionBackgroundService>();
// builder.Services.AddScoped<SolutionDistributionService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});




builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => 
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ref API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Введите токен JWT в формате **Bearer {ваш токен}**",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, new string[] { } }
    });
});

builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddScoped<IHallService, HallService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFilmService, FilmService>();

builder.Services.AddScoped<IReviewService, ReviewService>();

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ISeatCategoryService, SeatCategoryService>();
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddHostedService<TicketExpirationService>();


builder.Services.AddSingleton<ITokenRevocationService, TokenRevocationService>();

builder.Services.AddHttpContextAccessor();


var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
builder.Services.AddSingleton(smtpSettings);
builder.Services.AddScoped<IEmailService, EmailService>();


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .EnableSensitiveDataLogging());

var secretKey = "G7@!f4#Zq8&lN9^kP2*eR1$hW3%tX6@zB5";
var key = Encoding.ASCII.GetBytes(secretKey); 

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; 
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();




if (true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ref API V1");
        c.RoutePrefix = string.Empty;
    });
}




app.UseCors("AllowAll");

app.UseAuthentication(); 
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (args.Length != 0 && args[0] == "delete")
    {
        await dbContext.Database.EnsureDeletedAsync();
    }

    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();