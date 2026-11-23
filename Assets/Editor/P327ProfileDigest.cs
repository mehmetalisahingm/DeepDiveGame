using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace DeepDive.Editor
{
    // #327 (Utku): reads a development player's profiler capture (-PerfProfile, Unity's .raw) and writes which markers
    // cost the main thread most (self time) and which allocate the managed memory each frame (GC.Alloc under them),
    // summed over every loaded frame. Path from the environment variable P327_RAW; output next to it as .digest.json.
    public static class P327ProfileDigest
    {
        [Serializable] public sealed class Row { public string marker; public double selfMsPerFrame, allocKbPerFrame; public int calls; }
        [Serializable] public sealed class Digest { public string raw; public int frames; public List<Row> topSelf = new List<Row>(), topAlloc = new List<Row>(); }

        public static void Run()
        {
            var raw = Environment.GetEnvironmentVariable("P327_RAW");
            if (string.IsNullOrEmpty(raw) || !File.Exists(raw)) throw new InvalidOperationException("P327_NO_RAW " + raw);
            if (!ProfilerDriver.LoadProfile(raw, false)) throw new InvalidOperationException("P327_LOAD_FAILED " + raw);
            var first = ProfilerDriver.firstFrameIndex;
            var last = ProfilerDriver.lastFrameIndex;
            var self = new Dictionary<string, double>();
            var alloc = new Dictionary<string, double>();
            var calls = new Dictionary<string, int>();
            var frames = 0;
            for (var f = first; f <= last; f++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.Default,
                           HierarchyFrameDataView.columnSelfTime, false))
                {
                    if (view == null || !view.valid) continue;
                    frames++;
                    var children = new List<int>();
                    var stack = new Stack<(int Id, string Parent)>();
                    stack.Push((view.GetRootItemID(), "(root)"));
                    while (stack.Count > 0)
                    {
                        var (id, parentName) = stack.Pop();
                        var name = view.GetItemName(id);
                        if (name == "GC.Alloc")
                        {
                            var owner = parentName;
                            alloc[owner] = (alloc.TryGetValue(owner, out var a) ? a : 0) + view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory);
                        }
                        else if (id != view.GetRootItemID())
                        {
                            self[name] = (self.TryGetValue(name, out var s) ? s : 0) + view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
                            calls[name] = (calls.TryGetValue(name, out var c) ? c : 0) + (int)view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls);
                        }
                        children.Clear();
                        view.GetItemChildren(id, children);
                        foreach (var child in children) stack.Push((child, name));
                    }
                }
            }
            var digest = new Digest { raw = raw, frames = frames };
            foreach (var pair in self.OrderByDescending(p => p.Value).Take(40))
                digest.topSelf.Add(new Row { marker = pair.Key, selfMsPerFrame = pair.Value / Math.Max(1, frames), calls = calls[pair.Key] });
            foreach (var pair in alloc.OrderByDescending(p => p.Value).Take(40))
                digest.topAlloc.Add(new Row { marker = pair.Key, allocKbPerFrame = pair.Value / 1024.0 / Math.Max(1, frames) });
            var output = Path.ChangeExtension(raw, ".digest.json");
            File.WriteAllText(output, JsonUtility.ToJson(digest, true));
            Debug.Log($"P327_DIGEST_WRITTEN {output} frames={frames}");
        }
    }
}
