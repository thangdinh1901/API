using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Combined pressure class / pipe schedule choices for Part Family (Plant 3D Class/Sch).
    /// </summary>
    public sealed class ClassScheduleOption
    {
        public ClassScheduleOption(string id, string pressureClass, string pipeSchedule)
        {
            Id = id.Trim();
            PressureClass = pressureClass.Trim();
            PipeSchedule = pipeSchedule.Trim();
        }

        public string Id { get; }

        public string PressureClass { get; }

        public string PipeSchedule { get; }

        public override string ToString() => Id;
    }

    public static class CatalogClassScheduleOptions
    {
        private static readonly ClassScheduleOption[] PipeMaterial =
        [
            Opt("10", "150", "10"),
            Opt("10S", "150", "10S"),
            Opt("40", "150", "40"),
            Opt("40S", "150", "40S"),
            Opt("80", "150", "80"),
            Opt("80S", "150", "80S"),
            Opt("SDR17", "150", "SDR17"),
            Opt("SDR11", "150", "SDR11"),
        ];

        private static readonly ClassScheduleOption[] BwFitting =
        [
            Opt("40", "150", "40"),
            Opt("80", "150", "80"),
            Opt("SDR17", "150", "SDR17"),
            Opt("SDR11", "150", "SDR11"),
        ];

        private static readonly ClassScheduleOption[] SwFitting =
        [
            Opt("3000", "3000", ""),
            Opt("6000", "6000", ""),
        ];

        private static readonly ClassScheduleOption[] Flanged =
        [
            Opt("150", "150", ""),
            Opt("300", "300", ""),
            Opt("600", "600", ""),
            Opt("900", "900", ""),
            Opt("1500", "1500", ""),
            Opt("2500", "2500", ""),
        ];

        private static readonly ClassScheduleOption[] GasketClass =
        [
            Opt("150", "150", ""),
            Opt("300", "300", ""),
        ];

        private static readonly ClassScheduleOption[] PeHdpe =
        [
            Opt("SDR17", "150", "SDR17"),
            Opt("SDR11", "150", "SDR11"),
        ];

        private static readonly ClassScheduleOption[] Threaded =
        [
            Opt("150", "150", ""),
            Opt("3000", "3000", ""),
        ];

        private static readonly ClassScheduleOption[] DefaultList =
        [
            Opt("150", "150", ""),
            Opt("40", "150", "40"),
            Opt("80", "150", "80"),
        ];

        public static IReadOnlyList<ClassScheduleOption> Resolve(
            string? categoryId,
            string? pipingComponent,
            string? primaryEndType)
        {
            string category = CatalogCategories.NormalizeCategoryId(categoryId);
            string component = pipingComponent?.Trim() ?? "";
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);

            if (category.Equals(CatalogCategories.Pipe, StringComparison.OrdinalIgnoreCase)
                && IsPipeMaterial(component))
            {
                return PipeMaterial;
            }

            if (category.Equals(CatalogCategories.Flanges, StringComparison.OrdinalIgnoreCase))
                return Flanged;

            if (category.Equals(CatalogCategories.Fasteners, StringComparison.OrdinalIgnoreCase))
            {
                if (component.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
                    return GasketClass;
                if (component.Equals("StubEnd", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Collar", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("BackingRing", StringComparison.OrdinalIgnoreCase))
                {
                    return BwFitting;
                }

                return Flanged;
            }

            if (category.Equals(CatalogCategories.Valves, StringComparison.OrdinalIgnoreCase))
            {
                return IsScheduleEnd(end) ? BwFitting : Flanged;
            }

            if (category.Equals(CatalogCategories.Olet, StringComparison.OrdinalIgnoreCase))
            {
                return IsScheduleEnd(end) ? BwFitting : IsSwEnd(end) ? SwFitting : Flanged;
            }

            if (category.Equals(CatalogCategories.Instruments, StringComparison.OrdinalIgnoreCase)
                || category.Equals(CatalogCategories.Actuators, StringComparison.OrdinalIgnoreCase))
            {
                return Flanged;
            }

            if (category.Equals(CatalogCategories.Miscellaneous, StringComparison.OrdinalIgnoreCase))
            {
                return IsScheduleEnd(end) ? BwFitting : Flanged;
            }

            // Fittings (default)
            if (IsPeEnd(end))
                return PeHdpe;
            if (IsSwEnd(end))
                return SwFitting;
            if (IsThreadedEnd(end))
                return Threaded;
            if (IsScheduleEnd(end))
                return BwFitting;
            if (IsFlangedEnd(end))
                return Flanged;

            return DefaultList;
        }

        public static ClassScheduleOption Match(
            string? pressureClass,
            string? pipeSchedule,
            string? categoryId,
            string? pipingComponent,
            string? primaryEndType)
        {
            string pc = string.IsNullOrWhiteSpace(pressureClass) ? "150" : pressureClass.Trim();
            string sch = PipeScheduleCatalog.NormalizeOrEmpty(pipeSchedule);
            IReadOnlyList<ClassScheduleOption> options = Resolve(categoryId, pipingComponent, primaryEndType);

            ClassScheduleOption? bySchedule = options.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.PipeSchedule)
                && o.PipeSchedule.Equals(sch, StringComparison.OrdinalIgnoreCase)
                && o.PressureClass.Equals(pc, StringComparison.OrdinalIgnoreCase));
            if (bySchedule != null)
                return bySchedule;

            bySchedule = options.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.PipeSchedule)
                && o.PipeSchedule.Equals(sch, StringComparison.OrdinalIgnoreCase));
            if (bySchedule != null)
                return bySchedule;

            ClassScheduleOption? byClass = options.FirstOrDefault(o =>
                string.IsNullOrEmpty(o.PipeSchedule)
                && o.PressureClass.Equals(pc, StringComparison.OrdinalIgnoreCase));
            if (byClass != null)
                return byClass;

            return options.Count > 0 ? options[0] : DefaultList[0];
        }

        public static bool PartMatches(ClassScheduleOption selected, CustomPartDefinition part)
        {
            if (!part.PressureClass.Equals(selected.PressureClass, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(selected.PipeSchedule))
                return true;

            return PipeScheduleCatalog.PartMatches(selected.PipeSchedule, part.PipeSchedule);
        }

        private static bool IsPipeMaterial(string component) =>
            component.Equals("CS", StringComparison.OrdinalIgnoreCase)
            || component.Equals("SS", StringComparison.OrdinalIgnoreCase)
            || component.Equals("HDPE", StringComparison.OrdinalIgnoreCase)
            || component.Equals("Pipe", StringComparison.OrdinalIgnoreCase);

        private static bool IsScheduleEnd(string end) =>
            end.Equals("BV", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PPL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("BW", StringComparison.OrdinalIgnoreCase);

        private static bool IsSwEnd(string end) =>
            end.Equals("SW", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PSW", StringComparison.OrdinalIgnoreCase);

        private static bool IsThreadedEnd(string end) =>
            end.Equals("THDM", StringComparison.OrdinalIgnoreCase)
            || end.Equals("THDF", StringComparison.OrdinalIgnoreCase)
            || end.Equals("TAP", StringComparison.OrdinalIgnoreCase);

        private static bool IsFlangedEnd(string end) =>
            end.Equals("FL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("SO", StringComparison.OrdinalIgnoreCase)
            || end.Equals("WF", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LFL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LLP", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LAP", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LUG", StringComparison.OrdinalIgnoreCase);

        private static bool IsPeEnd(string end) =>
            end.Equals("P", StringComparison.OrdinalIgnoreCase)
            || end.Equals("SL", StringComparison.OrdinalIgnoreCase);

        private static ClassScheduleOption Opt(string id, string pc, string sch) =>
            new(id, pc, sch);
    }
}
