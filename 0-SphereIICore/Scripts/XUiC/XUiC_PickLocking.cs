using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class XUiC_PickLocking : XUiController
{
    public static string ID = "";
    public static GameObject lockPick = null;
    private XUiC_TextInput txtPassword;

    public override void Init()
    {
        XUiC_PickLocking.ID = windowGroup.ID;
        base.Init();
        txtPassword = (XUiC_TextInput)base.GetChildById("txtPassword");

        ((XUiC_SimpleButton)base.GetChildById("btnCancel")).OnPressed += BtnCancel_OnPressed;
        ((XUiC_SimpleButton)base.GetChildById("btnOk")).OnPressed += BtnOk_OnPressed;
    }

    private void BtnOk_OnPressed(XUiController _sender, OnPressEventArgs _onPressEventArgs)
    {

        GameManager.ShowTooltip(base.xui.playerUI.entityPlayer, "Closing Lock");
        base.xui.playerUI.windowManager.Close(base.WindowGroup.ID);
    }

    private void BtnCancel_OnPressed(XUiController _sender, OnPressEventArgs _e)
    {
        base.xui.playerUI.windowManager.Close(base.WindowGroup.ID);
    }

    public override void OnOpen()
    {
        EntityPlayer player = base.xui.playerUI.entityPlayer;
        base.OnOpen();
        base.xui.playerUI.entityPlayer.PlayOneShot("open_sign", false);
        if ( lockPick == null )
        {
            GameObject temp = DataLoader.LoadAsset<GameObject>("#@modfolder(0-SphereIICore):Resources/LockPick.unity3d?Button");
            if (temp != null)
            {
                Debug.Log("Loaded Asset");
                Camera mainCamera = GameObject.FindObjectOfType<Camera>();
                Vector3 v3Pos = new Vector3(-0.15f, .5f, 10);
                v3Pos = Camera.main.ViewportToWorldPoint(v3Pos);
                lockPick = UnityEngine.Object.Instantiate<GameObject>(temp, v3Pos, Quaternion.identity);
                if (lockPick != null)
                {
                    lockPick.transform.SetParent(base.xui.playerUI.uiCamera.transform);
                    Vector3 local_offset = new Vector3(0f, 0f, 0f);
                    Vector3 local_rotation = new Vector3(0f, 0f, 0f);
                    Debug.Log("Lock Pick");
                    lockPick.SetActive(true);
                   // Utils.SetLayerRecursively(lockPick, 11);
                }

            }
        }

    }

    public override void OnClose()
    {
        base.OnClose();
        base.xui.playerUI.entityPlayer.PlayOneShot("close_sign", false);
    }




}
