using System.Xml;
using UnityEngine;

/// <summary>
/// Changes the target's relationship with the specified faction, and all the specified related
/// factions (if any), according to the relationships between the faction and the related factions.
/// </summary>
/// <example>
/// <code>
/// <!--
///     Gives 10 points to the player's relationship with bandits, and adjusts the player's
///     relationship with the whisperer faction according to its current relationship with bandits.
///     So if the whisperer faction loves bandits, add 6.66.. points to the player's relationship
///     with whisperers; if the whisperer faction dislikes (but doesn't hate) bandits, subtract 
///     3.33.. points from the player's relationship with whisperers.
/// -->
/// <triggered_effect trigger="onSelfBuffStart" action="ModifyRelatedFactionsSDX, Mods" target="self" faction="bandits" related="whisperers" value="10" />
/// <!--
///     Subtracts 10 points to the player's relationship with white river, and adjust the player's
///     relationships with the bandit, red team, blue team, and green team factions according to
///     their current relationships with white river.
/// -->
/// <triggered_effect trigger="onSelfPrimaryActionEnd" action="ModifyRelatedFactionsSDX, Mods" target="self" faction="whiteriver" related="bandit,redteam,blueteam,greenteam" value="-10" />
/// <!--
///     Adds 10 points to the player's relationship with the red team.
///     Since the "related" attribute is omitted, it does not modify the relationships with any
///     other factions.
///     In this case, it behaves exactly like ModifyFactionSDX.
/// -->
/// <triggered_effect trigger="onSelfPrimaryActionEnd" action="ModifyRelatedFactionsSDX, Mods" target="self" faction="redteam" value="10" />
/// </code>
/// </example>
public class MinEventActionModifyRelatedFactionsSDX : MinEventActionRemoveBuff
{
    private bool debug = true; // for logging when testing

    private string faction = "";

    private string[] relatedFactions = null;

    private float value = 0f;

    public override void Execute(MinEventParams _params)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            EntityAlive entity = targets[i];
            if (entity != null)
            {
                Faction otherFaction = FactionManager.Instance.GetFactionByName(faction);
                if (otherFaction != null)
                {
                    otherFaction.ModifyRelationship(entity.factionId, value);
                    if (debug)
                    {
                        Debug.Log(faction + " relationship modified by " + value);
                    }
                    ModifyRelatedFactionRelationships(entity.factionId, otherFaction);
                }
            }
        }
    }

    private void ModifyRelatedFactionRelationships(byte targetEntityFactionId, Faction otherFaction)
    {
        if (relatedFactions == null)
        {
            return;
        }

        for (int i = 0; i < relatedFactions.Length; i++)
        {
            Faction relatedFaction = FactionManager.Instance.GetFactionByName(relatedFactions[i]);

            // Guard against users entering a related faction name that doesn't exist, is the
            // name of a target entity's faction, or is the same as the "faction" attribute
            if (relatedFaction != null &&
                relatedFaction.ID != otherFaction.ID &&
                relatedFaction.ID != targetEntityFactionId)
            {
                float modifier = CalculateRelatedFactionModifier(
                    relatedFaction.GetRelationship(otherFaction.ID),
                    value);

                relatedFaction.ModifyRelationship(targetEntityFactionId, modifier);

                if (debug)
                {
                    Debug.Log(relatedFactions[i] + " relationship modified by " + modifier);
                }
            }
        }
    }

    private static float CalculateRelatedFactionModifier(float relationshipToFaction, float modifier)
    {
        float ratio = 1; // default to Leader

        if (relationshipToFaction < 200f) // Hate
        {
            ratio = -0.6666667f;
        }
        else if (relationshipToFaction < 400f) // Dislike
        {
            ratio = -0.3333333f;
        }
        else if (relationshipToFaction < 600f) // Neutral
        {
            ratio = 0f;
        }
        else if (relationshipToFaction < 800f) // Like
        {
            ratio = 0.3333333f;
        }
        else if (relationshipToFaction < 1001f) // Love
        {
            ratio = 0.6666667f;
        }

        float relationshipToTarget = ratio * modifier;

        // The value 255f has a special meaning in TFP code, so avoid it
        return relationshipToTarget == 255f ? relationshipToTarget + 1 : relationshipToTarget;
    }

    public override bool ParseXmlAttribute(XmlAttribute _attribute)
    {
        if (base.ParseXmlAttribute(_attribute))
        {
            return true;
        }

        string name = _attribute.Name;
        if (name == "faction")
        {
            faction = _attribute.Value.Trim();
            return true;
        }
        if (name == "value")
        {
            value = StringParsers.ParseFloat(_attribute.Value);
            return true;
        }
        if (name == "related")
        {
            relatedFactions = ParseArray(_attribute.Value);
            return true;
        }

        return false;
    }

    private static string[] ParseArray(string str)
    {
        var subs = str.Split(',');
        for (int i = 0; i < subs.Length; i++)
            subs[i] = subs[i].Trim();
        return subs;
    }
}
