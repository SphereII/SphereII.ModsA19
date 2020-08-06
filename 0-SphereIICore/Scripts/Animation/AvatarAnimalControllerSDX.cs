using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class AvatarAnimalControllerSDX : AvatarAnimalController
{
    // This controls the animations if we are holding a weapon.
    protected Animator rightHandAnimator;
    private Transform rightHandItemTransform;
    private Transform rightHand;
        
      public override void SwitchModelAndView(string _modelName, bool _bFPV, bool _bMale)
    {
        base.SwitchModelAndView(_modelName, _bFPV, _bMale);
        this.rightHand = this.bipedTransform.FindInChilds(this.entity.GetRightHandTransformName(), false);
        if (this.rightHandItemTransform != null)
        {
            this.rightHandItemTransform.parent = this.rightHand;
            Vector3 position = AnimationGunjointOffsetData.AnimationGunjointOffset[this.entity.inventory.holdingItem.HoldType.Value].position;
            Vector3 rotation = AnimationGunjointOffsetData.AnimationGunjointOffset[this.entity.inventory.holdingItem.HoldType.Value].rotation;
            this.rightHandItemTransform.localPosition = position;
            this.rightHandItemTransform.localEulerAngles = rotation;
            if (this.entity.inventory.holdingItem.HoldingItemHidden)
                this.rightHandItemTransform.localScale = new Vector3(0, 0, 0);
            
        }
    }
    public override void SetInRightHand(Transform _transform)
    {
        this.idleTime = 0f;
        if (_transform != null)
        {
            _transform.parent = this.rightHand;
        }
        this.rightHandItemTransform = _transform;
        this.rightHandAnimator = ((_transform != null) ? _transform.GetComponent<Animator>() : null);
        if (this.rightHandAnimator != null)
        {
            this.rightHandAnimator.logWarnings = false;
        }
        if (this.rightHandItemTransform != null)
        {
            Utils.SetLayerRecursively(this.rightHandItemTransform.gameObject, 0);
        }
    }

    public override Transform GetRightHandTransform()
    {
        return rightHandItemTransform;
    }
    public override void StartAnimationReloading()
    {
        this.idleTime = 0f;
        if (this.bipedTransform == null || !this.bipedTransform.gameObject.activeInHierarchy)
            return;
        float value = EffectManager.GetValue(PassiveEffects.ReloadSpeedMultiplier, this.entity.inventory.holdingItemItemValue, 1f, this.entity, null, default(FastTags), true, true, true, true, 1, true);
        this.SetBool("Reload", true);
        this.SetFloat("ReloadSpeed", value);

        // Work around for the Ranged2 EAI Task that needs meta to be greater than 0 to fire.
        // The same EAI task decrements the meta flag for each bullet it consumes.
        this.entity.inventory.holdingItemItemValue.Meta = 1;

    }

    public override void StartAnimationFiring()
    {
        base.StartAnimationFiring();
        this.SetTrigger("WeaponFire");
    }
}

