using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class AvatarAnimalControllerSDX : AvatarAnimalController
{

    // If set to true, logging will be very verbose for troubleshooting
    private readonly bool blDisplayLog = false;

    // This controls the animations if we are holding a weapon.
    protected Animator rightHandAnimator;
    private string RightHand = "RightHand";
    private Transform rightHandItemTransform;
    private Transform rightHand;


    
    AvatarAnimalControllerSDX()
    {
        entity = base.transform.gameObject.GetComponent<EntityAlive>();
        EntityClass entityClass = EntityClass.list[entity.entityClass];
        if (entityClass.Properties.Values.ContainsKey("RightHandJointName"))
        {
            RightHand = entityClass.Properties.Values["RightHandJointName"];
        }
    }
    public override void SwitchModelAndView(string _modelName, bool _bFPV, bool _bMale)
    {
        base.SwitchModelAndView(_modelName, _bFPV, _bMale);

        // Check if this entity has a weapon or not
        if (rightHandItemTransform != null)
        {
            Log("Setting Right hand position");
            rightHandItemTransform.parent = rightHandItemTransform;
            Vector3 position = AnimationGunjointOffsetData.AnimationGunjointOffset[entity.inventory.holdingItem.HoldType.Value].position;
            Vector3 rotation = AnimationGunjointOffsetData.AnimationGunjointOffset[entity.inventory.holdingItem.HoldType.Value].rotation;
            rightHandItemTransform.localPosition = position;
            rightHandItemTransform.localEulerAngles = rotation;
            SetInRightHand(rightHandItemTransform);
        }
    }
    public override void SetInRightHand(Transform _transform)
    {
        if (!(rightHandItemTransform == null) && !(_transform == null))
        {
            Log("Setting Right Hand: " + rightHandItemTransform.name.ToString());
            idleTime = 0f;
            Log("Setting Right Hand Transform");
            rightHandItemTransform = _transform;
            if (rightHandItemTransform == null)
            {
                Log("Right Hand Animator is Null");
            }
            else
            {
                Log("Right Hand Animator is NOT NULL ");
                rightHandAnimator = rightHandItemTransform.GetComponent<Animator>();
                if (rightHandItemTransform != null)
                {
                    Utils.SetLayerRecursively(rightHandItemTransform.gameObject, 0);
                }
                Log("Done with SetInRightHand");
            }
        }
    }

    public override Transform GetRightHandTransform()
    {
        return rightHandItemTransform;
    }
    private void Log(string strLog)
    {
        if (blDisplayLog)
        {
            if (modelTransform == null)
            {
                Debug.Log(string.Format("Unknown Entity: {0}", strLog));
            }
            else
            {
                Debug.Log(string.Format("{0}: {1}", modelTransform.name, strLog));
            }
        }
    }
}

