using System.Text;
using CORE_BE.Data;
using CORE_BE.Infrastructure;
using CORE_BE.Middleware;
using CORE_BE.Models;
using CORE_BE.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var jwt = builder.Configuration.GetSection("AppSettings");
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"])),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("❌ Auth failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = async ctx =>
            {
                Console.WriteLine("✅ Token valid");
                var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var userId = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null || user.IsDeleted || !user.IsActive)
                    {
                        ctx.Fail("Tài khoản đã bị khóa hoặc bị xóa.");
                    }
                }
            },
        };
    });
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["*"];
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "CorsApi",
        policy =>
        {
            if (allowedOrigins.Contains("*"))
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
        }
    );
});

var mode = builder.Configuration["DistributedSettings:Mode"];

if (mode == "Center")
{
    builder.Services.AddDbContext<MyDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    );
    builder
        .Services.AddIdentityCore<ApplicationUser>()
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<MyDbContext>()
        .AddSignInManager();
}
else
{
    // Agent mode: Không cần DB và Identity
    Console.WriteLine(">>> Chạy ở chế độ Agent: Bỏ qua kết nối SQL Server nội bộ.");
}

builder.Services.AddHostedService<IdracWorker>();
builder.Services.AddHostedService<AgentWorker>();
builder.Services.AddScoped<IIdracService, IdracService>();

builder
    .Services.AddHttpClient("idrac")
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60); // tăng lên 60s
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            // Ép cho phép sử dụng các chuẩn bảo mật cũ (iDRAC đời cũ thường dùng TLS cũ)
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 
                           | System.Security.Authentication.SslProtocols.Tls13 
                           | System.Security.Authentication.SslProtocols.Tls11 
                           | System.Security.Authentication.SslProtocols.Tls
        }
    );

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Product Backend API",
            Version = "v1",
            Description = "Backend API for AUTO system",
        }
    );
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập JWT theo format: Bearer {token}",
        }
    );

    // 🔒 Áp dụng cho toàn bộ API
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});
var app = builder.Build();

// Global exception handler — must be first in pipeline
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Backend API v1");
    c.RoutePrefix = string.Empty; // Đặt Swagger UI tại root (http://localhost:port/)
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});
app.UseCors("CorsApi");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
