using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using ParticleEngine.Core.Manifest;
using Xunit;

namespace ParticleEngine.Parity
{
    /// <summary>
    /// Port-conformance test (spec 11.1). Asserts the Particle Engine manifest reproduces the owner's
    /// installed Culebra ports (names, nicknames, order, types) captured in culebra_ports.json, and
    /// enforces the structural rules. Deviations are explicit and listed as allowed exceptions.
    /// Runs with no Rhino: the manifest comes from Core's static catalog.
    /// </summary>
    public class ConformanceTests
    {
        // Our deliberate, documented deviations from the Culebra dump (BUILD_SPEC §17):
        //  - Creeper engines take Init/Move Settings as item access, not Culebra's list (spec R4).
        private static readonly HashSet<string> AccessExceptions = new()
        {
            "CreeperEngine/Init Settings",
            "CreeperEngine/Move Settings",
            "CreeperZombieEngine/Init Settings",
            "CreeperZombieEngine/Move Settings",
        };

        //  - Bundling_Engine uses nickname "BE", not Culebra's placeholder "Nickname".
        private static readonly HashSet<string> NickExceptions = new() { "BundlingEngine" };

        private static readonly IReadOnlyList<ComponentSpec> Manifest = ComponentManifest.Generate();
        private static readonly Dictionary<string, DumpComponent> Dump = LoadDump();

        [Fact]
        public void Exactly20ComponentsRegistered()
        {
            Assert.Equal(20, Manifest.Count);
        }

        [Fact]
        public void GuidsAreUnique()
        {
            var guids = Manifest.Select(c => c.Guid).ToList();
            Assert.Equal(guids.Count, guids.Distinct().Count());
        }

        [Fact]
        public void NewComponentBundlingZombieEngineIsPresentAndNotFromCulebra()
        {
            var bze = Manifest.Single(c => c.Key == "BundlingZombieEngine");
            Assert.False(bze.InCulebra);
            Assert.Equal("Bundling_ZombieEngine", bze.Name);
            Assert.Equal("BZE", bze.Nick);
        }

        [Fact]
        public void ScalarDefaultsAreWellFormedAndSingleValued()
        {
            foreach (var c in Manifest)
            {
                foreach (var p in c.Inputs)
                {
                    if (p.Default == null) continue;
                    // Single value only (R3): no list separators smuggled into a default.
                    Assert.DoesNotContain(";", p.Default);
                    switch (p.Kind)
                    {
                        case PortKind.Boolean:
                            Assert.True(bool.TryParse(p.Default, out _), $"{c.Key}/{p.Name} bad bool '{p.Default}'");
                            break;
                        case PortKind.Integer:
                            Assert.True(int.TryParse(p.Default, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                                $"{c.Key}/{p.Name} bad int '{p.Default}'");
                            break;
                        case PortKind.Number:
                            Assert.True(double.TryParse(p.Default, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                                $"{c.Key}/{p.Name} bad number '{p.Default}'");
                            break;
                        default:
                            Assert.Fail($"{c.Key}/{p.Name}: kind {p.Kind} should not carry a default");
                            break;
                    }
                }
            }
        }

        [Fact]
        public void PortsMatchCulebraDump()
        {
            var failures = new List<string>();

            foreach (var c in Manifest)
            {
                if (!c.InCulebra) continue; // the new Bundling_ZombieEngine has no Culebra entry

                if (!Dump.TryGetValue(c.Name, out var d))
                {
                    failures.Add($"{c.Name}: not found in dump");
                    continue;
                }

                if (!NickExceptions.Contains(c.Key) && c.Nick != d.Nick)
                    failures.Add($"{c.Name}: nick '{c.Nick}' != dump '{d.Nick}'");

                if (c.Subcategory != d.Sub)
                    failures.Add($"{c.Name}: sub '{c.Subcategory}' != dump '{d.Sub}'");

                ComparePorts(c.Key, c.Name, "IN", c.Inputs, d.Inputs, failures);
                ComparePorts(c.Key, c.Name, "OUT", c.Outputs, d.Outputs, failures);
            }

            Assert.True(failures.Count == 0, "Port conformance failures:\n  " + string.Join("\n  ", failures));
        }

        private static void ComparePorts(string key, string comp, string dir,
            IReadOnlyList<PortSpec> ours, IReadOnlyList<DumpPort> theirs, List<string> failures)
        {
            if (ours.Count != theirs.Count)
            {
                failures.Add($"{comp} {dir}: port count {ours.Count} != dump {theirs.Count}");
                return;
            }

            for (int i = 0; i < ours.Count; i++)
            {
                PortSpec o = ours[i];
                DumpPort t = theirs[i];
                string where = $"{comp} {dir}[{i}] '{o.Name}'";

                if (o.Name != t.Name) failures.Add($"{where}: name != dump '{t.Name}'");
                if (o.Nick != t.Nick) failures.Add($"{where}: nick '{o.Nick}' != dump '{t.Nick}'");
                if (o.Kind.DumpTypeName() != t.Type) failures.Add($"{where}: type '{o.Kind.DumpTypeName()}' != dump '{t.Type}'");

                string access = o.Access.ToString().ToLowerInvariant();
                bool accessAllowed = access == t.Access || (dir == "IN" && AccessExceptions.Contains($"{key}/{o.Name}"));
                if (!accessAllowed) failures.Add($"{where}: access '{access}' != dump '{t.Access}'");
            }
        }

        // ---- dump model + loader ----

        private sealed class DumpPort
        {
            public string Name = "";
            public string Nick = "";
            public string Type = "";
            public string Access = "";
        }

        private sealed class DumpComponent
        {
            public string Nick = "";
            public string Sub = "";
            public List<DumpPort> Inputs = new();
            public List<DumpPort> Outputs = new();
        }

        private static Dictionary<string, DumpComponent> LoadDump()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "culebra_ports.json");
            Assert.True(File.Exists(path), $"Fixture missing: {path}");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var result = new Dictionary<string, DumpComponent>();

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("inputs", out _)) continue; // skip any error rows

                var comp = new DumpComponent
                {
                    Nick = el.GetProperty("nick").GetString() ?? "",
                    Sub = el.TryGetProperty("sub", out var s) ? s.GetString() ?? "" : "",
                    Inputs = ReadPorts(el, "inputs"),
                    Outputs = ReadPorts(el, "outputs"),
                };
                result[el.GetProperty("name").GetString() ?? ""] = comp;
            }

            return result;
        }

        private static List<DumpPort> ReadPorts(JsonElement comp, string prop)
        {
            var list = new List<DumpPort>();
            if (!comp.TryGetProperty(prop, out var arr)) return list;
            foreach (var p in arr.EnumerateArray())
            {
                list.Add(new DumpPort
                {
                    Name = p.GetProperty("name").GetString() ?? "",
                    Nick = p.GetProperty("nick").GetString() ?? "",
                    Type = p.GetProperty("type").GetString() ?? "",
                    Access = p.GetProperty("access").GetString() ?? "",
                });
            }
            return list;
        }
    }
}
