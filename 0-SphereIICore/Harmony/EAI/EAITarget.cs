using HarmonyLib;
using UnityEngine;

/**
 * SphereII_EAITarget_Tweaks
 * 
 * This class includes a Harmony patch for the EAITarget's check(). It adds additional rules for Human NPCs, such as if the NPC is sleeping,
 * they are the same faction, if you are their leader, your faction standing, etc.
 *
 */
class SphereII_EAITarget_Tweaks
{

    // Enables faction support in the targeting.
    [HarmonyPatch(typeof(EAITarget))]
    [HarmonyPatch("check")]
    public class SphereII_EAITarget_check
    {

        public static bool Postfix(bool __result, EAITarget __instance, EntityAlive _e)
        {
            // If the original method already determined this isn't a valid target, don't bother
            if (!__result)
                return __result;

            // Only "humans" should use faction-based targeting
            if (!EntityUtilities.IsHuman(__instance.theEntity.entityId))
                return __result;

            // If its a vehicle.. umm.. no.
            if (_e is EntityVehicle)
                return false;

            //if (__instance.theEntity.IsSleeping)
            //    return __result;

            // if the attack cool down is applied, don't set a new target.
            if (__instance.theEntity.Buffs.HasBuff("buffAttackCoolDown"))
                return false;
            
            var instanceTargetingEntity = GetTargetingEntity(__instance.theEntity);
            var eTargetingEntity = GetTargetingEntity(_e);

            // Check if the entities have the same ID. This can happen if one or both entities
            // have a leader or owner, and we're using it for targeting purposes.
            if (instanceTargetingEntity.entityId == eTargetingEntity.entityId)
                return false;

            // same faction
            if (instanceTargetingEntity.factionId == eTargetingEntity.factionId)
                return false;

            // We have some complicated checks here, since this method gets called by 3 different target methods.
            FactionManager.Relationship myRelationship = FactionManager.Instance.GetRelationshipTier(
                instanceTargetingEntity,
                eTargetingEntity);

            // Debug.Log("Checking Relationship: " + myRelationship.ToString() + " " + __instance.theEntity.EntityName + " and " + _e.EntityName);
            switch (myRelationship)
            {
                case FactionManager.Relationship.Hate:
                    return true;

                case FactionManager.Relationship.Dislike:
                case FactionManager.Relationship.Neutral:
                    // If you don't like them, or are more or less neutral to them, 
                    // know the difference between an aggressive hit, or just a regular scan of entities.
                    if (__instance is EAISetNearestEntityAsTarget)
                        return false; // they aren't an enemy to kill right off
                    if (__instance is EAISetAsTargetIfHurt)
                        return true;  // They suck! They hurt you. Get them!
                    return false;
                case FactionManager.Relationship.Love:
                case FactionManager.Relationship.Leader:
                    return false;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Gets the entity that should be used for targeting calculations.
    /// If the given entity has a leader or owner, that will be used.
    /// Otherwise, the entity itself will be used.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>The entity that should be used for targeting.</returns>
    private static EntityAlive GetTargetingEntity(EntityAlive entity)
    {
        var leader = EntityUtilities.GetLeaderOrOwner(entity.entityId) as EntityAlive;
        return leader == null ? entity : leader;
    }

}

