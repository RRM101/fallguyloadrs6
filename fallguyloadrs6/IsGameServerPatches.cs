using FG.Common;
using HarmonyLib;
using Levels.Obstacles;
using Levels.WallGuys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Levels.Obstacles.COMMON_PrefabSpawnerBase;

namespace fallguyloadrold
{
    public class IsGameServerPatches
    {
        [HarmonyPatch(typeof(WallGuysSegmentGenerator), "Awake")]
        [HarmonyPrefix]
        static bool WallGuysGeneratorAwake(WallGuysSegmentGenerator __instance)
        {
            __instance._collider = __instance.gameObject.GetComponent<BoxCollider>();
            FGRandom.Create(__instance._objectId, __instance.GameState.RoundRandomSeed);

            __instance.CreateSegmentObstacles();
            __instance._collider.enabled = false;

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "Start")]
        [HarmonyPrefix]
        static bool PrefabSpawnerStart(COMMON_PrefabSpawnerBase __instance)
        {
            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "CanPerformSpawn")]
        [HarmonyPrefix]
        static bool PrefabSpawnerCanSpawn(COMMON_PrefabSpawnerBase __instance, ref bool __result)
        {
            __result = true;

            return true;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "Spawn")]
        [HarmonyPrefix]
        static bool PrefabSpawnerSpawn(COMMON_PrefabSpawnerBase __instance)
        {
            SpawnerEntry randomValidSpawnEntry = __instance.GetRandomValidSpawnEntry(false);
            __instance.InstantiateObject(randomValidSpawnEntry, __instance.GetInitialPosition());
            __instance._lastEntriesSpawned.Add(randomValidSpawnEntry);
            __instance.ClearLastEntriesSpawned();
            try
            {
                __instance.onSpawn?.Invoke();
            }
            catch { }

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "InstantiateObject")]
        [HarmonyPrefix]
        static bool PrefabSpawnerInstantiateObject(COMMON_PrefabSpawnerBase __instance, SpawnerEntry entry, Vector3 spawnPosition)
        {
            Quaternion initialRotation = __instance.GetInitialRotation(entry);
            Vector3 spawnScale = entry.value.transform.localScale;
            GameObject gameObject = UnityEngine.Object.Instantiate(entry.value);
            gameObject.transform.position = spawnPosition;
            gameObject.transform.localScale = spawnScale;
            gameObject.transform.rotation = initialRotation;
            __instance.OnInstantiateObject(gameObject, entry);

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerTimed), "Start")]
        [HarmonyPrefix]
        static bool PrefabSpawnerTimedStart(COMMON_PrefabSpawnerTimed __instance)
        {
            __instance._timeUntilNextSpawn = __instance.initialDelayInSeconds;

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "TrySpawn")]
        [HarmonyPrefix]
        static bool PrefabSpawnerTrySpawn(COMMON_PrefabSpawnerBase __instance)
        {
            __instance.Spawn(null);

            return false;
        }

        [HarmonyPatch(typeof(COMMON_Pendulum), "Start")]
        [HarmonyPrefix]
        static bool PendulumStart(COMMON_Pendulum __instance)
        {
            __instance._worldToLocalRotationProjection = Quaternion.Euler(-__instance.transform.rotation.eulerAngles);
            __instance._localToWorldRotationProjection = Quaternion.Euler(__instance.transform.rotation.eulerAngles);
            __instance._swingVelocity = __instance.CalcRandomScaledSwingVelocity(__instance._localToWorldRotationProjection * new Vector3(__instance._velocityThreshold, 0f, 0f));
            __instance._canPush = true;
            __instance._delay = 0f;

            return false;
        }
    }
}
