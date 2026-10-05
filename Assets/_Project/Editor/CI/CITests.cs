using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Arash.Editor.Setup
{
    /// <summary>
    /// Runs the EditMode tests inside the build's own Unity session, so the cloud build needs only
    /// one licensed Unity run for tests and the APK. Lives in its own assembly because it only
    /// compiles once the Test Framework package is installed.
    /// </summary>
    [InitializeOnLoad]
    static class CITests
    {
        static CITests()
        {
            CIBuild.RunTests = Run;
        }

        static bool Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callbacks = new Callbacks();
            api.RegisterCallbacks(callbacks);
            var settings = new ExecutionSettings(new Filter { testMode = TestMode.EditMode }) { runSynchronously = true };
            api.Execute(settings);
            api.UnregisterCallbacks(callbacks);

            Debug.Log(callbacks.Report.ToString());
            return callbacks.Finished && callbacks.Failed == 0;
        }

        class Callbacks : ICallbacks
        {
            public readonly StringBuilder Report = new StringBuilder();
            public bool Finished;
            public int Failed;

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Finished = true;
                Failed = result.FailCount;
                Report.Insert(0, $"[Arash CI] Tests: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.\n");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                    Report.AppendLine($"[Arash CI] FAILED {result.FullName}: {result.Message}\n{result.StackTrace}");
            }
        }
    }
}
