using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class P4EquipmentCatalogTests
    {
        private GameObject root;
        private EconomyManager economy;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("p4-camera-catalog-test");
            economy = root.AddComponent<EconomyManager>();
            P4EquipmentCatalog.Apply(economy);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void UpperCameras_AreStableTierTwoAndThreeCatalogEntries()
        {
            Assert.That(economy.TryGetEquipmentDefinition(P4EquipmentCatalog.CameraAdvancedId, out var advanced), Is.True);
            Assert.That(advanced.Slot, Is.EqualTo("camera"));
            Assert.That(advanced.Level, Is.EqualTo(2));
            Assert.That(advanced.Price, Is.EqualTo(P4EquipmentCatalog.CameraAdvancedPrice));

            Assert.That(economy.TryGetEquipmentDefinition(P4EquipmentCatalog.CameraProfessionalId, out var professional), Is.True);
            Assert.That(professional.Slot, Is.EqualTo("camera"));
            Assert.That(professional.Level, Is.EqualTo(3));
            Assert.That(professional.Price, Is.EqualTo(P4EquipmentCatalog.CameraProfessionalPrice));
        }

        [Test]
        public void Restore_KeepsUpperCameraIds_WhenCatalogWasBoundFirst()
        {
            var data = new EconomySaveData
            {
                SchemaVersion = EconomySaveData.CurrentSchemaVersion,
                SharedBalance = 0,
                Revision = 7,
                Loadouts = new List<EconomyLoadoutSave>
                {
                    new EconomyLoadoutSave
                    {
                        PlayerId = 0,
                        EquipmentIds = new List<string>
                        {
                            EconomyManager.CameraBasicId,
                            P4EquipmentCatalog.CameraAdvancedId,
                            P4EquipmentCatalog.CameraProfessionalId
                        }
                    }
                }
            };

            Assert.That(economy.TryRestore(data), Is.True);
            var loadout = economy.LoadoutFor(new PlayerId(0));
            Assert.That(loadout, Does.Contain(EconomyManager.CameraBasicId));
            Assert.That(loadout, Does.Contain(P4EquipmentCatalog.CameraAdvancedId));
            Assert.That(loadout, Does.Contain(P4EquipmentCatalog.CameraProfessionalId));
        }
    }
}
