using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeepDive.World.Tests
{
    // The anchor is placement and identity only. These tests are mostly about what it must NOT
    // grow: a claim flag, a network identity, a call into the economy. Where the anchors sit in
    // DiveTestArea and that each is reachable without swimming is pinned by the beach scene
    // tests; nothing here opens a scene.
    public class BoatPartAnchorTests
    {
        private GameObject host;
        private BoatPartAnchor anchor;

        [SetUp]
        public void CreateAnchor()
        {
            host = new GameObject("BoatPart_Test");
            anchor = host.AddComponent<BoatPartAnchor>();
        }

        [TearDown]
        public void DestroyAnchor()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        private static void Validate(BoatPartAnchor target) =>
            typeof(BoatPartAnchor)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(target, null);

        [TestCase("boat-part-hull")]
        [TestCase("boat-part-engine")]
        [TestCase("boat-part-fuel-tank")]
        public void AnchorAcceptsEachContractId(string partId)
        {
            anchor.Configure(partId);
            Assert.AreEqual(partId, anchor.PartId);
            Assert.IsTrue(anchor.IsContractPart, partId + " must be one of the three fixed parts");
        }

        [Test]
        public void TheThreeIdsAreTheOnesTheEconomyKnows()
        {
            // Spelled out here rather than read from BoatRepairParts, so a rename on Mert's
            // side shows up as a failing test instead of silently renaming the world anchors
            // too. These strings reach the save file; they are not ours to change alone.
            CollectionAssert.AreEquivalent(
                new[] { "boat-part-hull", "boat-part-engine", "boat-part-fuel-tank" },
                BoatRepairParts.All);
        }

        [Test]
        public void UnknownIdIsNotAContractPart()
        {
            anchor.Configure("boat-part-sail");
            Assert.IsFalse(anchor.IsContractPart);
        }

        [Test]
        public void UnknownIdIsReportedOnValidate()
        {
            // An anchor with a typo looks perfectly fine in the scene while being unclaimable
            // forever, so it is a hard error rather than a warning.
            anchor.Configure("boat-part-sail");
            LogAssert.Expect(LogType.Error, new Regex("P3_BOATPART_UNKNOWN_ID"));
            Validate(anchor);
        }

        [Test]
        public void AnUnauthoredAnchorStaysQuiet()
        {
            // The default empty id means "not placed yet" and must not spam the console every
            // time someone adds the component.
            Assert.IsFalse(anchor.IsContractPart);
            Validate(anchor);
        }

        [Test]
        public void ConfigureTreatsNullAsUnauthoredRatherThanCrashing()
        {
            anchor.Configure(null);
            Assert.AreEqual(string.Empty, anchor.PartId);
            Assert.IsFalse(anchor.IsContractPart);
        }

        [Test]
        public void WorldPositionFollowsTheTransform()
        {
            host.transform.position = new Vector3(-6.5f, 7.76233f, -8f);
            Assert.AreEqual(new Vector3(-6.5f, 7.76233f, -8f), anchor.WorldPosition);
        }

        // --- What it must not become ---------------------------------------------------------

        [Test]
        public void BoatPartAnchorHasExactlyOneSerializedField()
        {
            // The guard against a second copy of progress: a "collected" bool here would be
            // state the save file never sees and the host never arbitrates.
            var serialized = typeof(BoatPartAnchor)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                           BindingFlags.DeclaredOnly)
                .Where(f => f.IsPublic || f.IsDefined(typeof(SerializeField), false))
                .ToArray();

            Assert.AreEqual(1, serialized.Length,
                "the anchor must hold only its id: " + string.Join(", ", serialized.Select(f => f.Name)));
            Assert.AreEqual("partId", serialized[0].Name);
            Assert.AreEqual(typeof(string), serialized[0].FieldType);
        }

        [Test]
        public void BoatPartAnchorIsNotNetworked()
        {
            // Scenery. A NetworkBehaviour here would need a GlobalObjectIdHash and would give
            // the anchor somewhere to replicate a claim from.
            Assert.IsFalse(typeof(NetworkBehaviour).IsAssignableFrom(typeof(BoatPartAnchor)));
            Assert.IsNull(host.GetComponent<NetworkObject>());
        }

        [Test]
        public void TheWorldAssemblyNeverCallsTheClaimSeam()
        {
            // BoatPartClaim lives in Core.Contracts, which DeepDive.World references, so the
            // assembly graph does not stop World from calling TryClaimFound the way it stops it
            // from touching EconomyManager. This scan is what closes that one gap: the claim is
            // Mehmet's interaction layer to make, and World only supplies the id and position.
            //
            // Comments are stripped first: explaining why this boundary exists has to stay
            // possible, and it is the call that is forbidden, not the name.
            var worldRoot = Path.Combine(Application.dataPath, "DeepDive", "World");
            var offenders = Directory
                .GetFiles(worldRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Replace('\\', '/').Contains("/Tests/"))
                .Where(path => StripComments(File.ReadAllText(path)).Contains("BoatPartClaim"))
                .Select(path => path.Substring(worldRoot.Length + 1))
                .ToArray();

            Assert.IsEmpty(offenders,
                "DeepDive.World must not reach into the claim seam: " + string.Join(", ", offenders));
        }

        // Block comments first, then line comments, so a "//" inside a stripped block does not
        // eat the line after it. A name that only survives inside a string literal cannot be a
        // call, so leaving string contents in is the conservative direction.
        private static string StripComments(string source) =>
            Regex.Replace(
                Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline),
                @"//[^\r\n]*", string.Empty);
    }
}
