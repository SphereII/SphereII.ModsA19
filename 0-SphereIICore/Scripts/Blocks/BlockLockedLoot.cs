using System;
using System.Globalization;
using Audio;
using Lockpicking;
using UnityEngine;


class BlockLockedLoot : BlockSecureLoot
{

    public Keyhole lockpick;
    public static GameObject lockPick = null;
    public Keyhole GetKeyHole => lockpick;

    public BlockLockedLoot()
    {
        HasTileEntity = true;
    }

    private readonly BlockActivationCommand[] cmds = new BlockActivationCommand[]
   {
        new BlockActivationCommand("light", "electric_switch", true),
        new BlockActivationCommand("Open", "hand", true),
        new BlockActivationCommand("take", "hand", false)
   };

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        #region GetActivationText

        PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
        string keybindString = playerInput.Activate.GetBindingXuiMarkupString(XUiUtils.EmptyBindingStyle.EmptyString, XUiUtils.DisplayStyle.Plain) + playerInput.PermanentActions.Activate.GetBindingXuiMarkupString(XUiUtils.EmptyBindingStyle.EmptyString, XUiUtils.DisplayStyle.Plain);
        //string keybindString = UIUtils.GetKeybindString(playerInput.Activate, playerInput.PermanentActions.Activate);
        Block block = Block.list[_blockValue.type];
        string blockName = block.GetBlockName();

        string strReturn = string.Format(Localization.Get("pickupPrompt"), Localization.Get(blockName));

        BlockEntityData _ebcd = _world.GetChunkFromWorldPos(_blockPos).GetBlockEntity(_blockPos);
        if (_ebcd != null && _ebcd.transform)
        {
            strReturn = "Testing";
        }
        return strReturn;
        #endregion
    }


    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        cmds[0].enabled = true;
        cmds[1].enabled = true;
        cmds[2].enabled = true;
        return cmds;
    }

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        if (_blockValue.ischild)
        {
            return;
        }
        shape.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        if (isMultiBlock)
        {
            multiBlockPos.AddChilds(world, _chunk, _chunk.ClrIdx, _blockPos, _blockValue);
        }

        if (!(world.GetTileEntity(_chunk.ClrIdx, _blockPos) is TileEntitySecureLootContainer))
        {
            Debug.Log("No Tile Entity. Creating...");
            TileEntityLootContainer tileEntitySecureLootContainer = new TileEntityLootContainer(_chunk);
            tileEntitySecureLootContainer.localChunkPos = World.toBlock(_blockPos);
            tileEntitySecureLootContainer.lootListIndex = (int)((ushort)this.lootList);
            tileEntitySecureLootContainer.SetContainerSize(LootContainer.lootList[this.lootList].size, true);
            _chunk.AddTileEntity(tileEntitySecureLootContainer);
        }

        Debug.Log("Adding Block Stub");
        _chunk.AddEntityBlockStub(new BlockEntityData(_blockValue, _blockPos)
        {

            bNeedsTemperature = true

        });

    }
    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        Debug.Log("On Block Activated");
        if (_blockValue.ischild)
        {
            Vector3i parentPos = Block.list[_blockValue.type].multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return this.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, parentPos, block, _player);
        }
        //TileEntitySecureLootContainer tileEntitySecureLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntitySecureLootContainer;
        //if (tileEntitySecureLootContainer == null)
        //{
        //    Debug.Log("not a tile entity");
        //    return false;
        //}
        //switch (_indexInBlockActivationCommands)
        //{
        //    case 0:
        //        if (!tileEntitySecureLootContainer.IsLocked() || tileEntitySecureLootContainer.IsUserAllowed(GamePrefs.GetString(EnumGamePrefs.PlayerId)))
        //        {
        //            return this.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        //        }
        //        Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/locked");
        //        return false;
        //    case 1:
        //        tileEntitySecureLootContainer.SetLocked(true);
        //        Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/locking");
        //        GameManager.ShowTooltip(_player as EntityPlayerLocal, "containerLocked");
        //        return true;
        //    case 2:
        //        tileEntitySecureLootContainer.SetLocked(false);
        //        Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
        //        GameManager.ShowTooltip(_player as EntityPlayerLocal, "containerUnlocked");
        //        return true;
        //    case 3:
        //        {
        //            LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(_player as EntityPlayerLocal);
        //            if (uiforPlayer != null)
        //            {
        //                XUiC_KeypadWindow.Open(uiforPlayer, tileEntitySecureLootContainer);
        //            }
        //            return true;
        //        }
        //    case 4:
        //        {

        // If there's no transform, no sense on keeping going for this class.
        BlockEntityData _ebcd = _world.GetChunkFromWorldPos(_blockPos).GetBlockEntity(_blockPos);
        if (_ebcd == null || _ebcd.transform == null)
        {
            Debug.Log("No ebdc");
            return false;
        }
        //lockpick = _ebcd.transform.GetComponent<Keyhole>();
        //if (lockpick == null)
        //{
        //    Debug.Log("Adding Keyhole Script");

        //    lockpick = _ebcd.transform.gameObject.AddComponent<Keyhole>();
        //}

        LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(_player as EntityPlayerLocal);
        if (lockPick == null)
        {
            GameObject temp = DataLoader.LoadAsset<GameObject>("#@modfolder(0-SphereIICore):Resources/LockPick.unity3d?Lockset");
            if (temp != null)
            {
                Debug.Log("Loaded Asset");
                Camera mainCamera = GameObject.FindObjectOfType<Camera>();
                
                Vector3 v3Pos = new Vector3(_blockPos.x, _blockPos.y, _blockPos.z);
                lockPick = UnityEngine.Object.Instantiate<GameObject>(temp, v3Pos, Quaternion.identity);
                if (lockPick != null)
                {
                   // lockPick.transform.SetParent(uiforPlayer.uiCamera.transform);
                    Debug.Log("Lock Pick");
                    lockPick.SetActive(true);
                    foreach( GameObject com in lockPick.GetComponentsInChildren<GameObject>())
                    {
                        Debug.Log("\tComponent: " + com.ToString());
                    }
                    Debug.Log("Position: " + lockPick.transform.position);
                    // Utils.SetLayerRecursively(lockPick, 11);
                }

            }
        }

        //uiforPlayer.windowManager.Open("LockPicking", true, false, true);

        //GameObject temp = DataLoader.LoadAsset<GameObject>("#@modfolder(0-SphereIICore):Resources/LockPick.unity3d?Button");
        //if (temp != null)
        //{


        //    Debug.Log("Loaded Asset");
        //    Camera mainCamera = GameObject.FindObjectOfType<Camera>();

        //    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(temp, mainCamera.transform);
        //    if (gameObject != null)
        //    {
        //        Vector3 local_offset = new Vector3(0f, 0f, 0f);
        //        Vector3 local_rotation = new Vector3(0f, 0f, 0f);
        //        Debug.Log("Lock Pick");
        //        gameObject.SetActive(true);
        //        Utils.SetLayerRecursively(gameObject, gameObject.transform.parent.gameObject.layer);
        //    }

        //}


        //    LocalPlayerUI playerUI = (_player as EntityPlayerLocal).PlayerUI;
        //    ItemValue item = ItemClass.GetItem(this.lockPickItem, false);
        //    if (playerUI.xui.PlayerInventory.GetItemCount(item) == 0)
        //    {
        //        playerUI.xui.CollectedItemList.AddItemStack(new ItemStack(item, 0), true);
        //        GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttLockpickMissing"));
        //        return true;
        //    }
        //    playerUI.windowManager.Open("timer", true, false, true);
        //    XUiC_Timer childByType = playerUI.xui.GetChildByType<XUiC_Timer>();
        //    TimerEventData timerEventData = new TimerEventData();
        //    timerEventData.CloseEvent += this.EventData_CloseEvent;
        //    float alternateTime = -1f;
        //    if (_player.rand.RandomRange(1f) < EffectManager.GetValue(PassiveEffects.LockPickBreakChance, _player.inventory.holdingItemItemValue, this.lockPickBreakChance, _player, null, default(FastTags), true, true, true, true, 1, true))
        //    {
        //        float value = EffectManager.GetValue(PassiveEffects.LockPickTime, _player.inventory.holdingItemItemValue, this.lockPickTime, _player, null, default(FastTags), true, true, true, true, 1, true);
        //        float num = value - ((tileEntitySecureLootContainer.PickTimeLeft == -1f) ? (value - 1f) : (tileEntitySecureLootContainer.PickTimeLeft + 1f));
        //        alternateTime = _player.rand.RandomRange(num + 1f, value - 1f);
        //    }
        //    timerEventData.Data = new object[]
        //    {
        //_cIdx,
        //_blockValue,
        //_blockPos,
        //_player,
        //item
        //    };
        //    timerEventData.Event += this.EventData_Event;
        //    timerEventData.alternateTime = alternateTime;
        //    timerEventData.AlternateEvent += this.EventData_CloseEvent;
        //    childByType.SetTimer(EffectManager.GetValue(PassiveEffects.LockPickTime, _player.inventory.holdingItemItemValue, this.lockPickTime, _player, null, default(FastTags), true, true, true, true, 1, true), timerEventData, tileEntitySecureLootContainer.PickTimeLeft, "");
        //    Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
        //            return true;
        //        }
        //    default:
        //        return false;
        //}

        return true;
    }
}

