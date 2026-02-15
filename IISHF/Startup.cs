using IISHF.Core.Configurations;
using IISHF.Core.Hubs;
using IISHF.Core.Interfaces;
using IISHF.Core.Services;
using IISHF.Core.Settings;
using IISHF.ExcelToPdf.Interfaces;
using Umbraco.Cms.Core.Services;
using Umbraco.StorageProviders.AzureBlob;
using FileService = IISHF.Core.Services.FileService;
using HttpClient = IISHF.Core.Services.HttpClient;
using IMediaService = IISHF.Core.Interfaces.IMediaService;
using IUserService = IISHF.Core.Interfaces.IUserService;
using MediaService = IISHF.Core.Services.MediaService;

namespace IISHF
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public Startup(IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _env = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Bind and register settings
            var apiKeySettings = new ApiKeySettings();
            _config.Bind("ApiKeySettings", apiKeySettings);
            services.AddSingleton(apiKeySettings);

            services.Configure<SendGridConfiguration>(_config.GetSection("SendGridConfiguration"));
            services.Configure<EmailConfiguration>(_config.GetSection("EmailSettings"));
            services.Configure<ServiceBusSettings>(_config.GetSection("ServiceBusSettings"));

            // Dependency injection for services
            services.AddScoped<IHttpClient, HttpClient>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IApprovals, ApprovalService>();
            services.AddScoped<ITournamentService, TournamentService>();
            services.AddScoped<IEventResultsService, EventResultsService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddScoped<IRosterService, RosterService>();
            services.AddScoped<IContactServices, ContactServices>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IInvitationService, InvitationService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IMessageSender, ServiceBusMessagingService>();
            services.AddScoped<INMAService, NMAService>();
            services.AddScoped<IUserInvitationService, UserInvitationService>();
            services.AddScoped<Core.Interfaces.IFileService, FileService>();
            services.AddScoped<IExcelToPdf, ExcelToPdf.Services.ExcelToPdf>();

            // Umbraco and Azure Blob integration
            services.AddUmbraco(_env, _config)
                .AddBackOffice()
                .AddWebsite()
                .AddDeliveryApi()
                .AddComposers()
                //.AddAzureBlobMediaFileSystem()
                //.AddAzureBlobImageSharpCache()
                .Build();

            // MVC and SignalR
            services.AddControllersWithViews();
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Exception handling and HTTPS
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // Forwarded headers (important for NGINX/Azure)
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                   Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
                KnownProxies = { System.Net.IPAddress.Parse("127.0.0.1") } // adjust if needed
            });

            // Static files and routing
            app.UseStaticFiles();
            app.UseRouting();

            // Umbraco & SignalR endpoints
            app.UseUmbraco()
                .WithMiddleware(u =>
                {
                    u.UseBackOffice();
                    u.UseWebsite();
                })
                .WithEndpoints(u =>
                {
                    u.UseInstallerEndpoints();
                    u.UseBackOfficeEndpoints();
                    u.UseWebsiteEndpoints();

                    // Azure SignalR Hub
                    u.EndpointRouteBuilder.MapHub<DataHub>("/dataHub");
                });
        }
    }
}
