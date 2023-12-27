using FG.Common;
using FGClient.UI;
using Levels.Progression;
using System;
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

        public void Start()
        {
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

        public void Update()
        {
            if (checkpointManager != null && transform.position.y < -50)
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
        }
    }
}
