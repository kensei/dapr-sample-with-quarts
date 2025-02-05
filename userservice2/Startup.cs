﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using DaprSample.MicroService.UsersService2.Services;
using DaprSample.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DaprSample.MicroService.UsersService2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
            services.AddDbContext<TestDbContext>(
                options => options
                    .UseMySql(
                        Configuration.GetConnectionString("UserContext"), 
                        serverVersion,
                        sqlOptions => {
                            sqlOptions.MigrationsAssembly("shared");
                        })
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors() // remove for production
                );
            var redisHost = Configuration.GetSection("RedisSettings").GetValue<string>("RedisHost");
            var config = new ConfigurationOptions()
            {
                EndPoints = { redisHost },
                KeepAlive = 180,
                ReconnectRetryPolicy = new ExponentialRetry(5000)
            };
            var redisDadabase = ConnectionMultiplexer.Connect(config).GetDatabase();
            services.AddSingleton<IDatabase>(redisDadabase);
            services.AddControllers().AddDapr();
            services.AddGrpc();
            services.AddGrpcReflection();

            services.AddScoped<UserServiceImpl>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<UserService>();

                if (env.IsDevelopment())
                {
                    endpoints.MapGrpcReflectionService();
                }

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
