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
using FG.Common.Definition;
using FG.Common.CMS;

namespace fallguyloadrold
{
    public class Patches
    {
        [HarmonyPatch(typeof(ClientGameStateView), "IsGamePlaying", MethodType.Getter)]
        [HarmonyPrefix]
        static bool isgameplaying(ClientGameStateView __instance, ref bool __result)
        {
            __result = LoaderBehaviour.isgameplaying;
            return false;
        }
        [HarmonyPatch(typeof(ClientGameStateView), "IsGameAlive", MethodType.Getter)]
        [HarmonyPrefix]
        static bool isgamealive(ClientGameStateView __instance, ref bool __result)
        {
            __result = LoaderBehaviour.isgameplaying;
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

        [HarmonyPatch(typeof(PlayerNameManager), "GetNameToDisplayForPlayer")]
        [HarmonyPrefix]
        static bool TrackEvent(PlayerNameManager __instance, ref string __result)
        {
            __result = Environment.UserName;
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
                    colourSchemes.Add(LoaderBehaviour.ItemDtoToColourSchemeDto(LoaderBehaviour.CMSDefinitionToItemDto(colourOption.CMSData)));
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
                    patterns.Add(LoaderBehaviour.ItemDtoToPatternDto(LoaderBehaviour.CMSDefinitionToItemDto(patternOption.CMSData)));
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
                    faceplates.Add(LoaderBehaviour.ItemDtoToFaceplateDto(LoaderBehaviour.CMSDefinitionToItemDto(faceplateOption.CMSData)));
                }
            }
            __result = faceplates;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserNameplateSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool NameplateGetList(CustomiserNameplateSection __instance, ref Il2CppSystem.Collections.Generic.List<NameplateDto> __result)
        {
            NameplateOption[] nameplateOptions = Resources.FindObjectsOfTypeAll<NameplateOption>();
            Il2CppSystem.Collections.Generic.List<NameplateDto> nameplates = new Il2CppSystem.Collections.Generic.List<NameplateDto>();

            foreach (NameplateOption nameplateOption in nameplateOptions)
            {
                if (nameplateOption.CMSData != null)
                {
                    nameplates.Add(LoaderBehaviour.ItemDtoToNameplateDto(LoaderBehaviour.CMSDefinitionToItemDto(nameplateOption.CMSData)));
                }
            }
            __result = nameplates;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserNicknameSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool NicknameGetList(CustomiserNicknameSection __instance, ref Il2CppSystem.Collections.Generic.List<NicknameDto> __result)
        {
            NicknamesSO nicknamesSO = Resources.FindObjectsOfTypeAll<NicknamesSO>().FirstOrDefault();
            Il2CppSystem.Collections.Generic.List<NicknameDto> nicknames = new Il2CppSystem.Collections.Generic.List<NicknameDto>();
            foreach (Nickname nickname in nicknamesSO.Nicknames.Values)
            {
                nicknames.Add(LoaderBehaviour.ItemDtoToNicknameDto(LoaderBehaviour.CMSDefinitionToItemDto(nickname)));
            }

            __result = nicknames;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserEmotesSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool NameplateGetList(CustomiserEmotesSection __instance, ref Il2CppSystem.Collections.Generic.List<EmoteDto> __result)
        {
            EmotesOption[] emotesOptions = Resources.FindObjectsOfTypeAll<EmotesOption>();
            Il2CppSystem.Collections.Generic.List<EmoteDto> emotes = new Il2CppSystem.Collections.Generic.List<EmoteDto>();

            foreach (EmotesOption emotesOption in emotesOptions)
            {
                if (emotesOption.CMSData != null)
                {
                    emotes.Add(LoaderBehaviour.ItemDtoToEmoteDto(LoaderBehaviour.CMSDefinitionToItemDto(emotesOption.CMSData)));
                }
            }
            __result = emotes;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserVictorySection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool VictoryGetList(CustomiserVictorySection __instance, ref Il2CppSystem.Collections.Generic.List<PunchlineDto> __result)
        {
            VictoryOption[] victoryOptions = Resources.FindObjectsOfTypeAll<VictoryOption>();
            Il2CppSystem.Collections.Generic.List<PunchlineDto> punchlines = new Il2CppSystem.Collections.Generic.List<PunchlineDto>();

            foreach (VictoryOption victoryOption in victoryOptions)
            {
                if (victoryOption.CMSData != null)
                {
                    punchlines.Add(LoaderBehaviour.ItemDtoToPunchlineDto(LoaderBehaviour.CMSDefinitionToItemDto(victoryOption.CMSData)));
                }
            }
            __result = punchlines;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserLowerCostumeSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool LowerGetList(CustomiserLowerCostumeSection __instance, ref Il2CppSystem.Collections.Generic.List<LowerCostumePieceDto> __result)
        {
            CostumeOption[] costumeOptions = Resources.FindObjectsOfTypeAll<CostumeOption>();
            Il2CppSystem.Collections.Generic.List<LowerCostumePieceDto> lowerCostumePieces = new Il2CppSystem.Collections.Generic.List<LowerCostumePieceDto>();

            foreach (CostumeOption costumeOption in costumeOptions)
            {                
                if (costumeOption.CMSData != null && costumeOption.CostumeType == CostumeType.Bottom)
                {
                    lowerCostumePieces.Add(LoaderBehaviour.ItemDtoToLowerCostumePieceDto(LoaderBehaviour.CMSDefinitionToItemDto(costumeOption.CMSData)));
                }
            }
            __result = lowerCostumePieces;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserUpperCostumeSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool UpperGetList(CustomiserUpperCostumeSection __instance, ref Il2CppSystem.Collections.Generic.List<UpperCostumePieceDto> __result)
        {
            CostumeOption[] costumeOptions = Resources.FindObjectsOfTypeAll<CostumeOption>();
            Il2CppSystem.Collections.Generic.List<UpperCostumePieceDto> upperCostumePieces = new Il2CppSystem.Collections.Generic.List<UpperCostumePieceDto>();

            foreach (CostumeOption costumeOption in costumeOptions)
            {
                if (costumeOption.CMSData != null && costumeOption.CostumeType == CostumeType.Top)
                {
                    upperCostumePieces.Add(LoaderBehaviour.ItemDtoToUpperCostumePieceDto(LoaderBehaviour.CMSDefinitionToItemDto(costumeOption.CMSData)));
                }
            }
            __result = upperCostumePieces;
            return false;
        }

        [HarmonyPatch(typeof(ItemDefinitionSO), "MenuDisplaySprite", MethodType.Getter)]
        [HarmonyPrefix]
        static bool ItemDefinitionSOMenuDisplaySprite(ItemDefinitionSO __instance, ref Sprite __result)
        {            
            if (__instance.ItemType == ItemType.Costume && (__instance.menuDisplaySpriteReference.SubObjectName.Contains("_Top_") | __instance.menuDisplaySpriteReference.SubObjectName.Contains("_Bottom_")))
            {
                string costumetype = "";

                if (__instance.menuDisplaySpriteReference.SubObjectName.Contains("_Top_"))
                {
                    costumetype = "Upper";
                }
                else
                {
                    costumetype = "Lower";
                }

                string path = Paths.PluginPath + $"/fallguyloadr/Assets/{costumetype}Costumes/{__instance.menuDisplaySpriteReference.SubObjectName}.png";
                __result = LoaderBehaviour.PNGtoSprite(path, 118, 134);
            }
            else
            {
                __result = __instance.AtlasLoadableAsset.GetSprite(__instance.menuDisplaySpriteReference.SubObjectName);
            }
            return false;
        }

        [HarmonyPatch(typeof(ShowsManager), "GetActiveShowsDefs")]
        [HarmonyPrefix]
        static bool ShowsManagerGetActiveShowDefs(ShowsManager __instance, ref Il2CppSystem.Collections.Generic.List<ShowDef> __result)
        {
            if (__instance._lastActiveShowDefs.Count < 1)
            {
                var Shows = Resources.FindObjectsOfTypeAll<ShowsSO>().FirstOrDefault().Shows;
                List<string> showIDs = new List<string>();
                foreach (string id in Shows.Keys)
                {
                    showIDs.Add(id);
                }

                for (int i = 0; i < 5; i++)
                {
                    int randomnumber = UnityEngine.Random.Range(0, showIDs.Count);
                    ShowDef showDef = new ShowDef();
                    showDef.ShowFromCMS = Shows[showIDs[randomnumber]];
                    showDef.Index = i;
                    __instance._lastActiveShowDefs.Add(showDef);
                }
            }

            __result = __instance._lastActiveShowDefs;
            return false;
        }

        [HarmonyPatch(typeof(ShowsManager), "IsPermanentActiveShow")]
        [HarmonyPatch(typeof(ShowsManager), "IsShowActive")]
        [HarmonyPrefix]
        static bool ShowsManagerIsPermanentActiveShow(ShowsManager __instance, ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(ShowsManager), nameof(ShowsManager.UpdateAndSaveLastSelectedShows))]
        [HarmonyPrefix]
        static bool ShowsManagerUpdateAndSaveLastSelectedShows(ShowsManager __instance, bool save)
        {
            save = true;
            return true;
        }

        [HarmonyPatch(typeof(ShowSelectorShowElementViewModel), "SetShowElementData")]
        [HarmonyPrefix]
        static bool ShowSelectorShowElementViewModelSetShowElementData(ShowSelectorShowElementViewModel __instance, ShowSelectorViewModel.ShowSelectorElementData elementData)
        {
            __instance._showSelectorVM = elementData.showSelectorVM;
            __instance._showDef = elementData.showDef;
            __instance._description = elementData.showDef.ShowFromCMS.ShowDescription;
            return true;
        }

        [HarmonyPatch(typeof(ShowSelectorShowElementViewModel), "OnClicked")]
        [HarmonyPrefix]
        static bool ShowSelectorShowElementViewModelOnClicked(ShowSelectorShowElementViewModel __instance)
        {
            AudioManager.PlayOneShot(AudioManager.EventMasterData.GenericAccept);
            __instance.RemovePip();
            __instance.Chosen = !__instance.Chosen;
            if (__instance.Chosen)
            {
                ShowsManager.Instance.SelectedShowDef[__instance._showDef] = true;
            }
            else
            {
                ShowsManager.Instance.SelectedShowDef[__instance._showDef] = false;
            }
            return false;
        }

        [HarmonyPatch(typeof(MainMenuViewModel), "OnConnectButtonPressed")]
        [HarmonyPrefix]
        static bool MainMenuViewModelOnConnectButtonPressed(MainMenuViewModel __instance)
        {
            ShowDef selectedShowDef = null;
            foreach (var pair in ShowsManager.Instance.SelectedShowDef)
            {
                if (pair.Value)
                {
                    selectedShowDef = pair.Key;
                }
            }
            LoaderBehaviour.loaderBehaviour.LoadRoundFromShowDef(selectedShowDef);
            try
            {
                __instance.ConnectInitiateTransition(false);
            }
            catch { }
            AudioManager.PlayOneShot(AudioManager.EventMasterData.MainMenuPlay);
            AudioManager.PlayOneShot(AudioManager.EventMasterData.LobbyFall);
            AudioManager.PlayOneShot(AudioManager.EventMasterData.Searching);


            return false;
        }

        [HarmonyPatch(typeof(SubMenuNavigation), "HandleConfigureRequestFailed")]
        [HarmonyPrefix]
        static bool SubMenuNavigationHandleConfigureRequestFailed(SubMenuNavigation __instance)
        {            
            return false;
        }
    }
}
