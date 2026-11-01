using System;
using System.Collections.Generic;
using System.Linq;
using ParticleEngine.Core.Manifest;
using Xunit;

namespace ParticleEngine.Tests
{
    // Catalog sanity, run on both net8.0 and net48 (the full dump comparison lives in
    // ParticleEngine.Parity, net8.0 only).
    public class CatalogTests
    {
        private static readonly IReadOnlyList<ComponentSpec> All = ComponentManifest.Generate();

        private static readonly HashSet<string> Subcategories = new()
        {
            ComponentCatalog.SubSpawn, ComponentCatalog.SubInit, ComponentCatalog.SubBehaviors,
            ComponentCatalog.SubForces, ComponentCatalog.SubEngine, ComponentCatalog.SubDisplay,
        };

        [Fact]
        public void HasExactly18Components()
        {
            // 20 minus Mesh Crawl II and Multi Path Tracking II (owner, 2026-11-01).
            Assert.Equal(18, All.Count);
        }

        [Fact]
        public void KeysAndGuidsAreUnique()
        {
            Assert.Equal(All.Count, All.Select(c => c.Key).Distinct().Count());
            Assert.Equal(All.Count, All.Select(c => c.Guid).Distinct().Count());
        }

        [Fact]
        public void EveryComponentUsesAKnownSubcategoryAndHasOutputs()
        {
            foreach (var c in All)
            {
                Assert.Contains(c.Subcategory, Subcategories);
                Assert.NotEmpty(c.Outputs);
                Assert.False(string.IsNullOrWhiteSpace(c.Nick), $"{c.Key} has no nickname");
            }
        }

        [Fact]
        public void ControllerHasVariableInputsAndNoFixedPorts()
        {
            var controller = ComponentCatalog.ByKey("Controller");
            Assert.True(controller.VariableInputs);
            Assert.Empty(controller.Inputs);
        }

        [Fact]
        public void ByKeyThrowsForUnknownKey()
        {
            Assert.Throws<KeyNotFoundException>(() => ComponentCatalog.ByKey("Nope"));
        }

        [Fact]
        public void PortKindTypeNamesAreStable()
        {
            Assert.Equal("Generic Data", PortKind.Generic.DumpTypeName());
            Assert.Equal("Number", PortKind.Number.DumpTypeName());
            Assert.Equal("Circle", PortKind.Circle.DumpTypeName());
        }
    }
}
