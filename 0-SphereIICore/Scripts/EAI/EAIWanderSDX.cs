using GamePath;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using UnityEngine;

class EAIWanderSDX : EAIWander
{
    public Vector3 position;

    public float time;

    private readonly bool blDisplayLog = true;
    private readonly bool blShowPathFindingBlocks = false;
    public void DisplayLog(String strMessage)
    {
        if (blDisplayLog)
            Debug.Log(this.theEntity.EntityName + ": " + strMessage);
    }

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);

        // Delay the wait task to slow them down.
        this.executeWaitTime = 2f;
    }
    public override void Reset()
    {
        base.Reset();

        // Turn jumping back on, to prevent them from jumping weirdly.
        if (this.theEntity is EntityAliveSDX)
            (theEntity as EntityAliveSDX).canJump = true;

    }
    public override void Update()
    {
        this.time += 0.05f;

        //If we are close, be done with it. This is to help prevent the NPC from standing on certain workstations that its supposed to path too.
        float dist = Vector3.Distance(this.position, this.theEntity.position);
        if (dist < 2f)
        {
            DisplayLog("I am within 1f of the block: " + dist);
            BlockValue block = GameManager.Instance.World.GetBlock(new Vector3i(this.position));
            if (block.type != BlockValue.Air.type || block.Block.GetBlockName() != "PathingCube")
            {
                DisplayLog("I am close enough to this block: " + block.Block.GetBlockName());
                this.time = 90f;
                return;
            }
        }
    }

    public override bool Continue()
    {
        // Reduces the entity from continuing to walk against a wall
        if (this.theEntity.moveHelper.BlockedTime >= 0.3f)
        {
            EntityUtilities.Stop(this.theEntity.entityId);
            this.position = Vector3.zero;
            return false;
        }

        // calling stop here if we can't continue to clear the path and movement. 
        bool result = this.theEntity.bodyDamage.CurrentStun == EnumEntityStunType.None && this.theEntity.moveHelper.BlockedTime <= 0.3f && this.time <= 30f && !this.theEntity.navigator.noPathAndNotPlanningOne();
        if (!result)
        {
            EntityUtilities.Stop(this.theEntity.entityId);
            this.position = Vector3.zero;
            return false;
        }

        return result;
    }


    public override void Start()
    {
        // if no pathing blocks, just randomly pick something.
        if (this.position == Vector3.zero)
        {
            int maxDistance = 30;

            if (this.theEntity.IsAlert)
                this.position = RandomPositionGenerator.CalcAway(this.theEntity, 0, maxDistance, maxDistance, this.theEntity.LastTargetPos);
            else
                this.position = RandomPositionGenerator.Calc(this.theEntity, maxDistance, maxDistance);
        }

        this.time = 0f;

        EntityUtilities.ChangeHandholdItem(this.theEntity.entityId, EntityUtilities.Need.Reset);

        // Turn off the entity jumping, and turn on breaking blocks, to allow for better pathing.
        if (this.theEntity is EntityAliveSDX)
        {
            (theEntity as EntityAliveSDX).canJump = false;
            (theEntity as EntityAliveSDX).moveHelper.CanBreakBlocks = true;
        }

        // Path finding has to be set for Breaking Blocks so it can path through doors
        PathFinderThread.Instance.FindPath(this.theEntity, this.position, this.theEntity.GetMoveSpeed(), true, this);


        return;
    }

    public override bool CanExecute()
    {
        // if they are set to IsBusy, don't try to wander around.
        bool isBusy = false;
        this.theEntity.emodel.avatarController.TryGetBool("IsBusy", out isBusy);

        if (isBusy)
            return false;

        // if you are supposed to stay put, don't wander. 
        if (EntityUtilities.CanExecuteTask(this.theEntity.entityId, EntityUtilities.Orders.Stay))
            return false;

        // If there's a target to fight, dont wander around. That's lame, sir.
        if (EntityUtilities.GetAttackOrReventTarget(this.theEntity.entityId) != null)
            return false;

        if (this.theEntity.Buffs.HasCustomVar("PathingCode") && this.theEntity.Buffs.GetCustomVar("PathingCode") == -1)
            return false;

        // If Pathing blocks does not exist, don't bother trying to do the enhanced wander code
        if (!EntityUtilities.CheckProperty(this.theEntity.entityId, "PathingBlocks"))
            if ( !this.theEntity.Buffs.HasCustomVar("PathingCode"))
                return base.CanExecute();

        if (this.theEntity.sleepingOrWakingUp)
            return false;
        if (this.theEntity.GetTicksNoPlayerAdjacent() >= 120)
            return false;
        if (this.theEntity.bodyDamage.CurrentStun != EnumEntityStunType.None)
            return false;
        int num = (int)(200f * this.executeWaitTime);
        if (base.GetRandom(1000) >= num)
            return false;
        if (this.manager.lookTime > 0f)
            return false;

        Vector3 newPosition = EntityUtilities.GetNewPositon(this.theEntity.entityId);
        if (newPosition == Vector3.zero)
        {
            DisplayLog("I do not have any pathing blocks");
            return base.CanExecute();
        }
        DisplayLog(" I have a new position I can path to: " + newPosition);

        //  For testing, change the target to this block, so we can see where the NPC intends to go.
        if (blShowPathFindingBlocks)
        {
            DisplayLog(" I have highlighted where I am going: " + newPosition);
            String strParticleName = "#@modfolder(0-SphereIICore):Resources/PathSmoke.unity3d?P_PathSmoke_X";
            if (!ParticleEffect.IsAvailable(strParticleName))
                ParticleEffect.RegisterBundleParticleEffect(strParticleName);

            Vector3 supportBlock = GameManager.Instance.World.FindSupportingBlockPos(newPosition);
            BlockUtilitiesSDX.addParticles(strParticleName, new Vector3i(supportBlock));
        }

        if (SphereCache.LastBlock.ContainsKey(this.theEntity.entityId))
        {
            if (blShowPathFindingBlocks)
            {
                DisplayLog("I am changing the block back to the pathing block");
                Vector3 supportBlock = GameManager.Instance.World.FindSupportingBlockPos(this.position);
                BlockUtilitiesSDX.removeParticles(new Vector3i(supportBlock));
            }
            SphereCache.LastBlock[this.theEntity.entityId] = newPosition;
        }
        else
        {
            // Store the LastBlock position here, so we know what we can remove next time.
            SphereCache.LastBlock.Add(this.theEntity.entityId, newPosition);
        }
        this.position = newPosition;

        return true;


    }

}

