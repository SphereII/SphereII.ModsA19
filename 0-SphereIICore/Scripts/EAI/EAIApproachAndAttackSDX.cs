using System;
using UnityEngine;

//<property name="AITask-5" value="ApproachAndAttackSDX, Mods" param1="" param2=""  /> <!-- param1 not used -->
// Disables the Eating animation
class EAIApproachAndAttackSDX : EAIApproachAndAttackTarget
{
    public readonly bool isTargetToEat = false;

    private readonly bool blDisplayLog = false;

    public void DisplayLog(String strMessage)
    {
        if (blDisplayLog)
            Debug.Log(GetType() + " : " + theEntity.EntityName + ": " + theEntity.entityId + ": " + strMessage);
    }


    public override void Start()
    {
        base.Start();
        EntityUtilities.ChangeHandholdItem(theEntity.entityId, EntityUtilities.Need.Melee);

    }
    public override bool CanExecute()
    {
        bool result = base.CanExecute();

        if (result && entityTarget != null)
        {
            if (EntityUtilities.CanExecuteTask(theEntity.entityId, EntityUtilities.Orders.Stay))
                return false;

            theEntity.SetLookPosition(entityTarget.getHeadPosition());
            theEntity.RotateTo(entityTarget, 30f, 30f);

            DisplayLog(" Has Task: " + EntityUtilities.HasTask(theEntity.entityId, "Ranged"));

            // Don't execute the approach and attack if there's a ranged ai task, and they are still 4 blocks away
            if (EntityUtilities.HasTask(theEntity.entityId, "Ranged"))
            {
                if (result)
                    result = !EntityUtilities.CheckAIRange(theEntity.entityId, entityTarget.entityId);

            }
        }
        return result;
    }

    public override bool Continue()
    {
        bool result = base.Continue();

        // Non zombies should continue to attack
        if (entityTarget.IsDead())
        {
            theEntity.IsEating = false;
            theEntity.SetAttackTarget(null, 0);
            theEntity.SetRevengeTarget(null);
            return false;
        }


        if (result)
        {


            // Don't execute the approach and attack if there's a ranged ai task, and they are still 4 blocks away
            if (EntityUtilities.HasTask(theEntity.entityId, "Ranged"))
                result = !EntityUtilities.CheckAIRange(theEntity.entityId, entityTarget.entityId);
        }

        if (!result)
            return result;

        EntityUtilities.ChangeHandholdItem(theEntity.entityId, EntityUtilities.Need.Melee);


        return result;
    }


}

