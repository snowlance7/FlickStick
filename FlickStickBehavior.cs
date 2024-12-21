using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static FlickStick.Plugin;
using static UnityEngine.LightAnchor;

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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        PlayerControllerB? previousPlayerHeldBy;
        const int mask = 1084754248;
        List<RaycastHit> hits = [];

        // Configs
        float pokeForce = 5f;
        float flickForce = 10f;
        float flipoffRange = 10f;

        public override void Update()
        {
            base.Update();
            if (playerHeldBy != null)
            {
                previousPlayerHeldBy = playerHeldBy;
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            playerHeldBy.equippedUsableItemQE = true;
        }

        public override void DiscardItem()
        {
            playerHeldBy.activatingItem = false;
            playerHeldBy.equippedUsableItemQE = true;
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            playerHeldBy.equippedUsableItemQE = true;
            base.PocketItem();
        }

        public override void DestroyObjectInHand(PlayerControllerB playerHolding)
        {
            playerHeldBy.equippedUsableItemQE = true;
            base.DestroyObjectInHand(playerHolding);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (buttonDown) // Flick
            {
                playerHeldBy.activatingItem = true;
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

        public List<RaycastHit> GetRaycastHits(float distance = 0.75f)
        {
            if (previousPlayerHeldBy == null) { return new List<RaycastHit>(); }
            return Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position, 0.3f, previousPlayerHeldBy.gameplayCamera.transform.forward, distance, mask, QueryTriggerInteraction.Collide).ToList();
        }

        void AddForce(EnemyAI enemy, float force, float duration = 1f)
        {
            Vector3 forwardDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.forward).normalized * 2;
            Vector3 upDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.up).normalized;
            Vector3 direction = (forwardDirection + upDirection).normalized;

            enemy.agent.enabled = false;
            Rigidbody rb = enemy.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;

            rb.velocity = Vector3.zero;
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
            StartCoroutine(RemoveRigidbodyAfterDelay(enemy, duration));
        }

        void AddForce(PlayerControllerB player, float force, float duration = 1f)
        {
            Vector3 forwardDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.forward).normalized * 2;
            Vector3 upDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.up).normalized;
            Vector3 direction = (forwardDirection + upDirection).normalized;

            Rigidbody rb = player.playerRigidbody;
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            player.externalForceAutoFade += direction * force;
            StartCoroutine(ResetKinematicsAfterDelay(player, duration));
        }

        IEnumerator ResetKinematicsAfterDelay(PlayerControllerB player, float delay)
        {
            yield return new WaitForSeconds(delay);
            player.playerRigidbody.isKinematic = true;
        }

        IEnumerator RemoveRigidbodyAfterDelay(EnemyAI enemy, float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(enemy.gameObject.GetComponent<Rigidbody>());
            enemy.agent.enabled = true;
        }

        // Animation stuff
        public void Poke()
        {
            hits = GetRaycastHits();
            foreach (var hit in hits)
            {
                if (hit.transform.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
                {
                    AddForce(player, pokeForce);
                    return;
                }

                if (hit.transform.TryGetComponent<EnemyAI>(out EnemyAI enemy))
                {
                    AddForce(enemy, pokeForce);
                    enemy.SetEnemyStunned(true, 1f, previousPlayerHeldBy);
                }
            }
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