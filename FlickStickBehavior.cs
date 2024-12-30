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
        public ScanNodeProperties ScanNode;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        PlayerControllerB? previousPlayerHeldBy;
        const int mask = 524288;
        List<RaycastHit> hits = [];

        // Constants
        const float forceUpMultiplier = 1f;
        const float forceForwardMultiplier = 2f;
        const float forceDuration = 2f;

        // Configs
        float pokeStunTime = 1f;
        float pokeForce = 15f;
        float flickForce = 30f;

        float flipoffRange = 30f;
        float pokeRange = 5f;
        float flickRange = 5f;

        public override void Start()
        {
            base.Start();
            if (UnityEngine.Random.Range(0, 100) == 1)
            {
                ScanNode.headerText = "Flicky Flicky Willy Donker";
            }

            pokeStunTime = configPokeStunTime.Value;
            pokeForce = configPokeForce.Value;
            flickForce = configFlickForce.Value;
            flipoffRange = configFlipOffRange.Value;
            pokeRange = configPokeRange.Value;
            flickRange = configFlickRange.Value;
        }

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

            if (playerHeldBy.activatingItem) { return; }

            if (buttonDown) // Flick
            {
                playerHeldBy.activatingItem = true;
                animator.SetTrigger("flick");
            }
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (playerHeldBy.activatingItem) { return; }

            if (right) // Poke
            {
                playerHeldBy.activatingItem = true;
                animator.SetTrigger("poke");
            }
            else // Flip off
            {
                playerHeldBy.activatingItem = true;
                animator.SetTrigger("flip");
            }
        }

        public List<RaycastHit> GetRaycastHits(float distance = 1f, float radius = 1f)
        {
            if (previousPlayerHeldBy == null) { return new List<RaycastHit>(); }
            //VisualizeSphereCast(PointerTip.position, previousPlayerHeldBy.playerEye.transform.forward, radius, distance, 5f);
            return Physics.SphereCastAll(PointerTip.position, radius, previousPlayerHeldBy.playerEye.transform.forward, distance, mask, QueryTriggerInteraction.Collide).ToList();
        }

        void AddForce(EnemyAI enemy, float force)
        {
            if (previousPlayerHeldBy == null) { logger.LogError("previousPlayerHeldBy is null"); return; }
            Vector3 forwardDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.forward).normalized * forceForwardMultiplier;
            Vector3 upDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.up).normalized * forceUpMultiplier;
            Vector3 direction = (forwardDirection + upDirection).normalized;

            bool usingEnemyRb = true;
            Rigidbody rb;
            
            if (!enemy.gameObject.TryGetComponent<Rigidbody>(out rb))
            {
                usingEnemyRb = false;
                rb = enemy.gameObject.AddComponent<Rigidbody>();
            }

            enemy.agent.enabled = false;
            rb.isKinematic = false;

            rb.velocity = Vector3.zero;
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
            StartCoroutine(ResetKinematicsAfterDelay(enemy, usingEnemyRb));
        }

        void AddForce(PlayerControllerB player, float force, float duration = 0.1f)
        {
            if (previousPlayerHeldBy == null) { logger.LogError("previousPlayerHeldBy is null"); return; }
            Vector3 forwardDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.forward).normalized * forceForwardMultiplier;
            Vector3 upDirection = previousPlayerHeldBy.transform.TransformDirection(Vector3.up).normalized * forceUpMultiplier;
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

        IEnumerator ResetKinematicsAfterDelay(EnemyAI enemy, bool usingEnemyRb)
        {
            yield return new WaitForSeconds(forceDuration);

            if (usingEnemyRb)
            {
                enemy.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            }
            else
            {
                Destroy(enemy.gameObject.GetComponent<Rigidbody>());
            }

            enemy.agent.enabled = true;
        }

        // Animation stuff
        public void Poke()
        {
            logger.LogDebug("In Poke()");
            hits = GetRaycastHits(pokeRange);

            logger.LogDebug("Poked:");
            foreach (var hit in hits)
            {
                if (hit.transform.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
                {
                    if (player == previousPlayerHeldBy) { continue; }
                    logger.LogDebug(player.playerUsername);
                    AddForce(player, pokeForce);
                    return;
                }

                if (hit.transform.TryGetComponent<EnemyAICollisionDetect>(out EnemyAICollisionDetect enemyCollision))
                {
                    EnemyAI? enemy = enemyCollision.mainScript;
                    if (enemy == null) { continue; }
                    logger.LogDebug(enemy.enemyType.enemyName);
                    enemy.SetEnemyStunned(true, 1f, previousPlayerHeldBy);
                }
            }

            playerHeldBy.activatingItem = false;
        }

        public void Flip()
        {
            logger.LogDebug("In Flip()");
            hits = GetRaycastHits(flipoffRange, 30f);
            RoundManager.Instance.PlayAudibleNoise(transform.position);
            ItemAudio.PlayOneShot(SlideSFX);

            logger.LogDebug("Flipped off:");
            foreach (var hit in hits)
            {
                logger.LogDebug(hit.transform.gameObject.name);
                if (hit.transform.gameObject.TryGetComponent<EnemyAICollisionDetect>(out EnemyAICollisionDetect enemyCollision))
                {
                    EnemyAI? enemy = enemyCollision.mainScript;
                    if (enemy == null) { continue; }
                    logger.LogDebug(enemy.enemyType.enemyName);
                    enemy.targetPlayer = previousPlayerHeldBy;
                    enemy.HitEnemy(0, previousPlayerHeldBy);
                }
            }

            playerHeldBy.activatingItem = false;
        }

        public void Flick()
        {
            logger.LogDebug("In Flick()");
            hits = GetRaycastHits(flickRange);
            RoundManager.Instance.PlayAudibleNoise(transform.position);
            ItemAudio.PlayOneShot(BoingSFX);

            logger.LogDebug("Flicked:");
            foreach (var hit in hits)
            {
                if (hit.transform.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
                {
                    if (player == previousPlayerHeldBy) { continue; }
                    logger.LogDebug(player.playerUsername);
                    AddForce(player, flickForce);
                    return;
                }

                if (hit.transform.TryGetComponent<EnemyAICollisionDetect>(out EnemyAICollisionDetect enemyCollision))
                {
                    EnemyAI? enemy = enemyCollision.mainScript;
                    if (enemy == null) { continue; }
                    logger.LogDebug(enemy.enemyType.enemyName);
                    AddForce(enemy, flickForce);
                }
            }

            playerHeldBy.activatingItem = false;
        }

        public void ActivatingItemFalse()
        {
            playerHeldBy.activatingItem = false;
            logger.LogDebug("Activating item false called");
        }

        void VisualizeSphereCast(Vector3 origin, Vector3 direction, float radius, float maxDistance, float duration)
        {
            GameObject line = new GameObject("SphereCastLine");
            line.layer = LayerMask.NameToLayer("Ignore Raycast"); // Assign to "Ignore Raycast" layer

            LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, origin);
            Vector3 endPosition = origin + direction.normalized * maxDistance;
            lineRenderer.SetPosition(1, endPosition);
            lineRenderer.startWidth = radius * 0.1f;
            lineRenderer.endWidth = radius * 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;

            // Destroy the line after the duration
            Destroy(line, duration);

        }
    }
}