﻿using DoEverything.Commands;
using Nice3point.Revit.Toolkit.External;
using Serilog;
using Serilog.Events;

namespace DoEverything
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateLogger();
            CreateRibbon();
        }

        public override void OnShutdown()
        {
            Log.CloseAndFlush();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "DoEverything");

            panel.AddPushButton<StartupCommand>("Execute")
                .SetImage("/DoEverything;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DoEverything;component/Resources/Icons/RibbonIcon32.png");
        }

        private static void CreateLogger()
        {
            const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
                .MinimumLevel.Debug()
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                Log.Fatal(exception, "Domain unhandled exception");
            };
        }
    }
}