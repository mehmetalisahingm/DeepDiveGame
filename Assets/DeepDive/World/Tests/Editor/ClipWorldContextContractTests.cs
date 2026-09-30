using System;
using System.Collections.Generic;
using System.Reflection;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // #101 Core contract: RecordingWorldContext reads as Empty by default and never holds null, "first" is a
    // species-only flag, the clip manifest/save carry the context with no position anywhere, and a save
    // written before the fields existed loads as Unknown / "" / false.
    public class ClipWorldContextContractTests
    {
        private static readonly Type[] PositionTypes =
            { typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Transform), typeof(Matrix4x4) };

        // Exact names only (case-insensitive): WorldContext, DayNumber or SizeBytes are fine.
        private static readonly string[] PositionNames = { "position", "worldposition", "pos", "x", "y", "z" };

        private static void AssertEmpty(in RecordingWorldContext c)
        {
            Assert.That(c.Kind, Is.EqualTo(RecordingSubjectKind.Unknown));
            Assert.That(c.RegionId, Is.EqualTo(""));
            Assert.That(c.CellId, Is.EqualTo(""));
            Assert.That(c.DepthBandId, Is.EqualTo(""));
            Assert.That(c.FirstRecordingOfSubject, Is.False);
        }

        private static ClipManifest Manifest(in RecordingWorldContext context) =>
            new ClipManifest("clip-rec-1", "rec-1", "dive-1", 2, new PlayerId(1), "sea_bass", 3, 12f, "h", 1000,
                true, true, context);

        [Test]
        public void DefaultAndNull_AreUnknownAndEmpty()
        {
            AssertEmpty(default(RecordingWorldContext));
            AssertEmpty(RecordingWorldContext.Empty);
            AssertEmpty(new RecordingWorldContext(RecordingSubjectKind.Unknown, null, null, null, false));
            AssertEmpty(default(ClipManifest).WorldContext);
        }

        [Test]
        public void FirstFlag_OnlyForSpecies_BadKindIsUnknown()
        {
            Assert.That(new RecordingWorldContext(RecordingSubjectKind.Species, "r", "c", "", true).FirstRecordingOfSubject, Is.True);
            Assert.That(new RecordingWorldContext(RecordingSubjectKind.Event, "r", "c", "", true).FirstRecordingOfSubject, Is.False);
            Assert.That(new RecordingWorldContext(RecordingSubjectKind.Unknown, "r", "c", "", true).FirstRecordingOfSubject, Is.False);

            var bad = new RecordingWorldContext((RecordingSubjectKind)7, "r", "c", "", true);
            Assert.That(bad.Kind, Is.EqualTo(RecordingSubjectKind.Unknown));
            Assert.That(bad.FirstRecordingOfSubject, Is.False);
        }

        [TestCase(typeof(RecordingWorldContext))]
        [TestCase(typeof(ClipManifest))]
        [TestCase(typeof(ClipSave))]
        public void NoPositionFields(Type type)
        {
            var offenders = new List<string>();
            Scan(type, type.Name, new HashSet<Type>(), offenders);
            Assert.That(offenders, Is.Empty, string.Join(", ", offenders));
        }

        // Fields and properties by type and by exact name, descending into nested DeepDive structs
        // (ClipManifest.WorldContext -> RecordingWorldContext, Owner -> PlayerId).
        private static void Scan(Type type, string path, HashSet<Type> seen, List<string> offenders)
        {
            if (!seen.Add(type)) return;
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var f in type.GetFields(all)) Check(f.FieldType, f.Name, path, seen, offenders);
            foreach (var p in type.GetProperties(all))
                if (p.GetIndexParameters().Length == 0) Check(p.PropertyType, p.Name, path, seen, offenders);
        }

        private static void Check(Type memberType, string name, string path, HashSet<Type> seen, List<string> offenders)
        {
            var where = path + "." + name;
            if (Array.IndexOf(PositionTypes, memberType) >= 0) offenders.Add(where + " : " + memberType.Name);
            foreach (var banned in PositionNames)
                if (string.Equals(name, banned, StringComparison.OrdinalIgnoreCase)) offenders.Add(where + " (name)");

            if (memberType.IsValueType && !memberType.IsPrimitive && !memberType.IsEnum &&
                memberType.Namespace != null && memberType.Namespace.StartsWith("DeepDive", StringComparison.Ordinal))
                Scan(memberType, where, seen, offenders);
        }

        [Test]
        public void OldCtor_GivesEmptyContext()
        {
            var m = new ClipManifest("clip-rec-1", "rec-1", "dive-1", 2, new PlayerId(1), "sea_bass", 3, 12f, "h",
                1000, true, true);
            AssertEmpty(m.WorldContext);
            Assert.That(m.IsWellFormed, Is.True);
            Assert.That(m.IsCommercialCandidate, Is.True);
        }

        [Test]
        public void ClipSave_JsonRoundTrip_KeepsContext()
        {
            var context = new RecordingWorldContext(RecordingSubjectKind.Species, ExplorationIds.NearRegionId,
                ExplorationIds.CellId(ExplorationIds.NearRegionId, 1, -2), DepthBandIds.Shallow, true);
            var data = new MediaSaveData();
            data.Clips.Add(ClipSave.From(Manifest(context)));

            var loaded = JsonUtility.FromJson<MediaSaveData>(JsonUtility.ToJson(data));
            var c = loaded.Clips[0].ToManifest().WorldContext;
            Assert.That(c.Kind, Is.EqualTo(RecordingSubjectKind.Species));
            Assert.That(c.RegionId, Is.EqualTo(context.RegionId));
            Assert.That(c.CellId, Is.EqualTo(context.CellId));
            Assert.That(c.DepthBandId, Is.EqualTo(DepthBandIds.Shallow));
            Assert.That(c.FirstRecordingOfSubject, Is.True);
        }

        [Test]
        public void LegacyClipSaveJson_LoadsUnknownEmpty()
        {
            const string json =
                "{\"Clips\":[{\"ClipId\":\"clip-rec-1\",\"RecordingId\":\"rec-1\",\"DiveId\":\"dive-1\",\"DayNumber\":1," +
                "\"OwnerPlayerId\":0,\"SubjectId\":\"sea_bass\",\"Quality\":3,\"DurationSeconds\":12.0," +
                "\"ContentHash\":\"h\",\"SizeBytes\":1000,\"MediaReady\":true,\"SafeReturned\":true}]," +
                "\"Publications\":[],\"Followers\":0}";

            var loaded = JsonUtility.FromJson<MediaSaveData>(json);
            var save = loaded.Clips[0];
            Assert.That(save.SubjectKind, Is.EqualTo(0));
            Assert.That(save.RegionId, Is.EqualTo(""));
            Assert.That(save.CellId, Is.EqualTo(""));
            Assert.That(save.DepthBandId, Is.EqualTo(""));
            Assert.That(save.FirstRecordingOfSubject, Is.False);

            var m = save.ToManifest();
            AssertEmpty(m.WorldContext);
            Assert.That(m.IsWellFormed, Is.True);
            Assert.That(m.ClipId, Is.EqualTo("clip-rec-1"));
        }

        [Test]
        public void TamperedSave_EventWithFirstFlag_IsFolded()
        {
            var save = ClipSave.From(Manifest(RecordingWorldContext.Empty));
            save.SubjectKind = (byte)RecordingSubjectKind.Event;
            save.FirstRecordingOfSubject = true;
            save.RegionId = null;
            Assert.That(save.ToManifest().WorldContext.FirstRecordingOfSubject, Is.False);
            Assert.That(save.ToManifest().WorldContext.RegionId, Is.EqualTo(""));

            save.SubjectKind = 9;
            Assert.That(save.ToManifest().WorldContext.Kind, Is.EqualTo(RecordingSubjectKind.Unknown));
            Assert.That(save.ToManifest().WorldContext.FirstRecordingOfSubject, Is.False);
        }
    }
}
