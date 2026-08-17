using System.Text;
using LivingBank.Api.Configuration;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Integrations.EnableBanking;
using LivingBank.Api.Jobs;
using LivingBank.Api.Middleware;
using LivingBank.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuração ----
builder.Services.Configure<EnableBankingOptions>(builder.Configuration.GetSection(EnableBankingOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

// ---- Base de dados ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---- Identity ----
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// ---- Autenticação JWT ----
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };
    });

// ---- Autorização por permissões (mapeadas para roles) ----
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Permissions.ManageUsers, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Permissions.ManageBankAccounts, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Permissions.ViewTransactions, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Viewer));
    options.AddPolicy(Permissions.ForceSync, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Permissions.ViewLogs, p => p.RequireRole(Roles.Admin));
});

// ---- CORS (frontend web + mobile Capacitor) ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// ---- Serviços de aplicação ----
builder.Services.AddHttpClient<IEnableBankingClient, EnableBankingClient>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ---- Quartz (cron interno, verifica os 4 horários configurados a cada 5 min) ----
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ScheduledSyncJob");
    q.AddJob<ScheduledSyncJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ScheduledSyncJob-trigger")
        .WithSimpleSchedule(s => s.WithIntervalInMinutes(5).RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ---- Migrações + seed automático no arranque ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Render/Fly.io terminam TLS num proxy à frente do container; sem isto o
    // esquema chegaria sempre como http e UseHsts/redirect entrariam em loop.
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditLoggingMiddleware>();

app.MapControllers();

app.Run();
