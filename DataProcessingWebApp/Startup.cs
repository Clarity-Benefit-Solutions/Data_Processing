using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using DataProcessing;
using Hangfire.Dashboard;
using DataProcessingWebApp;
using DataProcessingWebApp.Jobs;
using Hangfire;
using Hangfire.Common;
using Hangfire.Console;
using Hangfire.SqlServer;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

[assembly: OwinStartup(typeof(Startup))]

namespace DataProcessingWebApp
{
    public class Startup
    {
        // keep hangfire logs for N days
        public class ProlongExpirationTimeAttribute : JobFilterAttribute, IApplyStateFilter
        {
            public void OnStateApplied(ApplyStateContext filterContext, IWriteOnlyTransaction transaction)
            {
                filterContext.JobExpirationTimeout = TimeSpan.FromDays(30);

            }
            public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                context.JobExpirationTimeout = TimeSpan.FromDays(30);
            }
        }


        public static IEnumerable<IDisposable> GetHangfireConfiguration()
        {
            var vars = new DataProcessing.Vars();
            //
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("App", "DataProcessingWebApp")
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Verbose()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
                //.WriteTo.File(restrictedToMinimumLevel: LogEventLevel.Verbose, path: $"{Vars.GetProcessBaseDir()}/../_Logs/DataProcessingWebAppVerbose.log")
                //.WriteTo.File(restrictedToMinimumLevel: LogEventLevel.Warning, path: $"{Vars.GetProcessBaseDir()}/../_Logs/DataProcessingWebAppWarnings.log")
                .CreateLogger();

            Log.Warning("Starting Hangfire Configuration");
            
            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSerilogLogProvider()
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseConsole(new ConsoleOptions { FollowJobRetentionPolicy = true, TimestampColor = "Red" })
                .UseSqlServerStorage(vars.ConnStrNameHangfire, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                    EnableHeavyMigrations = true
                });

            GlobalJobFilters.Filters.Add(new ProlongExpirationTimeAttribute());

            Log.Warning("Completed Hangfire Configuration");

            yield return new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = 4,
                StopTimeout = TimeSpan.FromSeconds(5)
            });
        }

        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();

            app.UseHangfireAspNet(GetHangfireConfiguration);
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                // ReSharper disable once UseArrayEmptyMethod
                Authorization = new IDashboardAuthorizationFilter[0]
            });

            //RecurringJob.AddOrUpdate<SnippetHighlighter>(
            //    "SnippetHighlighter.CleanUp",
            //    x => x.CleanUpAsync(),
            //    Cron.Daily);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfireServer();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

    }
}