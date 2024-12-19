using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static FlickStick.Plugin;

namespace FlickStick
{
    // Player override animations:
    // "Grab" bool
    // "UseHeldItem1" trigger

    internal class FlickStickBehavior : PhysicsProp
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AudioSource ItemAudio;
        public AudioClip BoingSFX;
        public AudioClip SlideSFX;
        public Animator animator;
        public Transform PointerTip;
        public AnimationClip PlayerGrabAnim;
        public AnimationClip PlayerChargeAnim;

        public static RuntimeAnimatorController originalController;
        public static AnimatorOverrideController overrideController;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        //const string _grabAnimString = "Grab";
        //const string _useAnimString = "UseHeldItem1";
        const string _grabAnimName = "HoldOneHandedItem"; // HoldOneHandedItem
        //const string _useAnimName = "KnifeStab";

        public override void Start()
        {
            base.Start();
            
            if (overrideController != null) { return; }

            Animator playerAnimator = localPlayer.playerBodyAnimator;
            overrideController = new AnimatorOverrideController(playerAnimator.runtimeAnimatorController);
            overrideController[_grabAnimName] = PlayerGrabAnim;
            //overrideController[_useAnimName] = PlayerChargeAnim;
        }

        /*public override void GrabItem()
        {
            base.GrabItem();
            UseOverrideController(true);
            playerHeldBy.playerBodyAnimator.SetBool(_grabAnimString, true);
        }*/

        public override void EquipItem()
        {
            base.EquipItem();
            UseOverrideController(true);
            //playerHeldBy.playerBodyAnimator.SetBool(_grabAnimString, true);
            //playerHeldBy.playerBodyAnimator.Play(_grabAnimName);
        }

        public override void DiscardItem()
        {
            playerHeldBy.activatingItem = false;
            UseOverrideController(false);
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            UseOverrideController(false);
            base.PocketItem();
        }

        public override void DestroyObjectInHand(PlayerControllerB playerHolding)
        {
            UseOverrideController(false);
            base.DestroyObjectInHand(playerHolding);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (buttonDown) // Flick
            {
                //playerHeldBy.activatingItem = true;
                animator.SetTrigger("flick");
                playerHeldBy.playerBodyAnimator.Play(_grabAnimName); // for testing
                //playerHeldBy.playerBodyAnimator.SetTrigger(_useAnimString);
            }
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);

            if (right) // Poke
            {

            }
            else // Flip off
            {

            }
        }

        void UseOverrideController(bool enable)
        {
            playerHeldBy.equippedUsableItemQE = enable;

            if (enable)
            {
                //if (playerHeldBy.playerBodyAnimator.runtimeAnimatorController == overrideController) { logger.LogWarning("Already using override controller"); return; }

                originalController = playerHeldBy.playerBodyAnimator.runtimeAnimatorController;

                playerHeldBy.playerBodyAnimator.runtimeAnimatorController = overrideController;
            }
            else
            {
                //if (playerHeldBy.playerBodyAnimator.runtimeAnimatorController != overrideController) { logger.LogWarning("Already not using override controller"); return; }
                if (originalController == null) { logger.LogError("Original controller is null"); return; }

                playerHeldBy.playerBodyAnimator.runtimeAnimatorController = originalController;
            }
        }

        // Animation stuff
        public void Poke()
        {

        }

        public void Flip()
        {

        }

        public void Flick()
        {

        }

        public void ActivatingItemFalse()
        {
            playerHeldBy.activatingItem = false;
        }
    }
}