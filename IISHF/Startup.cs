using IISHF.Core.Configurations;
using IISHF.Core.Interfaces;
using IISHF.Core.Services;
using IISHF.Core.Settings;
using Umbraco.Cms.Core.Services;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web hosting environment.</param>
        /// <param name="config">The configuration.</param>
        /// <remarks>
        /// Only a few services are possible to be injected here https://github.com/dotnet/aspnetcore/issues/9337.
        /// </remarks>
        public Startup(IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _env = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <remarks>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940.
        /// </remarks>
        public void ConfigureServices(IServiceCollection services)
        {
            var apiKeySettings = new ApiKeySettings();
            _config.Bind("ApiKeySettings", apiKeySettings);
            services.AddSingleton(apiKeySettings);

            services.AddScoped<IHttpClient, HttpClient>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITournamentService, TournamentService>();
            services.AddScoped<IEventResultsService, EventResultsService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddScoped<IRosterService, RosterService>();
            services.AddScoped<IContactServices, ContactServices>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IInvitationService, InvitationService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IMessageSender, ServiceBusMessagingService>();

            services.Configure<SendGridConfiguration>
                (_config.GetSection("SendGridConfiguration"));

            services.Configure<EmailConfiguration>
                (_config.GetSection("EmailSettings"));

            services.Configure<ServiceBusSettings>
                (_config.GetSection("ServiceBusSettings"));

            services.AddUmbraco(_env, _config)
                .AddBackOffice()
                .AddWebsite()
                .AddDeliveryApi()
                .AddComposers()
                .Build();

            services.AddMvc()
           .AddViewOptions(options =>
           {
               options.HtmlHelperOptions.ClientValidationEnabled = true;
           });
        }

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The web hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

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
                });
        }
    }
}
