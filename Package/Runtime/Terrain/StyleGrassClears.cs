using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleGrassClears
    {
        internal const int Stride = 3;

        const int Marks = 256;

        sealed class Entry
        {
            internal HBStyleGrassClear clear;
            internal Vector4 place;
            internal Vector4 shape;
            internal Vector4 extra;
            internal Vector4 area;
        }

        static readonly List<Entry> s_Entries = new List<Entry>();
        static readonly Vector4[] s_Marks = new Vector4[Marks];

        internal static int version { get; private set; }

        internal static void Register(HBStyleGrassClear clear)
        {
            if (Find(clear) != null)
                return;

            var entry = new Entry { clear = clear };

            Capture(entry);
            s_Entries.Add(entry);
            Mark(entry.area);
        }

        internal static void Release(HBStyleGrassClear clear)
        {
            var entry = Find(clear);

            if (entry == null)
                return;

            s_Entries.Remove(entry);
            Mark(entry.area);
        }

        internal static void Refresh()
        {
            for (var i = s_Entries.Count - 1; i >= 0; i--)
            {
                var entry = s_Entries[i];

                if (entry.clear == null)
                {
                    s_Entries.RemoveAt(i);
                    Mark(entry.area);
                    continue;
                }

                var place = entry.place;
                var shape = entry.shape;
                var extra = entry.extra;
                var area = entry.area;

                Capture(entry);

                if (place != entry.place || shape != entry.shape || extra != entry.extra)
                {
                    Mark(area);
                    Mark(entry.area);
                }
            }
        }

        internal static bool Touched(int seen, Vector4 area)
        {
            if (version - seen > Marks)
                return true;

            for (var mark = seen; mark < version; mark++)
            {
                if (Overlaps(s_Marks[mark % Marks], area))
                    return true;
            }

            return false;
        }

        internal static int Gather(Vector4 area, Vector4[] data, int start, int most)
        {
            var count = 0;

            foreach (var entry in s_Entries)
            {
                if (count == most)
                    break;

                if (!Overlaps(entry.area, area))
                    continue;

                var at = (start + count) * Stride;

                data[at] = entry.place;
                data[at + 1] = entry.shape;
                data[at + 2] = entry.extra;
                count++;
            }

            return count;
        }

        static Entry Find(HBStyleGrassClear clear)
        {
            foreach (var entry in s_Entries)
            {
                if (ReferenceEquals(entry.clear, clear))
                    return entry;
            }

            return null;
        }

        static void Capture(Entry entry)
        {
            var clear = entry.clear;
            var transform = clear.transform;
            var position = transform.position;
            var edge = clear.edge;

            if (clear.shape == HBStyleGrassClearShape.Box)
            {
                var half = clear.scaledSize * 0.5f;
                var right = transform.right;
                var heading = new Vector2(right.x, right.z);
                var turn = heading.sqrMagnitude > 1e-6f ? heading.normalized : Vector2.right;
                var reachX = Mathf.Abs(turn.x) * half.x + Mathf.Abs(turn.y) * half.z + edge;
                var reachZ = Mathf.Abs(turn.y) * half.x + Mathf.Abs(turn.x) * half.z + edge;

                entry.place = new Vector4(position.x, position.y, position.z, -1f);
                entry.shape = new Vector4(half.x, half.z, turn.x, turn.y);
                entry.extra = new Vector4(half.y, edge, 0f, 0f);
                entry.area = new Vector4(position.x - reachX, position.z - reachZ, position.x + reachX,
                                         position.z + reachZ);
            }
            else
            {
                var radius = clear.radius;
                var reach = radius + edge;

                entry.place = new Vector4(position.x, position.y, position.z, radius);
                entry.shape = Vector4.zero;
                entry.extra = new Vector4(0f, edge, 0f, 0f);
                entry.area = new Vector4(position.x - reach, position.z - reach, position.x + reach,
                                         position.z + reach);
            }
        }

        static void Mark(Vector4 area)
        {
            s_Marks[version % Marks] = area;
            version++;
        }

        static bool Overlaps(Vector4 a, Vector4 b)
        {
            return a.x <= b.z && a.z >= b.x && a.y <= b.w && a.w >= b.y;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            foreach (var entry in s_Entries)
                Mark(entry.area);

            s_Entries.Clear();
        }
    }
}
