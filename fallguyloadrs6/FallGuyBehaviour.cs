using FG.Common;
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

        public void Start()
        {
            checkpointManager = FindObjectOfType<CheckpointManager>();
            rigidbody = GetComponent<Rigidbody>();
            mpgNetObject = GetComponent<MPGNetObject>();
        }

        public void Update()
        {
            if (checkpointManager != null && transform.position.y < -50)
            {
                foreach (var checkpoint in checkpointManager._checkpointZones)
                {
                    if (checkpoint.UniqueId == checkpointManager._netIDToCheckpointMap[mpgNetObject.netID()])
                    {
                        checkpoint.GetNextSpawnPositionAndRotation(out var position, out var rotation);

                        transform.position = position;
                        transform.rotation = rotation;
                        rigidbody.velocity = new Vector3(0,0,0);
                        break;
                    }
                }
            }
        }
    }
}
