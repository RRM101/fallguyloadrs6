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
using UnityEngine.ResourceManagement.AsyncOperations;
using BepInEx.Configuration;
using System.Text.Json;
using UnityEngine.UI;

namespace fallguyloadrold
{
    [BepInPlugin("org.rrm1.fallguyloadr.s6", "fallguyloadr", version)]
    public class Plugin : BasePlugin
    {
        public const string version = "1.3.0";
        public static ConfigEntry<bool> RandomShows { get; set; }
        public static ConfigEntry<int> Theme { get; set; }

        public override void Load()
        {
            RandomShows = Config.Bind("Config", "Random Shows", true, "Whether to show random shows or shows selected by you in selectedshows.txt");
            Theme = Config.Bind("Config", "Theme", 6, "Choose the theme. Maximum 6, Minimum 1");

            ClassInjector.RegisterTypeInIl2Cpp<LoaderBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<Fixes.ButtonFix>();
            ClassInjector.RegisterTypeInIl2Cpp<FallGuyBehaviour>();
            GameObject obj = new GameObject("Loader Behaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.AddComponent<LoaderBehaviour>();
            Harmony.CreateAndPatchAll(typeof(Patches));
            Harmony.CreateAndPatchAll(typeof(IsGameServerPatches));
            Log.LogInfo($"Plugin fallguyloadr has been loaded!");
        }        
    }

    public class LoaderBehaviour : MonoBehaviour
    {
        string text = "round_";
        string imagefile = "Filename Here";
        string imagewidth = "width";
        string imageheight = "height";
        bool loadimage;
        bool closeimageloader;
        bool single;
        bool random;
        bool imageloader;
        bool imageloaderui = false;
        bool startPressed = false;
        bool loginFailPopupDestroyed = false;
        public static bool isgameplaying = false;
        public static Action IntroCompleteAction;
        public static LoaderBehaviour loaderBehaviour;
        public static string musicbank;
        string musicevent;
        string menumusicbank;
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
        public ShowDef currentShowDef;

        public void Start()
        {
            if (loaderBehaviour != null)
            {
                Destroy(this);
            }
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
                if (!imageloaderui)
                {
                    GUI.Box(new Rect(10f, 10f, 120f, 155f), "");
                    GUI.Label(new Rect(30f, 15f, 100f, 30f), "fallguy loadr");
                    text = GUI.TextField(new Rect(20f, 100f, 100f, 25f), text);
                    single = GUI.Button(new Rect(20f, 40f, 100f, 25f), "load");
                    random = GUI.Button(new Rect(20f, 70f, 100f, 25f), "load random");
                    imageloader = GUI.Button(new Rect(20f, 130f, 100f, 25f), "Image Loader");
                    if (whatisthis)
                    {
                        GUI.Label(new Rect(47f, 185f, 100f, 30f), "Press H");
                        text = GUI.TextField(new Rect(20f, 100f, 100f, 25f), text);
                    }
                }
                else
                {
                    GUI.Box(new Rect(10f, 10f, 120f, 155f), "");
                    GUI.Label(new Rect(30f, 15f, 100f, 30f), "Image Loader");
                    imagefile = GUI.TextField(new Rect(20f, 40f, 100f, 25f), imagefile);
                    imagewidth = GUI.TextField(new Rect(20f, 70f, 47.5f, 25f), imagewidth);
                    imageheight = GUI.TextField(new Rect(73f, 70f, 47.5f, 25f), imageheight);
                    loadimage = GUI.Button(new Rect(20f, 100f, 100f, 25f), "Load"); 
                    closeimageloader = GUI.Button(new Rect(20f, 130f, 100f, 25f), "Back");
                }
                GUI.Label(new Rect(50f, 165f, 100f, 30f), "v" + Plugin.version);
            }
        }

        public void AddCMSStringKeys()  // From CEP
        {
            Dictionary<string, string> stringsToAdd = new Dictionary<string, string>()
            {
                {"xtreme_title", "QUIT GAME"},
                {"xtreme_message_insult", "Seriously, why did you pick XTREME mode?"},
                {"imageloader_error", "ERROR"},
                {"imageloader_number_error_message", "Please enter a number for the height and width."},
                {"imageloader_file_not_found_error_message", "File not found."},
                {"imageloader_success", "SUCCESS"},
                {"imageloader_success_message", "Successfully loaded image"}
            };

            foreach (var toAdd in stringsToAdd) AddNewStringToCMS(toAdd.Key, toAdd.Value);
        }

        public void AddNewStringToCMS(string key, string value)
        {
            if (!CMSLoader.Instance._localisedStrings._localisedStrings.ContainsKey(key))
            {
                CMSLoader.Instance._localisedStrings._localisedStrings.Add(key, value);
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
            VictoryOption[] victoryOptions = Resources.FindObjectsOfTypeAll<VictoryOption>();
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
            customisationSelections.VictoryPoseOption = victoryOptions[UnityEngine.Random.Range(0, victoryOptions.Length)];
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isgameplaying = false;
            RuntimeManager.UnloadBank("BNK_SFX_WinnerScreen");
            if (scene.name.Contains("FallGuy_"))
            {
                RuntimeManager.LoadBank("BNK_UI_MainMenu");
                AudioManager.PlayOneShot("MUS_InGame_PreparationPhase");
                cameraDirector = FindObjectOfType<CameraDirector>();
                RuntimeManager.UnloadBank(menumusicbank);
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
                PlayerRatioedBulkItemSpawner bulkItemSpawner = FindObjectOfType<PlayerRatioedBulkItemSpawner>();

                if (rolloutManager != null)
                {
                    int index = 0;
                    foreach (RolloutManager.RingSegmentSchema ringSegment in rolloutManager._ringSegmentSchemas)
                    {
                        rolloutManager.InstantiateRing(index, 1);
                        index++;
                    }
                }

                if (bulkItemSpawner != null)
                {
                    Transform ItemParent;

                    if (bulkItemSpawner.ItemParent != null)
                    {
                        ItemParent = bulkItemSpawner.ItemParent;
                    }
                    else
                    {
                        ItemParent = bulkItemSpawner.ItemParents[0];
                    }

                    int spawns = ItemParent.GetChildCount();

                    for (int i = 0; i < spawns; i++)
                    {
                        int randomnumber = UnityEngine.Random.Range(0, spawns - 1);
                        Transform spawn = ItemParent.GetChild(randomnumber);
                        Instantiate(bulkItemSpawner.ItemPrefab, spawn.position, spawn.rotation);
                    }
                }
            }
            else if (scene.name.Contains("Fallguy_"))
            {
                RuntimeManager.UnloadBank(musicbank);
                StartCoroutine(PlayVictoryAnimation().WrapToIl2Cpp());
            }
        }

        IEnumerator PlayVictoryAnimation()
        {
            VictoryScene victoryScene = FindObjectOfType<VictoryScene>();
            GameObject fallguy = victoryScene.InstantiateFallguy(victoryScene._fallguySpawnPosition);
            VictoryOption victoryOption = victoryScene._playerProfile.CustomisationSelections.VictoryPoseOption;
            GameObject animprop = victoryScene.CreateVictoryAnimationProp(victoryOption);
            AnimationClip animationClip = null;
            try
            {
                victoryScene.PlayVictoryAnimations(fallguy, victoryOption, animprop);
            }
            catch { }
            if (victoryOption.clipAssetRef.Asset == null)
            {
                var victoryasset = victoryOption.clipAssetRef.LoadAsset<AnimationClip>();
                yield return victoryasset;
                if (victoryasset.Status == AsyncOperationStatus.Succeeded)
                {
                    animationClip = victoryasset.Result;
                }
            }
            else
            {
                animationClip = victoryOption.clipAssetRef.Asset.Cast<AnimationClip>();
            }
            yield return null;
            string victoryname = animationClip.name;

            fallguy.GetComponentInChildren<Animator>().Play(victoryname);
            victoryScene.PlayMusic();
            victoryScene.CustomiseFallguy(fallguy, victoryScene._playerProfile.CustomisationSelections, -1);
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
            fallGuy.AddComponent<FallGuyBehaviour>();
            lastplayernetid = 1001;
        }

        public void LoadCMS()
        {
            roundsSO_objects = Resources.FindObjectsOfTypeAll<RoundsSO>();
            roundSO = roundsSO_objects.First();
            roundsData = roundSO.Rounds;
        }

        public void ListShows()
        {
            LoadCMS();
            string path = Paths.PluginPath + "/fallguyloadr/CMS/showlist.txt";
            if (File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }
            else
            {
                File.Create(path).Close();
                ListShows();
            }
            StreamWriter writer = new StreamWriter(path);
            ShowsData showsData = CMSLoader.Instance.CMSData.Shows;
            foreach (var key in showsData.Keys)
            {
                Show show = showsData[key];
                try
                {
                    string showName = show.ShowName.Text;

                string text = $"{key}: {showName}";

                    Debug.Log(text);
                    writer.WriteLine(text);
                }
                catch { }
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
            servicesManager.HandleConnected();
            while (CMSLoader.Instance.CMSData == null) { yield return null; }
            AddCMSStringKeys();
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

            if (Plugin.Theme.Value < 6 && Plugin.Theme.Value > 0)
            {
                string jsonpath = $"{Paths.PluginPath}/fallguyloadr/Assets/Themes/Season{Plugin.Theme.Value}_Theme.json";
                string jsondata = File.ReadAllText(jsonpath);
                Theme theme = JsonSerializer.Deserialize<Theme>(jsondata);
                CMSLoader.Instance.CMSData.SettingsAudio["main_menu_music_soundbank"].Value = theme.music_bank;
                CMSLoader.Instance.CMSData.SettingsAudio["main_menu_music_event"].Value = theme.music_event;
                GameObject background = GameObject.Find("Generic_UI_Season6Background_Canvas");
                SetBackground(theme, background);

                GameObject loadingScreenBackground = Resources.FindObjectsOfTypeAll<LoadingGameScreenViewModel>().FirstOrDefault().gameObject.transform.GetChild(0).transform.GetChild(0).gameObject;
                SetBackground(theme, loadingScreenBackground);

                GameObject roundRevealBackground = Resources.FindObjectsOfTypeAll<RoundRevealCarouselViewModel>().FirstOrDefault().gameObject.transform.GetChild(1).transform.GetChild(0).gameObject;
                SetBackground(theme, roundRevealBackground);
            }

            try
            {
                FindObjectOfType<MainMenuManager>().OnSplashScreenComplete();
            }
            catch
            {
                TopBar.SetActive(true);
                TopLeftGroup.SetActive(true);
                RuntimeManager.LoadBank(menumusicbank);
                AudioManager.PlayOneShot("MUS_MainMenu_Season_06_LP");
            }

            CMSLoader.Instance.CMSData.SettingsAudio.TryGetValue("main_menu_music_soundbank", out var value);
            menumusicbank = value.Value;
            LoadCustomizations();
            FindObjectOfType<MainMenuManager>().ApplyOutfit();
            yield return new WaitForSeconds(0.25f);
            //nameTagViewModel.PlayerName = Environment.UserName;
            nameTagViewModel.Nickname = nicknamedict[nicknameKeys[UnityEngine.Random.Range(0, nicknameKeys.Count)]].Name.Text;
            int crowns = UnityEngine.Random.Range(0, 2000);
            crownRank.SetPlayerLevel(new PlayerLevelData(crowns));
            Destroy(loadingScreen.gameObject);
        }

        public void SetBackground(Theme theme, GameObject background)
        {
            if (theme.pattern != null)
            {
                background.transform.GetChild(0).FindChild("Pattern").gameObject.GetComponent<Image>().sprite = PNGtoSprite($"{Paths.PluginPath}/fallguyloadr/Assets/Themes/{theme.pattern}", 0, 0);
                background.transform.GetChild(0).FindChild("Pattern").gameObject.GetComponent<Image>().color = new Color(theme.circlesrgb[0], theme.circlesrgb[1], theme.circlesrgb[2]);
            }
            else
            {
                Destroy(background.transform.GetChild(0).FindChild("Pattern").gameObject);
            }
            background.transform.GetChild(0).FindChild("Circles").gameObject.GetComponent<Image>().color = new Color(theme.circlesrgb[0], theme.circlesrgb[1], theme.circlesrgb[2]);
            background.transform.GetChild(0).FindChild("Backdrop").gameObject.GetComponent<Image>().color = new Color(theme.uppergradientrgb[0], theme.uppergradientrgb[1], theme.uppergradientrgb[2]);
            background.transform.GetChild(0).FindChild("Gradient").gameObject.GetComponent<Image>().color = new Color(theme.lowergradientrgb[0], theme.lowergradientrgb[1], theme.lowergradientrgb[2]);
        }

        public void LoadRandomRound()
        {
            LoadCMS();
            currentShowDef = null;
            RuntimeManager.UnloadBank("BNK_SFX_WinnerScreen");
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

        public static PunchlineDto ItemDtoToPunchlineDto(ItemDto itemDto)
        {
            PunchlineDto cosmeticDto = new PunchlineDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static LowerCostumePieceDto ItemDtoToLowerCostumePieceDto(ItemDto itemDto)
        {
            LowerCostumePieceDto cosmeticDto = new LowerCostumePieceDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static UpperCostumePieceDto ItemDtoToUpperCostumePieceDto(ItemDto itemDto)
        {
            UpperCostumePieceDto cosmeticDto = new UpperCostumePieceDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        IEnumerator LoadRoundWithLoadingScreen(Round round, int delay)
        {
            isgameplaying = false;
            yield return new WaitForSeconds(delay);
            NetworkGameData.SetGameOptionsFromRoundData(round);
            LoadingGameScreenViewModel loadingScreen = UIManager.Instance.ShowScreen<LoadingGameScreenViewModel>(new ScreenMetaData
            {
                Transition = ScreenTransitionType.FadeInAndOut,
                ScreenStack = ScreenStackType.Default
            });
            RuntimeManager.UnloadBank(menumusicbank);
            var loadingaudioevent = AudioManager.CreateAudioEvent("MUS_InGame_Loading");
            loadingaudioevent.Value.start();
            yield return new WaitForSeconds(12);
            loadingaudioevent.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            LoadCMSScene(false);
            loadingScreen.HideScreen();
        }

        public void LoadRoundFromShowDef(ShowDef showDef)
        {
            currentShowDef = showDef;
            RoundPool roundPool = showDef.ShowFromCMS.DefaultEpisode.RoundPool;
            int randomnumber = UnityEngine.Random.Range(0, roundPool.Stages.Count);
            Round round = roundPool.Stages[randomnumber].Round;
            StartCoroutine(LoadRoundWithLoadingScreen(round, 5).WrapToIl2Cpp());
        }

        public static Sprite PNGtoSprite(string path, int width, int height)
        {
            byte[] imagedata = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            ImageConversion.LoadImage(texture, imagedata);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            return sprite;
        }

        public void LoadImage()
        {
            string path = $"{Paths.PluginPath}/fallguyloadr/Assets/UserImages/{imagefile}";

            if (!File.Exists(path))
            {
                ModalMessageData modalMessageData = new ModalMessageData
                {
                    Title = "imageloader_error",
                    Message = "imageloader_file_not_found_error_message",
                    ModalType = UIModalMessage.ModalType.MT_OK
                };
                PopupManager.Instance.Show(modalMessageData);
                return;
            }
            
            GameObject imageGameObject = new GameObject(imagefile);
            imageGameObject.transform.position = fallGuy.transform.position;
            bool heightsuccess = int.TryParse(imageheight,out int height);
            bool widthsuccess = int.TryParse(imagewidth,out int width);

            if (heightsuccess && widthsuccess)
            {
                imageGameObject.AddComponent<SpriteRenderer>().sprite = PNGtoSprite(path, width, height);
                ModalMessageData modalMessageData = new ModalMessageData
                {
                    Title = "imageloader_success",
                    Message = "imageloader_success_message",
                    ModalType = UIModalMessage.ModalType.MT_OK
                };
                PopupManager.Instance.Show(modalMessageData);
            }
            else
            {
                ModalMessageData modalMessageData = new ModalMessageData
                {
                    Title = "imageloader_error",
                    Message = "imageloader_number_error_message",
                    ModalType = UIModalMessage.ModalType.MT_OK
                };
                PopupManager.Instance.Show(modalMessageData);
                Destroy(imageGameObject);
            }
        }

        IEnumerator WinEnumerator()
        {
            isgameplaying = false;
            RuntimeManager.UnloadBank(musicbank);
            RoundEndedScreenViewModel.Show(null);
            AudioManager.PlayOneShot(AudioManager.EventMasterData.RoundOver);
            yield return new WaitForSeconds(2.5f);
            WinnerScreenViewModel.Show("winner", true, null);
            AudioManager.PlayGameplayEndAudio(true);
            yield return new WaitForSeconds(3);
            SceneManager.LoadSceneAsync("Fallguy_Victory_Scene");
        }

        public void Win()
        {
            StartCoroutine(WinEnumerator().WrapToIl2Cpp());
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
                currentShowDef = null;
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

            if (imageloader)
            {
                imageloaderui = true;
            }

            if (closeimageloader)
            {
                imageloaderui = false;
                closeimageloader = false;
            }

            if (loadimage)
            {
                LoadImage();
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