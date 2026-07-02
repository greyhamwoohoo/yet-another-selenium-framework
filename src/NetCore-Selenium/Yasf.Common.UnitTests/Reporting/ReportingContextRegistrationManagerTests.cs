using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using Yasf.Common.Reporting;
using Yasf.Common.Reporting.Contracts;

namespace Yasf.Common.UnitTests.Reporting
{
    /// <summary>
    /// An experiment using Builders for Unit Test (perhaps overkill for such a simple class!)
    /// </summary>
    [TestClass]
    public class ReportingContextRegistrationManagerTests
    {

        internal class ManagerUnderTestBuilder
        {
            IServiceCollection _services = default;
            ILogger _logger = default;
            ITestRunReporterContext _testRunExecutionContext = default;

            internal ManagerUnderTestBuilder()
            {
                _services = new ServiceCollection();

                WithNoRegisteredServices();

                _logger = NSubstitute.Substitute.For<ILogger>();
                _testRunExecutionContext = NSubstitute.Substitute.For<ITestRunReporterContext>();
            }

            internal ManagerUnderTestBuilder WithNoRegisteredServices()
            {
                _services = new ServiceCollection();
                return this;
            }

            internal ManagerUnderTestBuilder WithTestCaseReporterContextRegistered()
            {
                _services.AddSingleton<ITestCaseReporterContext>();
                return this;
            }

            internal ManagerUnderTestBuilder WithTestCaseReporterRegistered()
            {
                _services.AddSingleton<ITestCaseReporter>();
                return this;
            }

            internal ManagerUnderTestBuilder WithTestRunReporterRegistered()
            {
                _services.AddSingleton<ITestRunReporter>();
                return this;
            }

            internal ManagerUnderTestBuilder WithAllRegisteredServices()
            {
                WithTestCaseReporterContextRegistered();
                WithTestRunReporterRegistered();
                WithTestCaseReporterRegistered();
                return this;
            }

            internal ReportingContextRegistrationManager Build()
            {
                return new ReportingContextRegistrationManager(_logger, _services, _testRunExecutionContext);
            }
        }

        [TestClass]
        public class WhenReportingIsNotRegisteredAtAll
        {
            ReportingContextRegistrationManager manager = default;

            [TestInitialize]
            public void Assumption()
            {
                manager = new ManagerUnderTestBuilder()
                    .WithNoRegisteredServices()
                    .Build();
            }

            [TestMethod]
            public void ReportingIsNotConfigured()
            {
                manager.IsConfigured.Should().BeFalse(because: "no report context services were injected. ");
            }

            [TestMethod]
            public void ReportingIsNotPartiallyConfigured()
            {
                manager.IsPartiallyConfigured.Should().BeFalse(because: "no report context services were injected");
            }

            [TestMethod]
            public void DefaultReportingCanBeConfigured()
            {
                manager.PopulateDefaultReportingContexts();

                manager.IsConfigured.Should().BeTrue(because: "the default reporting services should have been configured. ");
                manager.IsPartiallyConfigured.Should().BeFalse(because: "all of the reporting service were configured. So it is not partial. ");
            }

            [TestMethod]
            public void RegisteringTestRunReporterWillMakeItPartiallyConfigured()
            {
                manager = new ManagerUnderTestBuilder()
                    .WithTestRunReporterRegistered()
                    .Build();

                manager.IsPartiallyConfigured.Should().BeTrue(because: "we have registered a single service. ");
            }

            [TestMethod]
            public void RegisteringTestCaseReporterWillMakeItPartiallyConfigured()
            {
                manager = new ManagerUnderTestBuilder()
                    .WithTestCaseReporterRegistered()
                    .Build();

                manager.IsPartiallyConfigured.Should().BeTrue(because: "we have registered a single service. ");
            }

            [TestMethod]
            public void RegisteringTestCaseReporterContextWillMakeItPartiallyConfigured()
            {
                manager = new ManagerUnderTestBuilder()
                    .WithTestCaseReporterContextRegistered()
                    .Build();

                manager.IsPartiallyConfigured.Should().BeTrue(because: "we have registered a single service. ");
            }
        }


        [TestClass]
        public class WhenReportingIsFullyConfigured
        {
            ReportingContextRegistrationManager manager = default;

            [TestInitialize]
            public void Assumption()
            {
                manager = new ManagerUnderTestBuilder()
                    .WithAllRegisteredServices()
                    .Build();

                manager.IsConfigured.Should().BeTrue(because: "we are assuming that all services are registered. ");
            }

            [TestMethod]
            public void ReportingIsNotPartiallyConfigured()
            {
                manager.IsPartiallyConfigured.Should().BeFalse(because: "no report context services were injected");
            }

            [TestMethod]
            public void DefaultReportingCannotBeConfiguredAgain()
            {
                Assert.ThrowsExactly<InvalidOperationException>(() => manager.PopulateDefaultReportingContexts());
            }
        }

        [TestClass]
        public class WhenReportingIsPartiallyConfigured
        {
            ReportingContextRegistrationManager manager = default;

            [TestInitialize]
            public void Assumption()
            {
                manager = new ManagerUnderTestBuilder()
                    .WithTestCaseReporterContextRegistered()
                    .Build();
            }

            [TestMethod]
            public void ReportingIsPartiallyConfigured()
            {
                manager.IsPartiallyConfigured.Should().BeTrue(because: "only some of the services are registered. ");
            }

            [TestMethod]
            public void DefaultReportingCannotBeConfigured()
            {
                Assert.ThrowsExactly<InvalidOperationException>(() => manager.PopulateDefaultReportingContexts());
            }
        }
    }
}
