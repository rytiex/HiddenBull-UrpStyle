using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleLiquidBakeHook
    {
        const string ProbeStartName = "OnBakeStarted";

        static bool s_Probing;
        static bool s_Rerun;

        static StyleLiquidBakeHook()
        {
            var probes = ProbeStart();

            Lightmapping.bakeStarted -= Started;

            if (probes != null)
                Lightmapping.bakeStarted -= probes;

            Lightmapping.bakeStarted += Started;

            if (probes != null)
                Lightmapping.bakeStarted += probes;

            Lightmapping.bakeCompleted -= Finished;
            Lightmapping.bakeCompleted += Finished;
            EditorApplication.update -= Watch;
            EditorApplication.update += Watch;
            EditorApplication.delayCall += StyleLiquidBakeLight.Clear;
        }

        static Action ProbeStart()
        {
            var type = typeof(AdaptiveProbeVolumes);

            RuntimeHelpers.RunClassConstructor(type.TypeHandle);

            var method = type.GetMethod(ProbeStartName, BindingFlags.NonPublic | BindingFlags.Static, null,
                                        Type.EmptyTypes, null);

            return method != null && method.ReturnType == typeof(void)
                ? (Action)Delegate.CreateDelegate(typeof(Action), method, false)
                : null;
        }

        static void Started()
        {
            StyleBakeEnvironment.Begin();
            StyleLiquidBakeLight.Create();
        }

        static void Finished()
        {
            StyleLiquidBakeLight.Clear();
            StyleBakeLightmaps.QueueStrip();
        }

        static void Watch()
        {
            var probing = AdaptiveProbeVolumes.isRunning;

            var neutral = probing && !s_Probing && StyleBakeEnvironment.Begin();

            if (probing && !s_Probing && !Lightmapping.isRunning && (neutral || !StyleLiquidBakeLight.built))
            {
                if (!StyleLiquidBakeLight.built)
                    StyleLiquidBakeLight.Create();

                if ((neutral || StyleLiquidBakeLight.built) && AdaptiveProbeVolumes.Cancel())
                {
                    s_Rerun = true;
                    probing = AdaptiveProbeVolumes.isRunning;
                }
            }
            else if (s_Rerun && !probing)
            {
                s_Rerun = false;
                probing = AdaptiveProbeVolumes.BakeAsync();
            }

            s_Probing = probing;
            StyleBakeEnvironment.Watch(probing || Lightmapping.isRunning || s_Rerun);

            if (!s_Rerun && StyleLiquidBakeLight.built && !probing && !Lightmapping.isRunning)
                StyleLiquidBakeLight.Clear();
        }
    }
}
