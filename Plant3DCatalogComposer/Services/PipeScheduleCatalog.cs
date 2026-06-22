using System;
using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Pipe wall schedule codes for catalog project (ASME B36.10).</summary>
    public static class PipeScheduleCatalog
    {
        /// <summary>Default project schedule; matches standard BW fitting set (BW_SCH40).</summary>
        public const string Default = "40";

        public static IReadOnlyList<PipeScheduleOption> All { get; } =
        [
            new("40"),
            new("80"),
            new("40S"),
            new("80S"),
            new("10"),
            new("10S"),
            new("SDR17"),
            new("SDR11"),
        ];

        public static string Normalize(string? schedule)
        {
            string? known = TryNormalize(schedule);
            return known ?? Default;
        }

        public static string NormalizeOrEmpty(string? schedule) =>
            TryNormalize(schedule) ?? "";

        private static string? TryNormalize(string? schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
                return null;

            string s = schedule.Trim();
            foreach (PipeScheduleOption opt in All)
            {
                if (opt.Id.Equals(s, StringComparison.OrdinalIgnoreCase))
                    return opt.Id;
            }

            if (s.Equals("Sch40", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Sch-40", StringComparison.OrdinalIgnoreCase))
                return "40";
            if (s.Equals("Sch80", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Sch-80", StringComparison.OrdinalIgnoreCase))
                return "80";
            if (s.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                return Default;

            return null;
        }

        /// <summary>True when part has no schedule tag or matches project schedule.</summary>
        public static bool PartMatches(string projectSchedule, string? partSchedule)
        {
            if (string.IsNullOrWhiteSpace(partSchedule))
                return true;

            return Normalize(projectSchedule)
                .Equals(Normalize(partSchedule), StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class PipeScheduleOption(string id)
    {
        public string Id { get; } = id;

        public string Display => Id;

        public override string ToString() => Id;
    }
}
