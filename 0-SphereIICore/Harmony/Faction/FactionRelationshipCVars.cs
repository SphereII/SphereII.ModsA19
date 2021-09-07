using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// This class will add read-only custom variables (cvars), that represent the player's
/// relationship with each non-player faction. Those cvars can be used any place that a cvar can
/// normally be read: effect requirements, localization, XUi player stats entries, etc.
/// 
/// The name of the cvar will be "_relationship[faction name]" where "[faction name]" is the name
/// of the faction, as defined in the faction's "name" attribute in <c>npc.xml</c>.
/// 
/// It must be enabled in the <c>FactionRelationshipCVars</c> property, under
/// <c>AdvancedNPCFeatures</c> in <c>ConfigFeatureBlock</c>.
/// 
/// NOTE: If the relationship is exactly zero, the cvar will not be set. This is because 7D2D
/// treats setting any cvar value to zero as removing the cvar. This can happen if the faction
/// relationship is initially "Hate," or is reduced to zero later. The cvar will be set again if
/// the relationship becomes anything other than zero. Trying to read the value of a non-existent
/// cvar will result in a value of zero anyway, so this should not affect any uses of the cvar.
/// </summary>
public class FactionRelationshipCVars
{
    [HarmonyPatch(typeof(EntityPlayerLocal), "Update")]
    public class FactionRelationshipCVars_EntityPlayerLocal_Update
    {
        private const string CVarPrefix = "_relationship";
        private const string AdvFeatureClass = "AdvancedNPCFeatures";
        private const string Feature = "FactionRelationshipCVars";

        private static bool enabled = false;
        private static Faction[] factions = null;
        private static bool initialized = false;

        private static void Initialize(EntityPlayerLocal __instance)
        {
            initialized = true;
            enabled = Configuration.CheckFeatureStatus(AdvFeatureClass, Feature);
            if (!enabled)
                return;

            // Unfortunately, the Factions field is private
            var factionsInfo = typeof(FactionManager).GetField(
                "Factions",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (factionsInfo != null)
            {
                factions = (Faction[]) factionsInfo.GetValue(FactionManager.Instance);
                SetRelationshipsFromCVars(__instance);
            }
        }

        /// <summary>
        /// This method sets the 7D2D vanilla faction relationships from the cvars.
        /// It is necessary because cvars are saved, but the faction relationships are not -
        /// they are re-read from the XML every time the game loads.
        /// </summary>
        private static void SetRelationshipsFromCVars(EntityPlayerLocal __instance)
        {
            for (var i = 0; i < factions.Length; i++)
            {
                if (factions[i] == null || factions[i].IsPlayerFaction)
                    continue;
                
                var relationship = __instance.GetCVar($"{CVarPrefix}{factions[i].Name}");
                if (relationship != 0f)
                {
                    factions[i].SetRelationship(__instance.factionId, relationship);
                }
            }
        }

        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (!initialized)
                Initialize(__instance);
            
            if (!enabled || factions == null)
                return;

            for (var i = 0; i < factions.Length; i++)
            {
                if (factions[i] == null || factions[i].IsPlayerFaction)
                    continue;
                
                var relationship = factions[i].GetRelationship(__instance.factionId);
                __instance.SetCVar($"{CVarPrefix}{factions[i].Name}", relationship);
            }
        }
    }
}