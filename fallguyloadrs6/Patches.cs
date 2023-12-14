using FGClient.CatapultServices;
using HarmonyLib;
using Levels.CrownMaze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using FGClient;
using UnityEngine;

namespace fallguyloadrold
{
    public class Patches
    {
        [HarmonyPatch(typeof(ClientGameStateView), "IsGamePlaying", MethodType.Getter)]
        [HarmonyPrefix]
        static bool isgameplaying(ClientGameStateView __instance, ref bool __result)
        {
            __result = Plugin.LoaderBehaviour.isgameplaying;
            return false;
        }
        [HarmonyPatch(typeof(ClientGameStateView), "IsGameAlive", MethodType.Getter)]
        [HarmonyPrefix]
        static bool isgamealive(ClientGameStateView __instance, ref bool __result)
        {
            __result = Plugin.LoaderBehaviour.isgameplaying;
            return false;
        }

        [HarmonyPatch(typeof(ClientGameStateView), "RoundRandomSeed", MethodType.Getter)]
        [HarmonyPrefix]
        static bool roundrandomseed(ClientGameStateView __instance, ref int __result)
        {
            __result = DateTime.Now.Millisecond;
            return false;
        }

        [HarmonyPatch(typeof(GlobalGameStateClient), "FixedUpdate")]
        [HarmonyPrefix]
        static bool GGSCfu(GlobalGameStateClient __instance)
        {
            if (__instance.GameStateView.IsGamePlaying)
            {
                __instance.GameStateView.PrevSimulationFixedTime += Time.fixedDeltaTime;
            }
            else
            {
                __instance.GameStateView.PrevSimulationFixedTime = 0;
            }
            return false;
        }

        [HarmonyPatch(typeof(GlobalGameStateClient), "Update")]
        [HarmonyPrefix]
        static bool GGSCu(GlobalGameStateClient __instance)
        {
            if (__instance.GameStateView.IsGamePlaying)
            {
                __instance.GameStateView.PrevSimulationFixedDeltaTime = Time.fixedDeltaTime;
                __instance.GameStateView.PrevSimulationDeltaTime = Time.deltaTime;
                __instance.GameStateView.PrevSimulationTime += Time.deltaTime;
            }
            else
            {
                __instance.GameStateView.PrevSimulationTime = 0;
            }
            return false;
        }
    }
}
