using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Yasf.Common.Reporting.Contracts;

namespace Yasf.Common.Infrastructure
{
    /// <summary>
    /// Core Framework Initialization 
    /// </summary>
    public static class MsTestInitialization
    {
        public const string TEST_EXECUTION_CONTEXT_KEY_NAME = "TestExecutionContext";

        static IServiceProvider _serviceProvider = default;
        static IEnumerable<ITestRunReporter> _testRunReporters = default;

        public static void Initialize(string prefix, string defaultTestExecutionContext, TestContext testContext)
        {
            Initialize(prefix, defaultTestExecutionContext, testContext, (prefix, serviceCollection, testRunReporterContext) => { });
        }

        public static void Initialize(string prefix, string defaultTestExecutionContext, TestContext testContext, Action<string, IServiceCollection, ITestRunReporterContext> callback)
        {
            if (null == prefix) throw new ArgumentNullException(nameof(prefix));
            if (null == defaultTestExecutionContext) throw new ArgumentNullException(nameof(defaultTestExecutionContext));
            if (null == testContext) throw new ArgumentNullException(nameof(testContext));
            if (null == callback) throw new ArgumentNullException(nameof(callback));

            // Initialize this early on (for bootstrapping) and keep it simple: console output only (MsTest will capture this)
            Log.Logger = new LoggerConfiguration()
                        .Enrich
                        .FromLogContext()
                        .WriteTo
                        .Console()
                        .CreateLogger();

            // The Text Execution Context (environment, variables) are chosen in this order:
            //
            // 1. {PREFIX}TEST_EXECUTION_CONTEXT
            // 2. DEFAULT_TEST_EXECUTION_CONTEXT
            // 3. .runsettings is the fallback (if in the solution root)
            var testExecutionContextName = $"{Environment.GetEnvironmentVariable($"{prefix}TEST_EXECUTION_CONTEXT") ?? defaultTestExecutionContext}";
            Log.Logger.Information($"Candidate Test Exection Context to use: {testExecutionContextName}");

            if (testContext.Properties.ContainsKey(TEST_EXECUTION_CONTEXT_KEY_NAME))
            {
                Log.Logger.Information($"The .runsettings contains a property called {TEST_EXECUTION_CONTEXT_KEY_NAME}. We will retrieve that. ");
                testExecutionContextName = Convert.ToString(testContext.Properties[TEST_EXECUTION_CONTEXT_KEY_NAME]);
            }

            Log.Logger.Information($"TestExecutionContextName to use: {testExecutionContextName}");

            var testExecutionContextFilename = $"tec.{testExecutionContextName}.json";
            Log.Logger.Information($"TestExecutionContextFilename to be used: {testExecutionContextFilename}");

            var testExecutionContext = TestExecutionContextFactory.Create(testExecutionContextFilename);

            var environmentVariables = testExecutionContext.EnvironmentVariables;
            environmentVariables.Keys.ToList().ForEach(ev =>
            {
                Log.Logger.Information($"Setting Environment Variable '{ev}' to '{environmentVariables[ev]}' for this process. ");

                Environment.SetEnvironmentVariable(ev, environmentVariables[ev], EnvironmentVariableTarget.Process);
            });

            Log.Logger.Information("START: To initialize singleton container. ");
            ContainerSingleton.Initialize(Log.Logger, prefix, (prefix, services, testRunReporterContext) =>
            {
                Log.Logger.Information($"    START: Callback before container is built. ");
                services.AddSingleton(testExecutionContext);
                Log.Logger.Information($"    END: Callback after container is built. ");

                callback(prefix, services, testRunReporterContext);
            });
            Log.Logger.Information("END: Initializing singleton container. ");

            _serviceProvider = ContainerSingleton.Instance;

            _testRunReporters = _serviceProvider.GetServices<ITestRunReporter>();
            _testRunReporters.ToList().ForEach(testRunReporter =>
            {
                testRunReporter.Initialize();
            });
        }

        public static void Uninitialize()
        {
            _testRunReporters?.ToList().ForEach(testRunReporter =>
            {
                testRunReporter.Uninitialize();
            });
        }
    }
}
