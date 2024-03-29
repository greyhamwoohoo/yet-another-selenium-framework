﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Serilog;
using System;
using System.IO;
using System.Linq;
using Yasf.Common.ExecutionContext.Contracts;
using Yasf.Common.ExecutionContext.Runtime;
using Yasf.Common.ExecutionContext.Runtime.BrowserSettings;
using Yasf.Common.ExecutionContext.Runtime.BrowserSettings.Contracts;
using Yasf.Common.ExecutionContext.Runtime.ControlSettings;
using Yasf.Common.ExecutionContext.Runtime.DeviceSettings;
using Yasf.Common.ExecutionContext.Runtime.DeviceSettings.Contracts;
using Yasf.Common.ExecutionContext.Runtime.EnvironmentSettings;
using Yasf.Common.ExecutionContext.Runtime.InstrumentationSettings;
using Yasf.Common.ExecutionContext.Runtime.RemoteWebDriverSettings;
using Yasf.Common.Reporting;
using Yasf.Common.Reporting.Contracts;
using Yasf.Common.SessionManagement;
using Yasf.Common.SessionManagement.Contracts;

namespace Yasf.Common.Infrastructure
{
    /// <summary>
    /// TODO: This is becoming a monster. Refactor. 
    /// </summary>
    public static class ContainerSingleton
    {
        private static IServiceProvider _instance;
        private static object locker = new object();

        public static IServiceProvider Instance
        {
            get
            {
                lock (locker)
                {
                    if (_instance == null) throw new InvalidOperationException($"You must call Container.Initialize exactly once to initialize the container. ");

                    return _instance;
                }
            }
        }
        public static void Initialize(ILogger bootstrappingLogger, string prefix)
        {
            Initialize(bootstrappingLogger, prefix, (prefix, services, testRunReporterContext) => { });
        }

        public static void Initialize(ILogger bootstrappingLogger, string prefix, Action<string, IServiceCollection, ITestRunReporterContext> beforeContainerBuild)
        {
            if (_instance != null) throw new InvalidOperationException($"The Initialize method has already been called. ");
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
            if (beforeContainerBuild == null) throw new ArgumentNullException(nameof(beforeContainerBuild));

            Environment.SetEnvironmentVariable("TEST_OUTPUT_FOLDER", Directory.GetCurrentDirectory(), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("TEST_DEPLOYMENT_FOLDER", Directory.GetCurrentDirectory(), EnvironmentVariableTarget.Process);

            //
            // TODO: When this gets too big, look at Factories
            //

            var services = new ServiceCollection();

            RegisterDeviceSettings(bootstrappingLogger, prefix, services);
            RegisterBrowserSettings(bootstrappingLogger, prefix, services);

            var instrumentationSettings = ConfigureSettings<IInstrumentationSettings, InstrumentationSettings>(bootstrappingLogger, prefix, "InstrumentationSettings", "common.json", "instrumentationSettings", services);

            RegisterSettings<RemoteWebDriverSettings>(bootstrappingLogger, prefix, "RemoteWebDriverSettings", "common-localhost-selenium.json", "remoteWebDriverSettings", services, registerInstance: true);
            RegisterSettings<EnvironmentSettings>(bootstrappingLogger, prefix, "EnvironmentSettings", "internet.json", "environmentSettings", services, registerInstance: true);
            ConfigureSettings<IControlSettings, ControlSettings>(bootstrappingLogger, prefix, "ControlSettings", "common.json", "controlSettings", services);

            // Clear the variables so they do not creep into the rest of our implementation
            Environment.SetEnvironmentVariable("TEST_OUTPUT_FOLDER", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("TEST_DEPLOYMENT_FOLDER", null, EnvironmentVariableTarget.Process);

            // Singletons: statics that are instantiated once for the lifetime of the entire test run
            services.AddSingleton<IDriverSessionFactory, DriverSessionFactory>();

            var testRunReporterContext = new TestRunReporterContext()
            {
                InstrumentationSettings = instrumentationSettings,
                RootReportingFolder = instrumentationSettings.RootReportingFolder,
                TestRunIdentity = DateTime.Now.ToString("yyyyMMdd-HHmmss")
            };

            services.AddSingleton<ITestRunReporterContext>(testRunReporterContext);

            // Scoped: per test
            services.AddScoped(isp =>
            {
                var serilogContext = BuildSerilogConfiguration();

                var logPath = isp.GetRequiredService<ITestCaseReporterContext>().LogFilePath;

                LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(serilogContext)
                    .Enrich
                    .FromLogContext();

                if (isp.GetRequiredService<IInstrumentationSettings>().LogFilePerTest)
                {
                    loggerConfiguration.WriteTo
                        .File(logPath);
                };

                ILogger logger = loggerConfiguration.CreateLogger();

                return logger;
            });

            services.AddScoped<ICommandExecutor>(isp =>
            {
                var remoteWebDriverSettings = isp.GetRequiredService<RemoteWebDriverSettings>();

                var commandExecutor = new HttpCommandExecutor(new Uri(remoteWebDriverSettings.RemoteUri), TimeSpan.FromSeconds(remoteWebDriverSettings.HttpCommandExecutorTimeoutInSeconds));

                return commandExecutor;
            });

            services.AddScoped(isp =>
            {
                var factory = isp.GetRequiredService<IDriverSessionFactory>();
                var browserProperties = isp.GetRequiredService<IBrowserProperties>();
                var remoteWebDriverSettings = isp.GetRequiredService<RemoteWebDriverSettings>();
                var environmentSettings = isp.GetRequiredService<EnvironmentSettings>();
                var controlSettings = isp.GetRequiredService<IControlSettings>();
                var deviceSettings = isp.GetRequiredService<IDeviceProperties>();
                var logger = isp.GetRequiredService<ILogger>();
                var testCaseReporter = isp.GetRequiredService<ITestCaseReporter>();
                var httpCommandExecutor = isp.GetRequiredService<ICommandExecutor>();

                var driverSession = factory.Create(deviceSettings, browserProperties, remoteWebDriverSettings, environmentSettings, controlSettings, logger, testCaseReporter, httpCommandExecutor);
                return driverSession;
            });

            beforeContainerBuild(prefix, services, testRunReporterContext);

            var reportingContextManager = new ReportingContextRegistrationManager(bootstrappingLogger, services, testRunReporterContext);

            reportingContextManager.AssertIsNotPartiallyConfigured();
            if (!reportingContextManager.IsConfigured)
            {
                reportingContextManager.PopulateDefaultReportingContexts();
            }

            _instance = services.BuildServiceProvider();
        }

        private static void RegisterDeviceSettings(ILogger logger, string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettings(logger);
            var paths = runtimeSettingsUtilities.CalculatePathsOfSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "DeviceSettings", "common-desktop-selenium.json");
            var configurationRoot = runtimeSettingsUtilities.BuildConfiguration(prefix, paths);

            var platformName = configurationRoot.GetSection("platformName")?.Value?.ToUpper();

            switch (platformName)
            {
                case "DESKTOP":
                    var instance = new DesktopSettings();

                    instance.PlatformName = platformName;

                    services.AddSingleton(instance);
                    services.AddSingleton<IDeviceProperties>(instance);
                    break;
                case "ANDROID":
                    var androidSettings = new AppiumSettings();

                    configurationRoot.Bind(androidSettings);

                    androidSettings = SubstituteEnvironmentVariables(androidSettings);

                    androidSettings.PlatformName = platformName;
                    androidSettings.Cleanse();

                    services.AddSingleton(androidSettings);
                    services.AddSingleton<IDeviceProperties>(androidSettings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"The device called {platformName} is currently not supported. ");
            }
        }

        private static void RegisterBrowserSettings(ILogger logger, string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettings(logger);
            var paths = runtimeSettingsUtilities.CalculatePathsOfSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "BrowserSettings", "common-chrome.json");
            var configurationRoot = runtimeSettingsUtilities.BuildConfiguration(prefix, paths);

            var browserName = configurationRoot.GetSection("browserName")?.Value?.ToUpper();
            var browserSettings = configurationRoot.GetSection("browserSettings");

            switch (browserName)
            {
                case "CHROME":
                    var instance = new ChromeBrowserSettings();

                    browserSettings.Bind(instance);

                    instance = SubstituteEnvironmentVariables(instance);

                    instance.BrowserName = browserName;
                    instance.Cleanse();

                    services.AddSingleton(instance);
                    services.AddSingleton<IBrowserProperties>(instance);
                    break;
                case "EDGE":
                    var edgeInstance = new EdgeBrowserSettings();

                    browserSettings.Bind(edgeInstance);

                    edgeInstance = SubstituteEnvironmentVariables(edgeInstance);

                    edgeInstance.BrowserName = browserName;
                    edgeInstance.Cleanse();

                    services.AddSingleton(edgeInstance);
                    services.AddSingleton<IBrowserProperties>(edgeInstance);
                    break;
                case "FIREFOX":
                    var ffInstance = new FireFoxBrowserSettings();

                    browserSettings.Bind(ffInstance);

                    ffInstance = SubstituteEnvironmentVariables(ffInstance);

                    ffInstance.BrowserName = browserName;
                    ffInstance.Cleanse();

                    services.AddSingleton(ffInstance);
                    services.AddSingleton<IBrowserProperties>(ffInstance);
                    break;
                case "INTERNETEXPLORER":
                    var ieInstance = new InternetExplorerBrowserSettings();

                    browserSettings.Bind(ieInstance);

                    ieInstance = SubstituteEnvironmentVariables(ieInstance);

                    ieInstance.BrowserName = browserName;
                    ieInstance.Cleanse();

                    services.AddSingleton(ieInstance);
                    services.AddSingleton<IBrowserProperties>(ieInstance);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"The browser called {browserName} is currently not supported. ");
            }
        }

        private static T RegisterSettings<T>(ILogger logger, string prefix, string settingsFolderName, string defaultFilename, string settingsName, IServiceCollection services, bool registerInstance = false) where T : class, new()
        {
            var runtimeSettingsUtilities = new RuntimeSettings(logger);
            var paths = runtimeSettingsUtilities.CalculatePathsOfSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), settingsFolderName, defaultFilename);
            var configurationRoot = runtimeSettingsUtilities.BuildConfiguration(prefix, paths);

            var controlSettings = configurationRoot.GetSection(settingsName);

            var instance = new T();

            controlSettings.Bind(instance);

            instance = SubstituteEnvironmentVariables(instance);

            var cleansor = instance as ICleanse;
            if (cleansor != null)
            {
                cleansor.Cleanse();
            }

            if (registerInstance)
            {
                services.AddSingleton(isp => instance);
            }

            return instance;
        }

        private static TI ConfigureSettings<TI, T>(ILogger logger, string prefix, string settingsFolderName, string defaultFilename, string settingsName, IServiceCollection services) where T : class, TI, new() where TI : class
        {
            var instance = RegisterSettings<T>(logger, prefix, settingsFolderName, defaultFilename, settingsFolderName, services, registerInstance: false);

            services.AddSingleton<TI, T>(isp => instance);

            return instance;
        }

        /// <summary>
        /// Brute force any environment variables embedded within the properties of the object. 
        /// TODO: use a ConfigurationProvider to do this
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static T SubstituteEnvironmentVariables<T>(T value) where T : class
        {
            var variables = Environment.GetEnvironmentVariables();
            var content = JsonConvert.SerializeObject(value);
            if (content.Contains("%"))
            {
                foreach (var variable in variables.Keys.Cast<string>())
                {
                    var candidateString = JsonConvert.ToString(variables[variable]).Trim('"');
                    content = content.Replace($"%{variable.ToUpper()}%", candidateString);
                }

                return JsonConvert.DeserializeObject<T>(content);
            }
            else
            {
                return value;
            }
        }

        private static IConfigurationRoot BuildSerilogConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("serilogsettings.json", optional: false)
                .Build();

            return configuration;
        }
    }
}
