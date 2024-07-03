using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProductReviewAPI.Controllers;
using ProductReviewAPI.Models;
using ProductReviewAPI.Repository;
using ProductReviewAPI.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// needed to store rate limit counters and ip rules
builder.Services.AddMemoryCache();

// load general configuration from appsettings.json
builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.Configure<ClientRateLimitPolicies>(builder.Configuration.GetSection("ClientRateLimitPolicies"));
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

// inject counter and rules stores
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();

// configuration (resolvers, counter key builders)
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// inject AspNetCoreRateLimit middlewares
builder.Services.AddInMemoryRateLimiting();

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("_myAllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://alfredo.dovahlabs.com", "https://dovahlabs.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// CONTEXT
builder.Services.AddDbContext<InmueblesDbpContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserConnection"));
});

// Repository
builder.Services.AddScoped(typeof(IUserRepository<>), typeof(UserRepository<>));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();


builder.Services.AddScoped<IProductCommentRepository, ProductCommentRepository>();
builder.Services.AddScoped<IProductCommentService, ProductCommentService>();

builder.Services.AddScoped<IPReviewService, PReviewController>();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        RoleClaimType = ClaimTypes.Role // Indicar que se usará el claim de rol para la autorización
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();
app.UseIpRateLimiting();
app.UseClientRateLimiting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("_myAllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
