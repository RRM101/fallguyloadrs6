using BepInEx;
using BepInEx.Unity.IL2CPP;
using FGClient.UI;
using FGClient;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using FGClient.CatapultServices;
using FGClient.UI.Core;
using System.Collections;
using FMODUnity;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FG.Common.CMS;
using System.IO;
using System;
using FG.Common.Definition;
using System.Linq;
using UnityEngine.SceneManagement;
using FG.Common;
using MPG.Utility;
using System.Collections.Generic;
using FG.Common.Character.MotorSystem;
using Levels.Obstacles;
using FG.Common.LODs;
using SRF;
using FGClient.PlayerLevel.UI;
using Catapult.Modules.Items.Protocol.Dtos;
using FallGuys.Player.Protocol.Client.Cosmetics;
using Levels.Rollout;
using FGClient.Customiser;

namespace fallguyloadrold
{
    [BepInPlugin("org.rrm1.fallguyloadr.s6", "fallguyloadr", version)]
    public class Plugin : BasePlugin
    {
        public const string version = "1.2.0";
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<LoaderBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<Fixes.ButtonFix>();
            GameObject obj = new GameObject("Loader Behaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.AddComponent<LoaderBehaviour>();
            Harmony.CreateAndPatchAll(typeof(Patches));
            Harmony.CreateAndPatchAll(typeof(IsGameServerPatches));
            Log.LogInfo($"Plugin fallguyloadr has been loaded!");
        }

        public class LoaderBehaviour : MonoBehaviour
        {
            string text = "round_";
            bool single;
            bool random;
            bool listcmsvariations;
            bool startPressed = false;
            bool loginFailPopupDestroyed = false;
            public static bool isgameplaying = false;
            public static Action IntroCompleteAction;
            public static LoaderBehaviour loaderBehaviour;
            string musicbank;
            string musicevent;
            bool showui = true;
            bool whatisthis = false;
            uint lastplayernetid;
            bool modalmessageobjectfound = false;
            GameObject TopBar;
            GameObject MenuBuilder;
            RoundsData roundsData;
            RoundsSO roundSO;
            RoundsSO[] roundsSO_objects;
            GameObject fallGuy;
            CameraDirector cameraDirector;
            Round mapSet;

            public void Start()
            {
                IntroCompleteAction = IntroComplete;
                loaderBehaviour = this;
                RuntimeManager.LoadBank("BNK_PlayGo");
                Debug.Log("fallguyloadr behaviour loaded");
            }

            public void OnEnable()
            {
                SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
            }

            private void OnGUI()
            {
                // Loader UI
                if (showui)
                {
                    GUI.Box(new Rect(10f, 10f, 120f, 155f), "");
                    GUI.Label(new Rect(30f, 15f, 100f, 30f), "fallguy loadr");
                    text = GUI.TextField(new Rect(20f, 100f, 100f, 25f), text);
                    single = GUI.Button(new Rect(20f, 40f, 100f, 25f), "load single");
                    random = GUI.Button(new Rect(20f, 70f, 100f, 25f), "load random");
                    listcmsvariations = GUI.Button(new Rect(20f, 130f, 100f, 25f), "CMS Rounds");
                    GUI.Label(new Rect(50f, 165f, 100f, 30f), "v"+version);
                    if (whatisthis)
                    {
                        GUI.Label(new Rect(47f, 185f, 100f, 30f), "Press H");
                    }
                }
            }

            public void LoadCMSScene(bool Additive)
            {
                LoadCMS();
                Round round = roundsData[NetworkGameData.currentGameOptions_._roundID];
                if (round != null)
                { 
                    string music_suffix;

                    if (round.IngameMusicSuffix != null)
                    {
                        music_suffix = round.IngameMusicSuffix;
                    }
                    else { music_suffix = ""; }

                    musicbank = $"{round.Archetype.SoundBank}{music_suffix}";
                    musicevent = $"{round.Archetype.SoundEvent}{music_suffix}";

                    if (Additive)
                    {
                        SceneManager.LoadSceneAsync(round.SceneName, LoadSceneMode.Additive);
                    }
                    else { SceneManager.LoadSceneAsync(round.SceneName); }
                    mapSet = round;
                }
            }

            public void LoadCustomizations()
            {
                CustomisationSelections customisationSelections = Singleton<GlobalGameStateClient>.Instance.PlayerProfile.CustomisationSelections;
                EmotesOption[] allEmotes = Resources.FindObjectsOfTypeAll<EmotesOption>();
                SkinPatternOption[] patternOptions = Resources.FindObjectsOfTypeAll<SkinPatternOption>();
                ColourOption[] colourOptions = Resources.FindObjectsOfTypeAll<ColourOption>();
                FaceplateOption[] faceplateOptions = Resources.FindObjectsOfTypeAll<FaceplateOption>();
                NameplateOption[] nameplateOptions = Resources.FindObjectsOfTypeAll<NameplateOption>();
                EmotesOption[] emotearray;
                List<EmotesOption> emotelist = new List<EmotesOption>();
                HashSet<int> uniqueEmotes = new HashSet<int>();
                while (uniqueEmotes.Count < 4)
                {
                    int randomnumber = UnityEngine.Random.Range(0, allEmotes.Length);
                    uniqueEmotes.Add(randomnumber);
                }

                foreach (int emoteNumber in uniqueEmotes)
                {
                    emotelist.Add(Resources.FindObjectsOfTypeAll<EmotesOption>()[emoteNumber]);
                }

                emotearray = emotelist.ToArray();

                customisationSelections.EmoteBottomOption = emotearray[2];
                customisationSelections.EmoteLeftOption = emotearray[3];
                customisationSelections.EmoteRightOption = emotearray[1];
                customisationSelections.EmoteTopOption = emotearray[0];
                customisationSelections.PatternOption = patternOptions[UnityEngine.Random.Range(0, patternOptions.Length)];
                customisationSelections.ColourOption = colourOptions[UnityEngine.Random.Range(0, colourOptions.Length)];
                customisationSelections.FaceplateOption = faceplateOptions[UnityEngine.Random.Range(0, faceplateOptions.Length)];
                customisationSelections.NameplateOption = nameplateOptions[UnityEngine.Random.Range(0, nameplateOptions.Length)];
            }

            public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                isgameplaying = false;
                if (scene.name.Contains("FallGuy_"))
                {
                    RuntimeManager.LoadBank("BNK_UI_MainMenu");
                    AudioManager.PlayOneShot("MUS_InGame_PreparationPhase");
                    cameraDirector = FindObjectOfType<CameraDirector>();
                    RuntimeManager.UnloadBank("BNK_Music_Menu_Season_06");
                    Destroy(TopBar);
                    Destroy(MenuBuilder);
                    SpawnFallGuy();
                    cameraDirector.UseIntroCameras(IntroCompleteAction);
                    try
                    {
                        EnableVariations();
                    }
                    catch { }
                    RolloutManager rolloutManager = FindObjectOfType<RolloutManager>();

                    if (rolloutManager != null)
                    {
                        int index = 0;
                        foreach (RolloutManager.RingSegmentSchema ringSegment in rolloutManager._ringSegmentSchemas)
                        {
                            rolloutManager.InstantiateRing(index, 1);
                            index++;
                        }
                    }
                }
            }

            public void EnableVariations()
            {
                LevelSwitchablesManager variationManager = FindObjectOfType<LevelSwitchablesManager>();
                variationManager.Init(mapSet.SetSwitchers, true);
                SeededRandomisablesManager randomisablesManager = FindObjectOfType<SeededRandomisablesManager>();
                randomisablesManager.RollSeededRandomisables();
            }

            public void IntroComplete()
            {
                RewiredManager rewiredManager = FindObjectOfType<RewiredManager>();
                isgameplaying = true;
                cameraDirector.AddCloseCameraTarget(fallGuy.gameObject, true);
                rewiredManager.SetActiveMap(0, 0);
                if (mapSet != null)
                {
                    RuntimeManager.LoadBank(musicbank);
                    AudioManager.PlayOneShot(musicevent);
                }
                FixObstacles();
            }

            public void SpawnFallGuy()
            {
                GameObject spawn = FindObjectOfType<MultiplayerStartingPosition>().gameObject;
                FallGuysCharacterController fallGuy_ = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().FirstOrDefault();
                fallGuy_.GetComponent<MotorAgent>()._motorFunctionsConfig = MotorAgent.MotorAgentConfiguration.Tutorial;
                Vector3 spawnpos = spawn.transform.position;
                Quaternion rot = spawn.transform.rotation;

                fallGuy = Instantiate(fallGuy_.gameObject, spawnpos, rot);
                fallGuy.GetComponent<FallGuysCharacterController>().IsControlledLocally = true;
                fallGuy.GetComponent<FallGuysCharacterController>().IsLocalPlayer = true;
                CustomisationManager.Instance.ApplyCustomisationsToFallGuy(fallGuy, GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections, -1);
                fallGuy.AddComponent<MPGNetObject>().netID_ = new MPGNetID(1001);
                lastplayernetid = 1001;
            }

            public void LoadCMS()
            {
                roundsSO_objects = Resources.FindObjectsOfTypeAll<RoundsSO>();
                roundSO = roundsSO_objects.First();
                roundsData = roundSO.Rounds;
            }

            public void ListCMSVariations()
            {
                LoadCMS();
                string path = Paths.PluginPath + "/fallguyloadr/CMS/cmsroundlistS6.txt";
                if (File.Exists(path))
                {
                    File.WriteAllText(path, string.Empty);
                }
                else
                {
                    File.Create(path);
                    ListCMSVariations();
                }
                StreamWriter writer = new StreamWriter(path);
                foreach (var key in roundsData.Keys)
                {
                    Round round = roundsData[key];
                    string round_name = CMSLoader.Instance._localisedStrings.GetString(round.DisplayName);
                    round_name = round_name.Replace("[", "").Replace("]", "");
                    string scene_name = round.SceneName;
                    string text = $"{round_name}: {scene_name}: {key}";
                    Debug.Log(text);
                    writer.WriteLine(text);
                }
                writer.Close();
                var content = File.ReadAllLines(path);
                Array.Sort(content);
                File.WriteAllLines(path, content);
            }

            public void FixObstacles()
            {
                COMMON_Button[] buttons = FindObjectsOfType<COMMON_Button>();
                MPGNetObjectBase[] networkAwareGenericObjects = FindObjectsOfType<MPGNetObjectBase>();
                List<MPGNetObject> networkAwareMPGNetObjects = new List<MPGNetObject>();
                COMMON_Pendulum[] pendulums = FindObjectsOfType<COMMON_Pendulum>();

                foreach (COMMON_Button button in buttons)
                {
                    button.gameObject.transform.FindChild("ButtonBody").gameObject.AddComponent<Fixes.ButtonFix>();
                }

                foreach (MPGNetObjectBase networkAwareGenericObject in networkAwareGenericObjects)
                {                   
                    MPGNetObject netObject = networkAwareGenericObject.gameObject.AddComponent<MPGNetObject>();
                    networkAwareMPGNetObjects.Add(netObject);                    
                }

                if (networkAwareMPGNetObjects.Count > 0)
                {
                    HashSet<int> uniqueIDs = new HashSet<int>();
                    while (uniqueIDs.Count < networkAwareMPGNetObjects.Count)
                    {
                        int randomID = UnityEngine.Random.RandomRange(42, 1000);
                        uniqueIDs.Add(randomID);
                    }

                    int[] uniqueIDArray = uniqueIDs.ToArray();

                    int index = 0;
                    foreach (MPGNetObject netObject in networkAwareMPGNetObjects)
                    {
                        netObject.netID_ = new MPGNetID((uint)uniqueIDArray[index]);
                        index++;
                    }
                }

                foreach (COMMON_Pendulum pendulum in pendulums)
                {
                    pendulum.gameObject.RemoveComponentsIfExists<LodController>();
                }
            }

            IEnumerator StartPressed(GameObject TitleScreen)
            {
                RuntimeManager.LoadBank("BNK_Music_GP");
                PlayerTargetSettings.ShowSelectorEnabled = true;
                PlayerTargetSettings.PrivateLobbiesV2Enabled = true;
                CatapultServicesManager servicesManager = FindObjectOfType<CatapultServicesManager>();
                if (!servicesManager._contentService.DoesContentFileExist())
                {
                    File.Copy(Paths.PluginPath + "/fallguyloadr/CMS/content_v1", Application.persistentDataPath + "/content_v1", true);
                }
                CMSLoader cmsLoader = FindObjectOfType<CMSLoader>();
                servicesManager.HandleConnected();
                while (cmsLoader.CMSData == null) { yield return null; }
                var nicknamedict = Resources.FindObjectsOfTypeAll<NicknamesSO>().FirstOrDefault().Nicknames;
                List<string> nicknameKeys = new List<string>();
                foreach (var nicknamekey in nicknamedict.Keys)
                {
                    nicknameKeys.Add(nicknamekey);
                    yield return null;
                }
                GameObject TopLeftGroup = Resources.FindObjectsOfTypeAll<MainMenuViewModel>().FirstOrDefault().transform.GetChild(0).transform.GetChild(3).gameObject;
                PlayerLevelViewModel crownRank = TopLeftGroup.transform.GetChild(0).GetComponent<PlayerLevelViewModel>();
                crownRank.gameObject.GetComponent<ToggleActiveWithFeatureFlag>()._enableIfFeatureDisabled = true;
                crownRank.gameObject.SetActive(true);
                NameTagViewModel nameTagViewModel = TopLeftGroup.transform.GetChild(1).GetComponent<NameTagViewModel>();

                TopBar = TitleScreen.transform.parent.gameObject.transform.GetChild(1).gameObject;
                MenuBuilder = TitleScreen.transform.parent.gameObject.transform.GetChild(0).gameObject;
                TitleScreen.transform.parent.gameObject.transform.GetChild(2).gameObject.SetActive(false);
                LoadingScreenViewModel loadingScreen = FindObjectOfType<LoadingScreenViewModel>();
                loadingScreen.Init(FindObjectOfType<UICanvas>(), new ScreenMetaData { Transition = ScreenTransitionType.FadeOut, TransitionTime = 0.25f });
                loadingScreen.HideScreen();
                try
                {
                    FindObjectOfType<MainMenuManager>().OnSplashScreenComplete();
                }
                catch
                {
                    TopBar.SetActive(true);
                    TopLeftGroup.SetActive(true);
                    RuntimeManager.LoadBank("BNK_Music_Menu_Season_06");
                    AudioManager.PlayOneShot("MUS_MainMenu_Season_06_LP");
                }
                LoadCustomizations();
                FindObjectOfType<MainMenuManager>().ApplyOutfit();
                yield return new WaitForSeconds(0.25f);
                //nameTagViewModel.PlayerName = Environment.UserName;
                nameTagViewModel.Nickname = nicknamedict[nicknameKeys[UnityEngine.Random.Range(0, nicknameKeys.Count)]].Name.Text;
                int crowns = UnityEngine.Random.Range(0, 2000);
                crownRank.SetPlayerLevel(new PlayerLevelData(crowns));
                Destroy(loadingScreen.gameObject);
            }

            public void LoadRandomRound()
            {
                LoadCMS();
                var lines = File.ReadAllLines(Paths.PluginPath + "/fallguyloadr/CMS/randomrounds.txt");
                int randomroundnumber = UnityEngine.Random.Range(0, lines.Length - 1);
                string round_id = lines[randomroundnumber];
                StartCoroutine(LoadRoundWithLoadingScreen(roundsData[round_id], 0).WrapToIl2Cpp());
            }

            public void EasterEgg()
            {
                GameObject easterEgg = Instantiate(fallGuy);
                FallGuysCharacterController fallGuysCharacterController = easterEgg.GetComponent<FallGuysCharacterController>();
                fallGuysCharacterController.IsControlledLocally = true;
                fallGuysCharacterController.IsLocalPlayer = true;
                CustomisationManager.Instance.ApplyCustomisationsToFallGuy(easterEgg, GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections, -1);
                lastplayernetid++;
                easterEgg.GetComponent<MPGNetObject>().netID_ = new MPGNetID(lastplayernetid);
            }

            public static ItemDto CMSDefinitionToItemDto(CMSItemDefinition itemDefinition)
            {
                ItemDto itemDto = new ItemDto();

                itemDto.ContentId = itemDefinition.Id;
                itemDto.Id = itemDefinition.FullItemId;
                itemDto.ContentType = itemDefinition.GroupId;
                itemDto.Quantity = 1;
                return itemDto;
            }

            public static ColourSchemeDto ItemDtoToColourSchemeDto(ItemDto itemDto) // i cant just use IOwnedCosmeticDto if anyone figures out how to use it please make a pr
            {
                ColourSchemeDto cosmeticDto = new ColourSchemeDto();
                cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
                cosmeticDto.Item = itemDto;
                cosmeticDto.IsFavourite = false;
                return cosmeticDto;
            }

            public static PatternDto ItemDtoToPatternDto(ItemDto itemDto)
            {
                PatternDto cosmeticDto = new PatternDto();
                cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
                cosmeticDto.Item = itemDto;
                cosmeticDto.IsFavourite = false;
                return cosmeticDto;
            }

            public static FaceplateDto ItemDtoToFaceplateDto(ItemDto itemDto)
            {
                FaceplateDto cosmeticDto = new FaceplateDto();
                cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
                cosmeticDto.Item = itemDto;
                cosmeticDto.IsFavourite = false;
                return cosmeticDto;
            }

            public static NameplateDto ItemDtoToNameplateDto(ItemDto itemDto)
            {
                NameplateDto cosmeticDto = new NameplateDto();
                cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
                cosmeticDto.Item = itemDto;
                cosmeticDto.IsFavourite = false;
                return cosmeticDto;
            }

            public static NicknameDto ItemDtoToNicknameDto(ItemDto itemDto)
            {
                NicknameDto cosmeticDto = new NicknameDto();
                cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
                cosmeticDto.Item = itemDto;
                cosmeticDto.IsFavourite = false;
                return cosmeticDto;
            }

            public static EmoteDto ItemDtoToEmoteDto(ItemDto itemDto)
            {
                EmoteDto cosmeticDto = new EmoteDto();
                cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
                cosmeticDto.Item = itemDto;
                cosmeticDto.IsFavourite = false;
                return cosmeticDto;
            }

            IEnumerator LoadRoundWithLoadingScreen(Round round, int delay)
            {
                yield return new WaitForSeconds(delay);
                NetworkGameData.SetGameOptionsFromRoundData(round);
                LoadingGameScreenViewModel loadingScreen = FindObjectOfType<UIManager>().ShowScreen<LoadingGameScreenViewModel>(new ScreenMetaData
                {
                    Transition = ScreenTransitionType.FadeInAndOut,
                    ScreenStack = ScreenStackType.Default
                });
                RuntimeManager.UnloadBank("BNK_Music_Menu_Season_06");
                var loadingaudioevent = AudioManager.CreateAudioEvent("MUS_InGame_Loading");
                loadingaudioevent.Value.start();
                yield return new WaitForSeconds(12);
                loadingaudioevent.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                LoadCMSScene(false);
                loadingScreen.HideScreen();
            }

            public void LoadRoundFromShowDef(ShowDef showDef)
            {
                RoundPool roundPool = showDef.ShowFromCMS.DefaultEpisode.RoundPool;
                int randomnumber = UnityEngine.Random.Range(0, roundPool.Stages.Count);
                Round round = roundPool.Stages[randomnumber].Round;
                StartCoroutine(LoadRoundWithLoadingScreen(round, 5).WrapToIl2Cpp());
            }

            public void Update()
            {
                GameObject modalMessageObject = null;
                if (!modalmessageobjectfound)
                {
                    modalMessageObject = FindObjectOfType<UICanvas>().gameObject.transform.GetChild(3).gameObject;
                }

                if (Input.GetKeyDown(KeyCode.F1))
                {
                    showui = !showui;
                }

                if (cameraDirector != null)
                {
                    cameraDirector.HandlePlayerCameraControls();
                }

                if (single)
                {
                    if (musicbank != null)
                    {
                        RuntimeManager.UnloadBank(musicbank);
                    }
                    LoadCMS();
                    NetworkGameData.SetGameOptionsFromRoundData(roundsData[text]);
                    LoadCMSScene(false);
                }

                if (random)
                {
                    if (musicbank != null)
                    {
                        RuntimeManager.UnloadBank(musicbank);
                    }
                    LoadRandomRound();
                }

                if (listcmsvariations)
                {
                    ListCMSVariations();
                }

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    whatisthis = !whatisthis;
                }

                if (Input.GetKeyDown(KeyCode.H) && whatisthis)
                {
                    EasterEgg();
                }

                if (!startPressed)
                {
                    GameObject TitleScreen;
                    try
                    {
                        TitleScreen = FindObjectOfType<TitleScreenViewModel>().gameObject;
                    }
                    catch { TitleScreen = null; }
                    if (TitleScreen != null)
                    {
                        GameObject TSbacking = TitleScreen.transform.FindChild("TitleScreenBacking").gameObject;

                        if (!TSbacking.active)
                        {
                            StartCoroutine(StartPressed(TitleScreen).WrapToIl2Cpp());
                            startPressed = true;
                        }
                    }
                }

                if (!loginFailPopupDestroyed)
                {
                    ModalMessagePopupViewModel popup = FindObjectOfType<ModalMessagePopupViewModel>();
                    if (popup != null && popup.Message == "Failed to login, please check your connection")
                    {
                        //Destroy(popup.transform.parent.gameObject);
                        PopupManager.Instance.DestroyActivePopup();
                        loginFailPopupDestroyed = true;
                    }
                }

                if (SceneManager.GetActiveScene().name == "MainMenu")
                {
                    ModalMessagePopupViewModel popup = FindObjectOfType<ModalMessagePopupViewModel>();
                    if (popup != null && popup.Message.Contains("Processing content failed!"))
                    {
                        PopupManager.Instance.DestroyActivePopup();
                    }

                    if (modalMessageObject != null)
                    {
                        try
                        {
                            if (modalMessageObject.transform.GetChild(0).gameObject.name.Contains("Scrim"))
                            {
                                Destroy(modalMessageObject.transform.GetChild(0).gameObject);
                            }
                        }
                        catch { }
                    }
                }
            }
        }
    }
}