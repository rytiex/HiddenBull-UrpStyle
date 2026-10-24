using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    struct StyleProfileLayer
    {
        internal HBStyleEnvironmentProfile profile;
        internal float weight;
        internal HBStyleProfileSections sections;
    }

    static class StyleEnvironmentZones
    {
        static readonly List<HBStyleEnvironmentZone> s_Zones = new List<HBStyleEnvironmentZone>();

        static readonly Comparison<HBStyleEnvironmentZone> s_ByPriority =
            (a, b) => a.priority.CompareTo(b.priority);

        internal static void Register(HBStyleEnvironmentZone zone)
        {
            s_Zones.Remove(zone);
            s_Zones.Add(zone);
            Sort();
        }

        internal static void Unregister(HBStyleEnvironmentZone zone)
        {
            s_Zones.Remove(zone);
        }

        internal static void Sort()
        {
            s_Zones.Sort(s_ByPriority);
        }

        internal static void Collect(Vector3 position, List<StyleProfileLayer> layers)
        {
            for (var i = 0; i < s_Zones.Count; i++)
            {
                var zone = s_Zones[i];

                if (zone == null || zone.profile == null || zone.sections == HBStyleProfileSections.None)
                    continue;

                var weight = zone.Weight(position);

                if (weight <= 0f)
                    continue;

                layers.Add(new StyleProfileLayer { profile = zone.profile, weight = weight, sections = zone.sections });
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            s_Zones.Clear();
        }
    }

    sealed class StyleProfileMix
    {
        internal readonly List<StyleProfileLayer> zones = new List<StyleProfileLayer>();

        internal HBStyleEnvironmentProfile source;
        internal HBStyleEnvironmentProfile target;
        internal float blend;

        internal bool single => blend >= 1f && zones.Count == 0;

        internal void Evaluate(Vector3? position)
        {
            target = StyleEnvironment.targetProfile;
            source = StyleEnvironment.sourceProfile;
            blend = source == target ? 1f : StyleEnvironment.profileBlend;

            zones.Clear();

            if (position.HasValue)
                StyleEnvironmentZones.Collect(position.Value, zones);
        }
    }
}
