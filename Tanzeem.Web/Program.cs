using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Persistence;
using Tanzeem.Persistence.Data.DbContexts;
using Tanzeem.Persistence.Data.Migrations;
using Tanzeem.Presentation;
using Tanzeem.Services.Abstractions.AI;
using Tanzeem.Services.Abstractions.Alerts;
using Tanzeem.Services.Abstractions.AuditLogs;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Services.Abstractions.Billing;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Services.Abstractions.Companies;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Dashboard;
using Tanzeem.Services.Abstractions.DeliveryIssues;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Services.Abstractions.Onboarding;
using Tanzeem.Services.Abstractions.Orders;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Services.Abstractions.Settings;
using Tanzeem.Services.Abstractions.Suppliers;
using Tanzeem.Services.Abstractions.Transactions;
using Tanzeem.Services.Alerts;
using Tanzeem.Services.AuditLogs;
using Tanzeem.Services.Authentication;
using Tanzeem.Services.Billing;
using Tanzeem.Services.Branches;
using Tanzeem.Services.BusinessCore;
using Tanzeem.Services.Companies;
using Tanzeem.Services.Current;
using Tanzeem.Services.Dashboard;
using Tanzeem.Services.DeliveryIssues;
using Tanzeem.Services.Notifications;
using Tanzeem.Services.Onboarding;
using Tanzeem.Services.Orders;
using Tanzeem.Services.Products;
using Tanzeem.Services.Settings;
using Tanzeem.Services.Suppliers;
using Tanzeem.Services.Transactions;
using Tanzeem.Shared;

namespace Tanzeem.Web {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
                ApplyStripeEnvironmentAliases(builder.Configuration);
            }

            var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var databaseProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
            var usesSqlite = databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

            if (builder.Environment.IsDevelopment() &&
                !builder.Configuration.GetValue("DatabaseSafety:AllowSharedDevelopmentDatabase", false) &&
                IsSharedDevelopmentDatabase(defaultConnectionString))
            {
                throw new InvalidOperationException(
                    "Refusing to start in Development with a shared database connection. " +
                    "Use an isolated local/test database, or set DatabaseSafety:AllowSharedDevelopmentDatabase=true only when intentional.");
            }

            // Add services to the container.

            #region Added Services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ProductHelperService>();
            
            builder.Services.AddScoped<ICompanyService, CompanyService>();
            builder.Services.AddScoped<IBranchService, BranchService>();
            
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<TransactionHelperService>();
            
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IBillingService, BillingService>();
            builder.Services.AddScoped<IBusinessCoreService, BusinessCoreService>();
            builder.Services.AddScoped<IOnboardingService, OnboardingService>();
            
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
            builder.Services.AddScoped<ICurrentService, CurrentService>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<ISupplierService, SupplierService>();
            builder.Services.AddScoped<IOrderService,OrderService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IAlertService, AlertService>();
            builder.Services.AddScoped<IAlertConfigurationsService, AlertConfigurationsService>();
            builder.Services.AddScoped<IDeliveryIssuesService, DeliveryIssuesService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IDemandForecastingService, DemandForecastingService>();
            builder.Services.AddHttpClient<DemandForecastingService>();
            builder.Services.AddScoped<IAIConfigService, AIConfigurationsService>();
            builder.Services.AddScoped<IAuditLogsService, AuditLogsService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            #endregion

            #region Added Authentication

            var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>()!;

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var sessionKey = context.Principal?.FindFirst("SessionId")?.Value;
                            var userIdValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                            if (string.IsNullOrWhiteSpace(sessionKey) ||
                                !int.TryParse(userIdValue, out var userId))
                            {
                                context.Fail("Login session is missing.");
                                return;
                            }

                            var dbContext = context.HttpContext.RequestServices.GetRequiredService<TanzeemDbContext>();
                            var now = DateTime.UtcNow;
                            var session = await dbContext.UserSessions
                                .FirstOrDefaultAsync(s => s.SessionKey == sessionKey && s.UserId == userId);

                            if (session is null || session.RevokedAt != null || session.ExpiresAt <= now)
                            {
                                context.Fail("Login session is no longer active.");
                                return;
                            }

                            if (session.LastSeenAt <= now.AddMinutes(-5))
                            {
                                session.LastSeenAt = now;
                                await dbContext.SaveChangesAsync();
                            }
                        }
                    };
                });

            #endregion

            #region Added Hangfire
            var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true) && !usesSqlite;
            if (hangfireEnabled) {
                builder.Services.AddHangfire(config => config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer() //when create job use simple service name not full name with version and Public Key Token
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(defaultConnectionString));

                builder.Services.AddHangfireServer();
            }
            #endregion

            #region Add CORS

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                            "https://tanzeem.runasp.net",
                            "https://tanzeem.runasp.net/",
                            "https://tanzeem-self.vercel.app",
                            "https://tanzeem-self.vercel.app/",
                            "https://tanzeem-ims.vercel.app",
                            "https://tanzeem-ims.vercel.app/",
                            "http://localhost:5173",
                            "https://localhost:5173",
                            "http://localhost:5174",
                            "https://localhost:5174",
                            "http://127.0.0.1:5173",
                            "https://127.0.0.1:5173",
                            "http://127.0.0.1:5174",
                            "https://127.0.0.1:5174")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            #endregion
            
            #region Controller & swagger

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            #endregion

            #region DB Connection

            builder.Services.AddDbContext<TanzeemDbContext>(options => {
                if (usesSqlite)
                {
                    options.UseSqlite(ResolveSqliteConnectionString(defaultConnectionString, builder.Environment.ContentRootPath));
                }
                else
                {
                    options.UseSqlServer(defaultConnectionString);
                }
            });

            #endregion

            var app = builder.Build();

            if (app.Environment.IsDevelopment() && usesSqlite)
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TanzeemDbContext>();
                dbContext.Database.EnsureCreated();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            #region background services
            if (hangfireEnabled) {
                using (var scope = app.Services.CreateScope())
                {
                    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    recurringJobManager.AddOrUpdate(
                        "check-inventory-weekly",
                        () => notificationService.CreateNotification(),
                        Cron.Weekly(DayOfWeek.Saturday, 1)
                    );
                    //recurringJobManager.AddOrUpdate<DemandForecastingService>(
                    //        "update-ai-demand-forecast-daily",
                    //service => service.UpdateAllForecastsAsync(),
                    //Cron.Daily(23, 0));
                    recurringJobManager.AddOrUpdate<IDemandForecastingService>(
                        "update-ai-demand-forecast-dailyy",
                        service => service.UpdateAllForecastsAsync(),
                        Cron.Daily(23, 0));
                    //RecurringJob.RemoveIfExists("update-ai-demand-forecast-daily");
                }
            }
            #endregion

            #region Swagger Routing

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment()){}
            app.UseSwagger();
            app.UseSwaggerUI(options => {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
                options.RoutePrefix = "swagger";
            });
            app.MapGet("/", () => Results.Redirect("/swagger"));

            #endregion

            if (hangfireEnabled) {
                app.UseHangfireDashboard("/hangfire"); // move it after auth middlewares -- at production phase
            }
            if (!app.Environment.IsDevelopment()) {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

        private static bool IsSharedDevelopmentDatabase(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            return connectionString.Contains("databaseasp.net", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("db41970", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveSqliteConnectionString(string? connectionString, string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "Data Source=.local/tanzeem-dev.db";

            var builder = new SqliteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource))
                builder.DataSource = ".local/tanzeem-dev.db";

            if (!Path.IsPathRooted(builder.DataSource) && builder.DataSource != ":memory:")
                builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));

            var directory = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            return builder.ConnectionString;
        }

        private static void ApplyStripeEnvironmentAliases(IConfiguration configuration)
        {
            SetIfPresent(configuration, "StripeOptions:SecretKey", "STRIPE_SECRET_KEY");
            SetIfPresent(configuration, "StripeOptions:PublishableKey", "STRIPE_PUBLISHABLE_KEY");
            SetIfPresent(configuration, "StripeOptions:WebhookSecret", "STRIPE_WEBHOOK_SECRET");
            SetIfPresent(configuration, "StripeOptions:DefaultPriceId", "STRIPE_DEFAULT_PRICE_ID");
        }

        private static void SetIfPresent(IConfiguration configuration, string configurationKey, string environmentKey)
        {
            var value = Environment.GetEnvironmentVariable(environmentKey);
            if (!string.IsNullOrWhiteSpace(value))
                configuration[configurationKey] = value;
        }
    }
}
