using DMT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Human sleepers are automatically awoken when they become active.
/// </summary>
public class HumanSleepersAwaken
{
    /// <summary>
    /// Harmony postfix patch of the EntityAlive.SetSleeperActive method.
    /// If the entity is an EntityNPC or subclass, it is woken up.
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive), "SetSleeperActive")]
    public class EntityAlive_SetSleeperActive
    {
        public static void Postfix(EntityAlive __instance)
        {
            if (__instance is EntityNPC)
                __instance.ConditionalTriggerSleeperWakeUp();
        }
    }
}
