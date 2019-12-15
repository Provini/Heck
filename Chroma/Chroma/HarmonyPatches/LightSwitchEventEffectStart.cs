﻿using Chroma.Beatmap.Events;
using Chroma.Extensions;
using Chroma.Settings;
using Harmony;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Chroma.HarmonyPatches {

    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(typeof(LightSwitchEventEffect))]
    [HarmonyPatch("Start")]
    class LightSwitchEventEffectStart {

        static void Postfix(LightSwitchEventEffect __instance, ref BeatmapEventType ____event) {
            __instance.StartCoroutine(WaitThenStart(__instance, ____event));
        }

        private static IEnumerator WaitThenStart(LightSwitchEventEffect __instance, BeatmapEventType ____event)
        {
            yield return new WaitForEndOfFrame();
            LightSwitchEventEffectExtensions.LSEStart(__instance, ____event);
        }

    }

}
