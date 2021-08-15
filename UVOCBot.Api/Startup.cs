using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System;
using UVOCBot.Api.Workers;
using UVOCBot.Core;

namespace UVOCBot.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DatabaseOptions>(Configuration.GetSection(DatabaseOptions.ConfigSectionName));

            DatabaseOptions dbOptions = services.BuildServiceProvider().GetRequiredService<IOptions<DatabaseOptions>>().Value;
            services.AddDbContext<DiscordContext>
            (
                options =>
                {
                    options.UseMySql(
                    dbOptions.ConnectionString,
                    new MariaDbServerVersion(new Version(dbOptions.DatabaseVersion)))
#if DEBUG
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors();
#else
                        ;
#endif
                    }
            );

            services.AddControllers();
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "UVOCBot.Api", Version = "v1" }));
            services.AddHostedService<CleanupWorker>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "UVOCBot.Api v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
