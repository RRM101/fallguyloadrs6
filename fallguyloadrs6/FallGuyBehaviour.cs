using BepInEx.Unity.IL2CPP.Utils.Collections;
using FG.Common;
using FG.Common.Audio;
using FGClient;
using FGClient.UI;
using FGClient.UI.Core;
using FMODUnity;
using Levels.Progression;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace fallguyloadrold
{
    public class FallGuyBehaviour : MonoBehaviour
    {
        CheckpointManager checkpointManager;
        Rigidbody rigidbody;
        MPGNetObject mpgNetObject;
        bool isXtreme = false;
        bool xtremePopupOpened = false;
        bool isInShow = false;
        bool hasQualified = false;

        public void Start()
        {
            isInShow = LoaderBehaviour.loaderBehaviour.currentShowDef == null ? false : true;
            checkpointManager = FindObjectOfType<CheckpointManager>();
            rigidbody = GetComponent<Rigidbody>();
            mpgNetObject = GetComponent<MPGNetObject>();
            if (GameObject.Find("SlimeSurvival").transform.FindChild("Slime").gameObject.active) { isXtreme = true; }
        }

        public void CloseGame(bool Clicked)
        {
            if (Clicked)
            {
                Application.Quit();
            }
        }

        public void ShowXtremePopup()
        {
            Il2CppSystem.Action<bool> quitAction = new System.Action<bool>(CloseGame);

            ModalMessageData modalMessageData = new ModalMessageData
            {
                Title = "xtreme_title",
                Message = "xtreme_message_insult",
                ModalType = UIModalMessage.ModalType.MT_OK,
                OnCloseButtonPressed = quitAction
            };
            PopupManager.Instance.Show(modalMessageData);
        }

        IEnumerator Qualify()
        {
            RuntimeManager.UnloadBank(LoaderBehaviour.musicbank);
            LoaderBehaviour.isgameplaying = false;
            QualifiedScreenViewModel.Show("qualified", null);
            AudioManager.PlayGameplayEndAudio(true);
            yield return new WaitForSeconds(3.5f);
            RoundEndedScreenViewModel.Show(null);
            AudioManager.PlayOneShot(AudioManager.EventMasterData.RoundOver);
            yield return new WaitForSeconds(3);
            RoundRevealCarouselViewModel roundRevealCarousel = UIManager.Instance.ShowScreen<RoundRevealCarouselViewModel>(new ScreenMetaData
            {
                Transition = ScreenTransitionType.FadeInAndOut
            });
            LoaderBehaviour.loaderBehaviour.LoadRoundFromShowDef(LoaderBehaviour.loaderBehaviour.currentShowDef);
            yield return new WaitForSeconds(5);
            roundRevealCarousel.HideScreen();
        }

        void OnTriggerEnter(Collider collision)
        {
            if (collision.gameObject.GetComponent<EndZoneVFXTrigger>() != null || collision.gameObject.GetComponent<COMMON_ObjectiveReachEndZone>() != null && isInShow)
            {
                if (!hasQualified)
                {
                    hasQualified = true;
                    StartCoroutine(Qualify().WrapToIl2Cpp());
                }
            }
        }

        public void Update()
        {
            if (transform.position.y < -50 || Input.GetKeyDown(KeyCode.R))
            {
                if (checkpointManager != null)
                {
                    foreach (var checkpoint in checkpointManager._checkpointZones)
                    {
                        if (!isXtreme)
                        {
                            try
                            {
                                if (checkpoint.UniqueId == checkpointManager._netIDToCheckpointMap[mpgNetObject.netID()])
                                {
                                    checkpoint.GetNextSpawnPositionAndRotation(out var position, out var rotation);

                                    transform.position = position;
                                    transform.rotation = rotation;
                                    rigidbody.velocity = new Vector3(0, 0, 0);
                                    break;
                                }
                            }
                            catch
                            {
                                MultiplayerStartingPosition multiplayerStartingPosition = FindObjectOfType<MultiplayerStartingPosition>();
                                transform.position = multiplayerStartingPosition.transform.position;
                                transform.rotation = multiplayerStartingPosition.transform.rotation;
                                rigidbody.velocity = new Vector3(0, 0, 0);
                            }
                        }
                        else
                        {
                            if (!xtremePopupOpened)
                            {
                                ShowXtremePopup();
                                xtremePopupOpened = true;
                            }
                        }
                    }
                }
                else
                {
                    MultiplayerStartingPosition multiplayerStartingPosition = FindObjectOfType<MultiplayerStartingPosition>();
                    transform.position = multiplayerStartingPosition.transform.position;
                    transform.rotation = multiplayerStartingPosition.transform.rotation;
                    rigidbody.velocity = new Vector3(0, 0, 0);
                }
            }
        }
    }
}
