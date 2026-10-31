using System;
using System.Diagnostics;
using System.Threading;

namespace Prison.Core
{
    public record PrisonerSnapshot(string Name, string TypeName, int Energy);

    public class TaskReportItem
    {
        public string PrisonerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ResultOrError { get; set; } = string.Empty;
        public long DurationMs { get; set; }
    }

    public static class SimulationService
    {
        public static TaskReportItem RunHeavySimulation(PrisonerSnapshot snapshot, int workload, bool simulateError, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            var report = new TaskReportItem
            {
                PrisonerName = $"{snapshot.Name} ({snapshot.TypeName})"
            };

            try
            {
                if (simulateError)
                {
                    throw new InvalidOperationException($"Critical failure in calculation matrix for {snapshot.Name}!");
                }

                double accumulator = 0.0;
                for (int i = 0; i < workload; i++)
                {
                    token.ThrowIfCancellationRequested();
                    accumulator += Math.Sqrt(i) * Math.Sin(snapshot.Energy + i);
                }

                sw.Stop();
                report.Status = "Success";
                report.DurationMs = sw.ElapsedMilliseconds;
                report.ResultOrError = $"Completed ({workload:N0} steps). Score: {Math.Abs((long)accumulator % 1000)}";
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                report.Status = "Canceled";
                report.DurationMs = sw.ElapsedMilliseconds;
                report.ResultOrError = "Calculation was canceled by user.";
            }
            catch (Exception ex)
            {
                sw.Stop();
                report.Status = "Error";
                report.DurationMs = sw.ElapsedMilliseconds;
                report.ResultOrError = ex.Message;
            }

            return report;
        }
    }
}