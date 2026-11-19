using System.Text;
using JobFlow.Api.Middlewares;
using JobFlow.Core.Interfaces;
using JobFlow.Infrastructure.Data;
using JobFlow.Infrastructure.Hubs;
using JobFlow.Infrastructure.Services;
using JobFlow.Worker.BackgroundServices;
using JobFlow.Worker.Handlers;
using JobFlow.Worker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Setup
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=jobflow;Username=postgres;Password=postgres;Include Error Detail=true";

builder.Services.AddDbContext<JobFlowDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// 3. JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretJobFlowSecurityKeyThatIsAtLeast32BytesLong!2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "JobFlowApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "JobFlowClient";

builder.Services.AddAuthentication(options =>
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
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // WebSocket authentication via query string (?access_token=...)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/jobflow"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// 4. CORS Setup
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 5. HttpClient & Infrastructure Services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IDatabaseReadinessService, DatabaseReadinessService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITelemetrySanitizer, TelemetrySanitizer>();

// AI Providers
builder.Services.AddSingleton<IAiAnalysisProvider, MockAiProvider>();
builder.Services.AddHttpClient<OpenAiProvider>();
builder.Services.AddSingleton<IAiAnalysisProvider, OpenAiProvider>();
builder.Services.AddHttpClient<OllamaAiProvider>();
builder.Services.AddSingleton<IAiAnalysisProvider, OllamaAiProvider>();
builder.Services.AddScoped<AiAnalysisService>();

// SignalR & Dispatcher
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISignalRNotificationDispatcher, SignalRNotificationDispatcher>();

// Worker Services & Handlers
builder.Services.AddSingleton<JobCancellationManager>();

builder.Services.AddSingleton<IJobHandler, SampleDataProcessingJobHandler>();
builder.Services.AddSingleton<IJobHandler, BatchRecordProcessingJobHandler>();
builder.Services.AddSingleton<IJobHandler, ReportGenerationJobHandler>();
builder.Services.AddSingleton<IJobHandler, SimulatedFailureJobHandler>();
builder.Services.AddSingleton<IJobHandler, ConfigurableLongRunningJobHandler>();
builder.Services.AddSingleton<JobHandlerRegistry>();

// Background Services (IHostedService)
builder.Services.AddHostedService<JobExecutionWorker>();
builder.Services.AddHostedService<JobSchedulerWorker>();
builder.Services.AddHostedService<JobMonitoringWorker>();
builder.Services.AddHostedService<AIAnalysisWorker>();

// 6. Controllers & OpenAPI / Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QueueSense API",
        Version = "v1",
        Description = "QueueSense — Intelligent background job monitoring and management platform API."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var readinessService = app.Services.GetRequiredService<IDatabaseReadinessService>();

// Auto-migrate & seed database on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();
        logger.LogInformation("Applying EF Core database migrations...");
        db.Database.Migrate();

        // Safe idempotent check: Ensure admin user has verified BCrypt password hash
        var adminUser = db.Users.FirstOrDefault(u => u.Email.ToLower() == "admin@jobflow.ai");
        if (adminUser != null)
        {
            if (!BCrypt.Net.BCrypt.Verify("AdminPassword123!", adminUser.PasswordHash))
            {
                logger.LogWarning("Admin password hash in database does not match AdminPassword123! Updating to verified hash.");
                adminUser.PasswordHash = "$2a$11$TKHys93kM2KIp9A57Cimh.jmBJKGCAKsDCC8XllMkKM/2o.SXzeoi";
                db.SaveChanges();
            }
        }

        readinessService.SetReady();
        logger.LogInformation("Database is ready and up to date.");
    }
    catch (Exception ex)
    {
        readinessService.SetFailed(ex.Message);
        logger.LogError(ex, "Database connection or migration failed on startup.");
    }
}

// Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "QueueSense v1"));
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Health & Readiness Endpoints
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "QueueSense API" }));
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", service = "QueueSense API" }));

app.MapGet("/health/ready", (IDatabaseReadinessService readiness) =>
{
    if (readiness.IsReady)
    {
        return Results.Ok(new
        {
            status = "Ready",
            database = "Connected",
            initializedAt = readiness.InitializedAt
        });
    }

    return Results.Json(new
    {
        status = "Unhealthy",
        database = "Disconnected",
        error = readiness.FailureReason ?? "Database initialization has not completed.",
        lastCheckedAt = readiness.InitializedAt ?? DateTime.UtcNow
    }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/api/health/ready", (IDatabaseReadinessService readiness) =>
{
    if (readiness.IsReady)
    {
        return Results.Ok(new
        {
            status = "Ready",
            database = "Connected",
            initializedAt = readiness.InitializedAt
        });
    }

    return Results.Json(new
    {
        status = "Unhealthy",
        database = "Disconnected",
        error = readiness.FailureReason ?? "Database initialization has not completed.",
        lastCheckedAt = readiness.InitializedAt ?? DateTime.UtcNow
    }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapControllers();
app.MapHub<JobFlowHub>("/hubs/jobflow");

app.Run();
