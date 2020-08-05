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
   
}

