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
using static CatapultAnalytics;
using FG.Common;
using FGClient.Customiser;
using FallGuys.Player.Protocol.Client.Cosmetics;

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

        [HarmonyPatch(typeof(NavigationEvent), "TrackEvent")]
        [HarmonyPrefix]
        static bool TrackEvent(NavigationEvent __instance)
        {
            return false;
        }

        [HarmonyPatch(typeof(CustomiserColourSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool ColourGetList(CustomiserColourSection __instance, ref Il2CppSystem.Collections.Generic.List<ColourSchemeDto> __result)
        {
            ColourOption[] colourOptions = Resources.FindObjectsOfTypeAll<ColourOption>();
            Il2CppSystem.Collections.Generic.List<ColourSchemeDto> colourSchemes = new Il2CppSystem.Collections.Generic.List<ColourSchemeDto>();

            foreach (ColourOption colourOption in colourOptions)
            {
                if (colourOption.CMSData != null)
                {
                    colourSchemes.Add(Plugin.LoaderBehaviour.ItemDtoToColourSchemeDto(Plugin.LoaderBehaviour.CMSDefinitionToItemDto(colourOption.CMSData)));
                }
            }
            __result = colourSchemes;
            return false;
        }


        [HarmonyPatch(typeof(CustomiserPatternsSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool PatternGetList(CustomiserPatternsSection __instance, ref Il2CppSystem.Collections.Generic.List<PatternDto> __result)
        {
            SkinPatternOption[] patternOptions = Resources.FindObjectsOfTypeAll<SkinPatternOption>();
            Il2CppSystem.Collections.Generic.List<PatternDto> patterns = new Il2CppSystem.Collections.Generic.List<PatternDto>();

            foreach (SkinPatternOption patternOption in patternOptions)
            {
                if (patternOption.CMSData != null)
                {
                    patterns.Add(Plugin.LoaderBehaviour.ItemDtoToPatternDto(Plugin.LoaderBehaviour.CMSDefinitionToItemDto(patternOption.CMSData)));
                }
            }
            __result = patterns;
            return false;
        }

        
        [HarmonyPatch(typeof(CustomiserFaceplateSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool FaceplateGetList(CustomiserFaceplateSection __instance, ref Il2CppSystem.Collections.Generic.List<FaceplateDto> __result)
        {
            FaceplateOption[] faceplateOptions = Resources.FindObjectsOfTypeAll<FaceplateOption>();
            Il2CppSystem.Collections.Generic.List<FaceplateDto> faceplates = new Il2CppSystem.Collections.Generic.List<FaceplateDto>();

            foreach (FaceplateOption faceplateOption in faceplateOptions)
            {
                if (faceplateOption.CMSData != null)
                {
                    faceplates.Add(Plugin.LoaderBehaviour.ItemDtoToFaceplateDto(Plugin.LoaderBehaviour.CMSDefinitionToItemDto(faceplateOption.CMSData)));
                }
            }
            __result = faceplates;
            return false;
        }
    }
}
