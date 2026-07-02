using Serilog;
using Yasf.Common.Reporting.Contracts;

namespace Yasf.Common.ExtentReports
{
    public class TestRunReporter : ITestRunReporter
    {
        public string RootOutputFolder { get; }
        public string TestRunIdentity { get; }
        public string ReportOutputFolder => System.IO.Path.Combine(RootOutputFolder, TestRunIdentity);
        internal AventStack.ExtentReports.ExtentReports ExtentReporter { get; private set; }
        private AventStack.ExtentReports.Reporter.ExtentSparkReporter _htmlReporter;

        public TestRunReporter(string rootOutputFolder, string testRunIdentity)
        {
            RootOutputFolder = rootOutputFolder ?? throw new System.ArgumentNullException(nameof(rootOutputFolder));
            TestRunIdentity = testRunIdentity ?? throw new System.ArgumentNullException(nameof(testRunIdentity));
        }

        public void Initialize()
        {
            ExtentReporter = new AventStack.ExtentReports.ExtentReports();

            // NOTE: For some reason, I need to specify the path here or it goes 'one level up'. Am I missing something?
            _htmlReporter = new AventStack.ExtentReports.Reporter.ExtentSparkReporter(System.IO.Path.Combine(ReportOutputFolder, "index.html"));

            ExtentReporter.AttachReporter(_htmlReporter);
        }

        public void Uninitialize()
        {
            ExtentReporter.Flush();
            ExtentReporter = null;
        }

        public ITestCaseReporter CreateTestCaseReporter(ILogger logger, ITestCaseReporterContext testCaseReporterContext)
        {
            return new TestCaseReporter(logger, this, testCaseReporterContext.LogFilePath);
        }
    }
}
