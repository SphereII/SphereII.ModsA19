using System;
using UnityEngine;

class EAISetAsTargetIfHurtSDX : EAISetAsTargetIfHurt
{
    private readonly bool blDisplayLog = false;
    public void DisplayLog(String strMessage)
    {
        if (blDisplayLog)
            Debug.Log(GetType() + " : " + theEntity.EntityName + ": " + theEntity.entityId + ": " + strMessage);
    }

    public override bool CanExecute()
    {
        // If the attack or revenge target is a friend, then forgive them
        if (IsFriend(theEntity.GetRevengeTarget()))
            return false;

        if (IsFriend(theEntity.GetAttackTarget()))
            return false;

        return base.CanExecute();
    }

    private bool IsFriend(EntityAlive target)
    {
        if (target != null)
        {
            if (target.factionId == theEntity.factionId)
                return true;
            if (EntityUtilities.IsAnAlly(theEntity.entityId, target.entityId))
                return true;
        }
        return false;
    }

}

