using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Plant 3D catalog package (6 deploy files). Template skeleton is always emitted;
    /// sections fill in as the user adds scene geometry and Port Manager connection points.
    /// </summary>
    internal static class CatalogCodeGenerator
    {
        public static CatalogPackage BuildPackage(ValveProject project)
        {
            CatalogPackageContext ctx = CatalogPackageContext.Create(project);
            bool sandbox = StandardCatalogGuard.IsStandardReferenceScene(project);
            string exportFolder = StandardCatalogGuard.ResolveExportFolderName(ctx.FolderName, sandbox);
            string scriptName = sandbox
                ? $"CUST_{ctx.FolderName}_PORT_REF"
                : ctx.ScriptName;

            return new CatalogPackage
            {
                ScriptName = scriptName,
                FolderName = ctx.FolderName,
                ExportFolderName = exportFolder,
                IsStandardPortReference = sandbox,
                StandardPartId = sandbox ? ctx.FolderName : null,
                PortManagerPortCount = ctx.Project.Ports.Count,
                CatalogEntryXml = BuildCatalogEntryXml(ctx, scriptName, sandbox),
                CatalogEntryPy = BuildCatalogEntryPy(ctx, scriptName, sandbox),
                ScriptGroupXml = BuildScriptGroupXml(ctx, scriptName),
                GeometryPy = BuildGeometryModule(ctx, sandbox),
            };
        }

        public static string Generate(ValveProject project) => BuildPackage(project).ToDisplayText();

        private static int DefaultCatalogDn(ValveProject project) =>
            project.Parameters.DN > 0 ? (int)Math.Round(project.Parameters.DN) : 100;

        private sealed class CatalogPackageContext
        {
            public ValveProject Project { get; }
            public string ScriptName { get; }
            public string FolderName { get; }
            public string ClassName { get; }
            public string Group { get; }
            public int PortCount { get; }
            public string FirstPortEndtypes { get; }
            public string AddPorts { get; }
            public bool LibraryReferenceOnly { get; }
            public string? LibraryPartId { get; }
            public string? LibraryClassName { get; }

            private CatalogPackageContext(ValveProject project)
            {
                Project = project;
                ScriptName = ResolveScriptName(project);
                FolderName = ScriptName.StartsWith("CUST_") ? ScriptName[5..] : ScriptName;
                LibraryPartId = TryGetSingleLibraryPartId(project);
                LibraryReferenceOnly = IsLibraryReferenceOnly(project, LibraryPartId);
                LibraryClassName = LibraryPartId != null
                    ? TryResolveLibraryPythonClassName(LibraryPartId)
                    : null;
                ClassName = ResolveGeometryClassName(project, ScriptName, LibraryPartId, LibraryClassName);
                AddPorts = ResolveAddPorts(project, LibraryPartId, LibraryReferenceOnly);
                PortCount = project.Ports.Count > 0
                    ? project.Ports.Count
                    : Math.Max(LibraryReferenceOnly ? CountLibraryPorts(LibraryPartId) : 0,
                        CatalogPortTemplates.CountPortsInAddPorts(AddPorts));
                if (PortCount == 0 && project.Parts.Count == 0)
                    PortCount = 1;
                FirstPortEndtypes = ResolveFirstPortEndtypes(project, PortCount, LibraryPartId);
                Group = ResolveGroup(project, LibraryPartId, FirstPortEndtypes);
            }

            public static CatalogPackageContext Create(ValveProject project) => new(project);

            public bool HasGeometry => Project.Parts.Count > 0;

            public string BuildBody =>
                HasGeometry
                    ? PythonCodeGenerator.GenerateBuildBody(Project, forCatalogPackage: true)
                    : "        geom = None  # TODO: insert parts in Scene tab";
        }

        private static bool IsLibraryReferenceOnly(ValveProject project, string? libraryPartId)
        {
            if (libraryPartId == null || project.Ports.Count > 0 || project.Operations.Count > 0)
                return false;

            if (project.Parts.Count != 1)
                return false;

            PrimitiveNode part = project.Parts[0];
            if (part.Kind != SceneNodeKind.Catalog)
                return false;

            if (HasNonIdentityTransform(part))
                return false;

            return CustomPartCatalog.FindById(libraryPartId) != null;
        }

        private static bool HasNonIdentityTransform(PrimitiveNode part)
        {
            double x = part.Origin.Length > 0 ? part.Origin[0] : 0;
            double y = part.Origin.Length > 1 ? part.Origin[1] : 0;
            double z = part.Origin.Length > 2 ? part.Origin[2] : 0;
            if (Math.Abs(x) > 1e-9 || Math.Abs(y) > 1e-9 || Math.Abs(z) > 1e-9)
                return true;

            if (part.Rotation == null || part.Rotation.Length < 9)
                return false;

            double[] r = part.Rotation;
            return Math.Abs(r[0] - 1) > 1e-6 || Math.Abs(r[4] - 1) > 1e-6 || Math.Abs(r[8] - 1) > 1e-6
                   || Math.Abs(r[1]) > 1e-6 || Math.Abs(r[2]) > 1e-6 || Math.Abs(r[3]) > 1e-6
                   || Math.Abs(r[5]) > 1e-6 || Math.Abs(r[6]) > 1e-6 || Math.Abs(r[7]) > 1e-6;
        }

        private static string? TryGetSingleLibraryPartId(ValveProject project)
        {
            if (project.Parts.Count != 1)
                return null;

            PrimitiveNode part = project.Parts[0];
            return part.Kind == SceneNodeKind.Catalog && !string.IsNullOrEmpty(part.CatalogPartId)
                ? part.CatalogPartId
                : null;
        }

        private static int CountLibraryPorts(string? libraryPartId)
        {
            if (string.IsNullOrEmpty(libraryPartId))
                return 0;

            string? addPorts = CatalogPortTemplates.TryLoadAddPortsMethod(libraryPartId);
            return CatalogPortTemplates.CountPortsInAddPorts(addPorts);
        }

        private static string ResolveScriptName(ValveProject project)
        {
            if (project.Parts.Count == 1 &&
                project.Parts[0].Kind == SceneNodeKind.Catalog &&
                !string.IsNullOrWhiteSpace(project.Parts[0].CatalogPartId))
            {
                return $"CUST_{project.Parts[0].CatalogPartId}";
            }

            string baseName = string.IsNullOrWhiteSpace(project.ValveName)
                ? "COMPOSER_PART"
                : SanitizeId(project.ValveName);
            return $"CUST_{baseName}";
        }

        private static string ResolveGroup(ValveProject project, string? libraryPartId, string firstPortEndtypes)
        {
            string? userGroup = null;
            if (!string.IsNullOrWhiteSpace(project.CatalogGroup))
                userGroup = project.CatalogGroup.Trim();
            else if (!string.IsNullOrEmpty(libraryPartId))
            {
                CustomPartDefinition? def = CustomPartCatalog.FindById(libraryPartId);
                if (def != null)
                    userGroup = def.Group;
            }

            userGroup ??= "Custom";
            return CatalogGroupResolver.Resolve(userGroup, project.Ports, firstPortEndtypes);
        }

        private static string ResolveFirstPortEndtypes(ValveProject project, int portCount, string? libraryPartId)
        {
            if (project.Ports.Count > 0)
                return CatalogPortTemplates.InferFirstPortEndtypesFromProject(project);

            if (!string.IsNullOrEmpty(libraryPartId))
                return CatalogPortTemplates.InferFirstPortEndtypes(libraryPartId, portCount);

            return CatalogPortTemplates.InferFirstPortEndtypesFromPrimaryEnd(project, portCount);
        }

        private static string ResolveAddPorts(ValveProject project, string? libraryPartId, bool libraryReferenceOnly)
        {
            if (project.Ports.Count > 0)
                return CatalogPortTemplates.GenerateAddPortsFromProject(project);

            if (!string.IsNullOrEmpty(libraryPartId))
            {
                string? fromLibrary = CatalogPortTemplates.TryLoadAddPortsMethod(libraryPartId);
                if (!string.IsNullOrWhiteSpace(fromLibrary))
                    return fromLibrary;
            }

            if (project.Parts.Count == 0)
            {
                return @"    def add_ports(self, s):
        """"""TODO: add connection points in Port Manager, then Apply.""""""
        return self";
            }

            return CatalogPortTemplates.GenerateDefaultAddPorts(
                ResolvePortSpan(project),
                "TODO: refine port positions in Port Manager (default axial ports).");
        }

        private static double ResolvePortSpan(ValveProject project)
        {
            if (project.Parameters.FaceToFace > 1e-6)
                return project.Parameters.FaceToFace;

            if (project.Parameters.BodyLength > 1e-6)
                return project.Parameters.BodyLength;

            double maxZ = 0;
            foreach (PrimitiveNode part in project.Parts)
            {
                if (part.Parameters.TryGetValue("L", out ParamValue? l) && l.Value > maxZ)
                    maxZ = l.Value;
                if (part.Parameters.TryGetValue("H", out ParamValue? h) && h.Value > maxZ)
                    maxZ = h.Value;
            }

            return maxZ > 1e-6 ? maxZ : 100;
        }

        private static string BuildCatalogEntryXml(CatalogPackageContext ctx, string scriptName, bool portRef)
        {
            if (portRef)
            {
                return BuildPortReferenceCatalogEntryXml(ctx, scriptName);
            }

            if (ctx.LibraryReferenceOnly && ctx.LibraryPartId != null)
            {
                string? existing = CatalogPortTemplates.TryLoadCatalogEntryXml(ctx.LibraryPartId);
                if (!string.IsNullOrEmpty(existing))
                    return existing;
            }

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<ArrayOfScript xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
            sb.AppendLine(
                $"\t<Script Name=\"{scriptName}\" LDesId=\"{scriptName}_L\" SDesId=\"{scriptName}\" ConnPortNum=\"{ctx.PortCount}\" LengthUnit=\"mm\" >");
            sb.AppendLine("\t\t<Params>");

            if (!ctx.HasGeometry)
            {
                sb.AppendLine(
                    $"\t\t\t<!-- TODO: add DN or other params after inserting parts -->");
                sb.AppendLine(
                    $"\t\t\t<Param Name=\"DN\" LDesId=\"{ctx.ScriptName}.DN\" SDesId=\"{ctx.ScriptName}.DN\" Type=\"INT\" Ask4Dist=\"false\" Value=\"100\" />");
            }
            else if (ctx.LibraryPartId != null && ctx.Project.Ports.Count == 0)
            {
                string? existing = CatalogPortTemplates.TryLoadCatalogEntryXml(ctx.LibraryPartId);
                if (!string.IsNullOrEmpty(existing))
                {
                    int paramsStart = existing.IndexOf("<Params>", StringComparison.Ordinal);
                    int paramsEnd = existing.IndexOf("</Params>", StringComparison.Ordinal);
                    if (paramsStart >= 0 && paramsEnd > paramsStart)
                    {
                        string inner = existing.Substring(paramsStart + 8, paramsEnd - paramsStart - 8).Trim();
                        foreach (string line in inner.Split('\n'))
                            sb.AppendLine("\t\t" + line.TrimEnd('\r'));
                        sb.AppendLine("\t\t</Params>");
                        sb.AppendLine("\t</Script>");
                        sb.AppendLine("</ArrayOfScript>");
                        return sb.ToString();
                    }
                }
            }

            if (ctx.Project.Parameters.DN > 0)
            {
                sb.AppendLine(
                    $"\t\t\t<Param Name=\"DN\" LDesId=\"{ctx.ScriptName}.DN\" SDesId=\"{ctx.ScriptName}.DN\" Type=\"INT\" Ask4Dist=\"false\" Value=\"{(int)Math.Round(ctx.Project.Parameters.DN)}\" />");
            }

            string cgp = CatalogExcelGeometryParams.ResolveParamDefinition(ctx.Project, ctx.FolderName);
            foreach ((string name, double value, string xmlType) in CatalogExcelGeometryParams.CollectScriptParams(
                         ctx.Project, cgp))
            {
                if (name.Equals("DN", StringComparison.OrdinalIgnoreCase))
                    continue;

                string valueText = xmlType.Equals("INT", StringComparison.OrdinalIgnoreCase)
                    ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                    : value.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine(
                    $"\t\t\t<Param Name=\"{name}\" LDesId=\"{ctx.ScriptName}.{name}\" SDesId=\"{ctx.ScriptName}.{name}\" Type=\"{xmlType}\" Ask4Dist=\"false\" Value=\"{valueText}\" />");
            }

            sb.AppendLine("\t\t</Params>");
            sb.AppendLine("\t</Script>");
            sb.AppendLine("</ArrayOfScript>");
            return sb.ToString();
        }

        private static string BuildPortReferenceCatalogEntryXml(CatalogPackageContext ctx, string scriptName)
        {
            int dn = DefaultCatalogDn(ctx.Project);
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ArrayOfScript xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
	<!-- Port reference only — standard library: parts/{ctx.FolderName}/ -->
	<Script Name=""{scriptName}"" LDesId=""{scriptName}_L"" SDesId=""{scriptName}"" ConnPortNum=""{ctx.PortCount}"" LengthUnit=""mm"" >
		<Params>
			<Param Name=""DN"" LDesId=""{scriptName}.DN"" SDesId=""{scriptName}.DN"" Type=""INT"" Ask4Dist=""false"" Value=""{dn}"" />
		</Params>
	</Script>
</ArrayOfScript>";
        }

        private static string BuildCatalogEntryPy(CatalogPackageContext ctx, string scriptName, bool portRef)
        {
            if (portRef)
            {
                int dn = DefaultCatalogDn(ctx.Project);
                return $@"# Port reference only — do not deploy. Update standard library:
# catalog_generator/parts/{ctx.FolderName}/{ctx.FolderName}/CUST_{ctx.FolderName}.py
from varmain.custom import *  # type: ignore

from {ctx.FolderName}.{scriptName} import {ctx.ClassName}


@activate(  # type: ignore
    Group=""{ctx.Group}"",
    TooltipShort=""PORT REF {ctx.FolderName}"",
    TooltipLong=""Composer port study for {ctx.FolderName} — not a catalog part"",
    FirstPortEndtypes=""{ctx.FirstPortEndtypes}"",
    LengthUnit=""mm"",
    Ports=""{ctx.PortCount}"",
)
def {scriptName}(s, DN={dn}, **kw):
    preview = bool(kw.get(""preview"", False))
    return {ctx.ClassName}(s, int(DN), add_ports=not preview)";
            }

            if (ctx.LibraryReferenceOnly && ctx.LibraryPartId != null)
            {
                string? existing = CatalogPortTemplates.TryLoadCatalogEntryPy(ctx.LibraryPartId);
                if (!string.IsNullOrEmpty(existing))
                    return existing;
            }

            string tooltipShort = !string.IsNullOrWhiteSpace(ctx.Project.TooltipShort)
                ? ctx.Project.TooltipShort.Trim()
                : ctx.FolderName;
            string tooltipLong = !string.IsNullOrWhiteSpace(ctx.Project.TooltipLong)
                ? ctx.Project.TooltipLong.Trim()
                : ctx.HasGeometry
                    ? (ctx.LibraryPartId ?? ctx.FolderName)
                    : "Generated by Plant 3D Catalog Composer";
            int defaultDn = DefaultCatalogDn(ctx.Project);
            string cgp = CatalogExcelGeometryParams.ResolveParamDefinition(ctx.Project, ctx.FolderName);
            IReadOnlyList<string> excelParams = CatalogExcelGeometryParams.ParseParamNames(cgp)
                .Where(n => !n.Equals("DN", StringComparison.OrdinalIgnoreCase))
                .ToList();
            string activateParams = BuildActivateParameterList(ctx, defaultDn, excelParams);

            string returnLine;
            if (excelParams.Count == 0)
            {
                returnLine = $"    return {ctx.ClassName}(s, int(DN), add_ports=not preview)";
            }
            else
            {
                var dimInit = new List<string>();
                foreach (string excelName in excelParams)
                {
                    if (CatalogExcelGeometryParams.TryGetValue(ctx.Project, excelName, out double value))
                    {
                        string internalName = MapExcelDimToGeometry(excelName);
                        dimInit.Add($"\"{internalName}\": {value.ToString(CultureInfo.InvariantCulture)}");
                    }
                    else if (excelName.Equals("L", StringComparison.OrdinalIgnoreCase))
                    {
                        double l = ctx.Project.Parameters.FaceToFace > 0
                            ? ctx.Project.Parameters.FaceToFace
                            : Math.Max(150, DefaultCatalogDn(ctx.Project) * 2);
                        dimInit.Add($"\"L\": {l.ToString(CultureInfo.InvariantCulture)}");
                    }
                }

                if (dimInit.Count == 0)
                {
                    dimInit.Add($"\"BodyOD\": {PipeSizeCatalog.OdSch40Mm(defaultDn).ToString(CultureInfo.InvariantCulture)}");
                }

                var bodyLines = new List<string>
                {
                    "    import pipe_sizes",
                    "    body_od = pipe_sizes.pipe_od_sch40_mm(pipe_sizes.resolve_dn(int(DN)))",
                };

                foreach (string excelName in excelParams)
                {
                    if (excelName.Equals("L", StringComparison.OrdinalIgnoreCase))
                    {
                        bodyLines.Add(
                            "    face_to_face = float(L) if L not in (None, \"\") else "
                            + $"{ResolveDefaultGeometryValue(ctx, excelName, defaultDn).ToString(CultureInfo.InvariantCulture)}");
                    }
                    else if (excelName.Equals("D1", StringComparison.OrdinalIgnoreCase))
                    {
                        bodyLines.Add(
                            "    body_od_override = float(D1) if D1 not in (None, \"\") else body_od");
                    }
                }

                bodyLines.Add("    dim = {" + string.Join(", ", dimInit) + "}");
                bodyLines.Add("    dim.setdefault(\"BodyOD\", body_od)");
                if (excelParams.Any(n => n.Equals("L", StringComparison.OrdinalIgnoreCase)))
                    bodyLines.Add("    dim[\"L\"] = face_to_face");
                if (excelParams.Any(n => n.Equals("D1", StringComparison.OrdinalIgnoreCase)))
                    bodyLines.Add("    dim[\"BodyOD\"] = body_od_override");
                bodyLines.Add("    for _k, _v in kw.items():");
                bodyLines.Add("        if _v in (None, \"\"):");
                bodyLines.Add("            continue");
                bodyLines.Add("        if _k == \"L\":");
                bodyLines.Add("            dim[\"L\"] = float(_v)");
                bodyLines.Add("        elif _k == \"D1\":");
                bodyLines.Add("            dim[\"BodyOD\"] = float(_v)");
                bodyLines.Add("        elif _k in dim:");
                bodyLines.Add("            dim[_k] = float(_v)");
                bodyLines.Add($"    return {ctx.ClassName}(s, int(DN), add_ports=not preview, **dim)");
                returnLine = string.Join(Environment.NewLine, bodyLines);
            }

            return $@"from varmain.custom import *  # type: ignore

from {ctx.FolderName}.{scriptName} import {ctx.ClassName}


@activate(  # type: ignore
    Group=""{ctx.Group}"",
    TooltipShort=""{EscapePyString(tooltipShort)}"",
    TooltipLong=""{EscapePyString(tooltipLong)}"",
    FirstPortEndtypes=""{ctx.FirstPortEndtypes}"",
    LengthUnit=""mm"",
    Ports=""{ctx.PortCount}"",
)
def {scriptName}(s, {activateParams}, **kw):
    preview = bool(kw.get(""preview"", False))
{returnLine}";
        }

        private static string BuildActivateParameterList(
            CatalogPackageContext ctx,
            int defaultDn,
            IReadOnlyList<string> excelParams)
        {
            var parts = new List<string> { $"DN={defaultDn}" };
            foreach (string excelName in excelParams)
            {
                double value = ResolveDefaultGeometryValue(ctx, excelName, defaultDn);
                string pyType = excelName.Equals("DN", StringComparison.OrdinalIgnoreCase)
                    || excelName.Equals("DN2", StringComparison.OrdinalIgnoreCase)
                    ? "INT"
                    : "LENGTH0";
                string valueText = pyType.Equals("INT", StringComparison.OrdinalIgnoreCase)
                    ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                    : value.ToString(CultureInfo.InvariantCulture);
                parts.Add($"{excelName}={valueText}");
            }

            return string.Join(", ", parts);
        }

        private static double ResolveDefaultGeometryValue(
            CatalogPackageContext ctx,
            string excelName,
            int defaultDn)
        {
            if (CatalogExcelGeometryParams.TryGetValue(ctx.Project, excelName, out double value))
                return value;

            if (excelName.Equals("L", StringComparison.OrdinalIgnoreCase))
            {
                return ctx.Project.Parameters.FaceToFace > 0
                    ? ctx.Project.Parameters.FaceToFace
                    : Math.Max(150, defaultDn * 2);
            }

            if (excelName.Equals("D1", StringComparison.OrdinalIgnoreCase))
                return PipeSizeCatalog.OdSch40Mm(defaultDn);

            return 0;
        }

        private static string MapExcelDimToGeometry(string excelName) =>
            excelName.ToUpperInvariant() switch
            {
                "L" => "L",
                "D1" => "BodyOD",
                _ => excelName,
            };

        private static string BuildScriptGroupXml(CatalogPackageContext ctx, string scriptName) =>
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ScriptInfo>
	<ScriptGroup>
		<ScriptName>{scriptName}</ScriptName>
		<Group>{ctx.Group}</Group>
		<FirstPortEndtypes>{ctx.FirstPortEndtypes}</FirstPortEndtypes>
	</ScriptGroup>
</ScriptInfo>";

        private static string BuildGeometryModule(CatalogPackageContext ctx, bool portRef)
        {
            if (portRef)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# PORT REFERENCE — study export for {ctx.FolderName}");
                sb.AppendLine($"# Apply port fixes to catalog_generator/parts/{ctx.FolderName}/ (standard library).");
                sb.AppendLine("# This file is not deployed to Spec Editor.");
                sb.AppendLine(BuildComposerGeometryClass(ctx).TrimEnd());
                return sb.ToString();
            }

            if (ctx.LibraryReferenceOnly && ctx.LibraryPartId != null && !string.IsNullOrEmpty(ctx.LibraryClassName))
            {
                return $@"# Catalog geometry — library part {ctx.LibraryPartId}
# Deploy catalog_generator/parts/{ctx.LibraryPartId}/ to CustomScripts.
from {ctx.LibraryPartId}.CUST_{ctx.LibraryPartId} import {ctx.LibraryClassName}

__all__ = [""{ctx.LibraryClassName}""]";
            }

            return BuildComposerGeometryClass(ctx);
        }

        private static string BuildComposerGeometryClass(CatalogPackageContext ctx)
        {
            var sb = new StringBuilder();
            if (ctx.Project.Ports.Count > 0)
            {
                sb.AppendLine(
                    $"# Port Manager: {ctx.Project.Ports.Count} connection point(s) — add_ports() below");
            }
            else
            {
                sb.AppendLine("# Port Manager: no ports — add_ports() uses library defaults or TODO placeholders");
            }

            sb.AppendLine(CatalogSceneManifest.Build(ctx.Project));
            sb.AppendLine("import primitives as prim");
            sb.AppendLine("from primitives import (");
            sb.AppendLine("    Box, Cone, Cylinder, Elbow, EllipsoidHead, EllipsoidHead2,");
            sb.AppendLine("    EllipsoidSegment, HalfSphere, Pyramid, Reduced_elbow,");
            sb.AppendLine("    RoundRectangle, SegmentedElbow, ShapeAssembly, Sphere,");
            sb.AppendLine("    SphereSegment, TorisPhericHead, TorisPhericHead2, TorisPhericHeadH, Torus,");
            sb.AppendLine("    ShapeObject,");
            sb.AppendLine(")");
            AppendCatalogPartImports(sb, ctx.Project);
            sb.AppendLine();
            int defaultDn = DefaultCatalogDn(ctx.Project);
            IReadOnlyList<(string Name, double Value)> exportDims =
                CatalogExportPrepareService.CollectExportDimensionParams(ctx.Project);
            string dimSig = exportDims.Count == 0
                ? ""
                : ", " + string.Join(", ", exportDims.Select(d =>
                    $"{d.Name}={d.Value.ToString(CultureInfo.InvariantCulture)}"));
            sb.AppendLine($"class {ctx.ClassName}(ShapeObject):");
            sb.AppendLine($"    def __init__(self, s, DN={defaultDn}{dimSig}, *, add_ports=True):");
            sb.AppendLine("        # --- geometry (scene graph) ---");
            sb.AppendLine(ctx.BuildBody);
            sb.AppendLine(@"        super().__init__(geom.obj if hasattr(geom, ""obj"") else geom)
        if add_ports:
            self.add_ports(s)");
            sb.AppendLine();
            sb.AppendLine(ctx.AddPorts);
            return sb.ToString();
        }

        private static void AppendCatalogPartImports(StringBuilder sb, ValveProject project)
        {
            foreach (string partId in project.Parts
                         .Where(p => p.Kind == SceneNodeKind.Catalog && !string.IsNullOrEmpty(p.CatalogPartId))
                         .Select(p => p.CatalogPartId!)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string? flatImport = CatalogPortTemplates.TryBuildFlatCatalogImport(partId);
                if (!string.IsNullOrEmpty(flatImport))
                    sb.AppendLine(flatImport);
                else
                    sb.AppendLine($"from CUST_{partId} import CUST_{partId}");
            }
        }

        private static string ResolveGeometryClassName(
            ValveProject project,
            string scriptName,
            string? libraryPartId,
            string? libraryClassName)
        {
            if (project.Ports.Count > 0 &&
                libraryPartId != null &&
                !string.IsNullOrEmpty(libraryClassName))
            {
                return libraryClassName + "Composer";
            }

            if (!project.Parts.Any(p => p.Kind != SceneNodeKind.Catalog) &&
                project.Parts.Count(p => p.Kind == SceneNodeKind.Catalog) == 1 &&
                libraryClassName != null &&
                (project.Ports.Count > 0 || project.Operations.Count > 0 || project.Parts.Any(HasNonIdentityTransform)))
            {
                return libraryClassName + "Composer";
            }

            return ToClassName(scriptName);
        }

        private static string? TryResolveLibraryPythonClassName(string partId)
        {
            string? entryPy = CatalogPortTemplates.TryLoadCatalogEntryPy(partId);
            if (string.IsNullOrEmpty(entryPy))
                return null;

            Match match = Regex.Match(entryPy, @"from\s+\S+\s+import\s+(\w+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ToClassName(string scriptName) =>
            scriptName.StartsWith("CUST_") ? scriptName[5..] : scriptName;

        private static string SanitizeId(string name) =>
            CatalogProjectService.SanitizeCatalogName(name);

        private static string EscapePyString(string value) =>
            value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
