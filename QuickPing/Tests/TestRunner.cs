#if DEBUG
using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickPing.Tests
{
    /// <summary>
    /// Debug-only in-game tests. Run them in a loaded world (F5 console): quickping_test [name filter].
    /// Results are printed and written to BepInEx/QuickPingTests.txt.
    /// </summary>
    internal static class TestRunner
    {
        public const string ResultLinePrefix = "RESULT:";

        public static string ResultsPath => Path.Combine(Paths.BepInExRootPath, "QuickPingTests.txt");

        public static bool Running { get; private set; }

        public static void Register()
        {
            new Terminal.ConsoleCommand("quickping_test",
                "[QuickPing dev] Run the in-game tests (optional: name filter)",
                args => Start(args.ArgsAll.Trim(), null));
        }

        /// <param name="done">Called at the end with true when every test passed.</param>
        public static void Start(string filter, Action<bool> done)
        {
            if (Running)
            {
                Print("quickping_test: already running");
                return;
            }
            if (!Player.m_localPlayer || !Minimap.instance)
            {
                Print("quickping_test: load a world first");
                done?.Invoke(false);
                return;
            }
            QuickPingPlugin.Instance.StartCoroutine(Run(filter, done));
        }

        private static IEnumerator Run(string filter, Action<bool> done)
        {
            Running = true;
            var lines = new List<string>
            {
                $"QuickPing {MyPluginInfo.PLUGIN_VERSION} in-game tests, Valheim {(global::Version.GetVersionString())}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            };
            int passed = 0, failed = 0;

            foreach (InGameTests.Test test in InGameTests.All
                .Where(t => filter == "" || t.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var outcome = new Outcome();
                float start = UnityEngine.Time.realtimeSinceStartup;

                InGameTests.BeforeEach();
                yield return Guard(test.Body(), outcome);
                yield return Guard(InGameTests.AfterEach(), outcome);

                string line = outcome.Error == null
                    ? $"PASS  {test.Name} ({(UnityEngine.Time.realtimeSinceStartup - start) * 1000f:0} ms)"
                    : $"FAIL  {test.Name}: {outcome.Error}";
                if (outcome.Error == null) passed++; else failed++;
                lines.Add(line);
                Print(line);
            }

            string result = $"{ResultLinePrefix} {passed} passed, {failed} failed";
            lines.Add(result);
            Print(result);
            File.WriteAllLines(ResultsPath, lines);
            Print($"quickping_test: written to {ResultsPath}");

            Running = false;
            done?.Invoke(failed == 0 && passed > 0);
        }

        private class Outcome
        {
            public string Error;
        }

        /// <summary>
        /// Run a test coroutine and record its first exception (Unity coroutines can't be wrapped in try/catch).
        /// Nested coroutines yielded by the test are guarded the same way.
        /// </summary>
        private static IEnumerator Guard(IEnumerator body, Outcome outcome)
        {
            while (outcome.Error == null)
            {
                object current;
                try
                {
                    if (!body.MoveNext()) yield break;
                    current = body.Current;
                }
                catch (TestFailure e)
                {
                    outcome.Error = e.Message;
                    yield break;
                }
                catch (Exception e)
                {
                    outcome.Error = e.ToString();
                    yield break;
                }

                yield return current is IEnumerator nested ? Guard(nested, outcome) : current;
            }
        }

        private static void Print(string text)
        {
            QuickPingPlugin.Log.LogInfo(text);
            if (Console.instance) Console.instance.Print(text);
        }
    }

    internal class TestFailure : Exception
    {
        public TestFailure(string message) : base(message) { }
    }

    internal static class Assert
    {
        public static void True(bool condition, string message)
        {
            if (!condition) throw new TestFailure(message);
        }

        public static void Equal<T>(T expected, T actual, string what)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new TestFailure($"{what}: expected '{expected}', got '{actual}'");
        }

        public static T NotNull<T>(T value, string what) where T : class
        {
            if (value == null || (value is UnityEngine.Object obj && !obj))
                throw new TestFailure($"{what}: missing");
            return value;
        }
    }
}
#endif
