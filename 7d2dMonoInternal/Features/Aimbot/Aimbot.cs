using UnityEngine;
using System.Collections.Generic;
using SevenDTDMono;

namespace SevenDTDMono.Features
{
    public class Aimbot : MonoBehaviour
    {
        private static NewSettings SettingsInstance => NewSettings.Instance;
        private static Dictionary<string, bool> BoolDict => SettingsInstance.GetChildDictionary<bool>(nameof(Dictionaries.BOOL_DICTIONARY));
        private static EntityPlayerLocal Player => NewSettings.EntityLocalPlayer;

        // Expose the latest calculated path for debug drawing
        public static bool HasTarget { get; private set; }
        public static Vector3 StartPos { get; private set; }
        public static Vector3 TargetPos { get; private set; }

        // Approximated projectile speed used for movement prediction
        private readonly float projectileSpeed = 60f;

        // Cache previous positions to estimate entity velocity
        private readonly Dictionary<int, Vector3> previousPositions = new Dictionary<int, Vector3>();

        // Hold this key for the aimbot to activate
        private readonly KeyCode activationKey = KeyCode.LeftAlt;

        // Controls how quickly the player rotates towards the target.
        // Higher values result in snappier aim while lower values appear smoother.
        private readonly float rotationSpeed = 10f;

        private void Update()
        {
            if (!SettingsInstance.GetBoolValue(nameof(SettingsBools.AIMBOT)))
            {
                return;
            }

            // Only activate aimbot while the activation key is held
            if (!Input.GetKey(activationKey))
            {
                return;
            }

            if (Player == null || !NewSettings.GameManager.gameStateManager.bGameStarted)
            {
                return;
            }

            HasTarget = false;
            EntityAlive bestZombie = null;
            float minAngle = float.MaxValue;

            // Use the camera as the reference point for aiming. In first person
            // mode the camera's forward vector represents the actual crosshair
            // direction which may differ from the player transform orientation.
            Vector3 playerHead = Player.emodel.GetHeadTransform().position;
            Vector3 referencePos = playerHead;
            Vector3 referenceForward = Player.transform.forward;
            if (Camera.main)
            {
                referencePos = Camera.main.transform.position;
                referenceForward = Camera.main.transform.forward;
            }

            foreach (EntityAlive zombie in NewSettings.EntityAlive)
            {
                if (zombie == null || !zombie.IsAlive())
                {
                    continue;
                }

                // Use the current, unadjusted position when checking the FOV so
                // the on-screen circle matches the selection logic.  Including
                // prediction and bullet drop caused the aimbot to ignore valid
                // targets near the crosshair.
                Vector3 target = GetTargetPositionSimple(zombie);
                if (!IsWithinFov(target))
                {
                    continue;
                }
                if (!HasLineOfSight(referencePos, target, zombie))
                {
                    continue;
                }
                Vector3 direction = target - referencePos;
                float angle = Vector3.Angle(referenceForward, direction);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestZombie = zombie;
                }
            }

            if (bestZombie != null)
            {
                Vector3 targetPos = GetTargetPosition(bestZombie);
                Vector3 direction = targetPos - referencePos;
                Quaternion look = Quaternion.LookRotation(direction);

                // Smoothly rotate the player towards the target. Directly
                // setting the rotation each frame caused visible jitter while
                // moving. Using Slerp keeps the motion fluid.
                Player.transform.rotation = Quaternion.Slerp(
                    Player.transform.rotation,
                    look,
                    rotationSpeed * Time.deltaTime);

                // Keep the camera aligned with the player so the crosshair
                // matches the adjusted view direction.
                if (Camera.main)
                {
                    Camera.main.transform.rotation = Player.transform.rotation;
                }

                // Store positions for debug drawing
                StartPos = referencePos;
                TargetPos = targetPos;
                HasTarget = true;
            }

            // Update cached positions for next frame
            foreach (EntityAlive zombie in NewSettings.EntityAlive)
            {
                if (zombie != null)
                {
                    previousPositions[zombie.entityId] = zombie.transform.position;
                }
            }
        }

        /// <summary>
        /// Calculates the approximate height of the entity using its renderer
        /// bounds. Falls back to the head transform if no renderer is found.
        /// </summary>
        private static float GetEntityHeight(EntityAlive entity)
        {
            float height = 2f;
            Renderer renderer = entity.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                height = renderer.bounds.size.y;
            }
            else
            {
                float headOffset = entity.emodel.GetHeadTransform().position.y - entity.transform.position.y;
                if (headOffset > 0f)
                {
                    height = headOffset / 0.9f;
                }
            }
            return height;
        }

        // Estimate where the entity will be when a projectile reaches it
        private Vector3 PredictEntityBasePosition(EntityAlive entity)
        {
            Vector3 current = entity.transform.position;

            // Early out if movement prediction is disabled
            if (!SettingsInstance.GetBoolValue(nameof(SettingsBools.MOVEMENT_PREDICTION)))
            {
                return current;
            }

            if (previousPositions.TryGetValue(entity.entityId, out Vector3 last))
            {
                float dt = Time.deltaTime;
                if (dt > 0f)
                {
                    Vector3 velocity = (current - last) / dt;
                    float distance = Vector3.Distance(Player.transform.position, current);
                    float travelTime = distance / projectileSpeed;
                    current += velocity * travelTime;
                }
            }
            return current;
        }

        /// <summary>
        /// Returns the world position the aimbot should target based on the
        /// selected body part.
        /// </summary>
        private Vector3 GetTargetPosition(EntityAlive entity)
        {
            float height = GetEntityHeight(entity);
            Vector3 basePos = PredictEntityBasePosition(entity);
            Vector3 target;

            switch (SettingsInstance.SelectedAimbotTarget)
            {
                case AimbotTarget.Chest:
                    target = basePos + Vector3.up * height * 0.5f;
                    break;
                case AimbotTarget.Feet:
                    target = basePos + Vector3.up * height * 0.1f;
                    break;
                default:
                    Transform head = entity.emodel?.GetHeadTransform();
                    if (head != null)
                    {
                        Vector3 offset = head.position - entity.transform.position;
                        target = basePos + offset;
                    }
                    else
                    {
                        target = basePos + Vector3.up * height * 0.9f;
                    }
                    break;
            }

            // Compensate for bullet drop by aiming slightly above the target
            if (Camera.main)
            {
                Vector3 from = Camera.main.transform.position;
                float distance = Vector3.Distance(from, target);
                // Simple ballistic drop approximation
                float gravity = 9.81f;
                float travelTime = distance / projectileSpeed;
                float drop = 0.5f * gravity * travelTime * travelTime;
                target += Vector3.up * drop;
            }

            return target;
        }

        /// <summary>
        /// Returns the entity position for FOV checks without bullet drop or
        /// movement prediction. Using the raw position ensures the on-screen
        /// FOV circle matches the selection logic.
        /// </summary>
        private static Vector3 GetTargetPositionSimple(EntityAlive entity)
        {
            float height = GetEntityHeight(entity);
            Vector3 basePos = entity.transform.position;
            Vector3 target;

            switch (SettingsInstance.SelectedAimbotTarget)
            {
                case AimbotTarget.Chest:
                    target = basePos + Vector3.up * height * 0.5f;
                    break;
                case AimbotTarget.Feet:
                    target = basePos + Vector3.up * height * 0.1f;
                    break;
                default:
                    Transform head = entity.emodel?.GetHeadTransform();
                    target = head != null ? head.position : basePos + Vector3.up * height * 0.9f;
                    break;
            }

            return target;
        }

        // Returns true if there is a clear line of sight from the origin to the
        // target position. Uses a raycast to detect any blocking colliders and
        // verifies that the first hit belongs to the intended entity.
        private static bool HasLineOfSight(Vector3 origin, Vector3 target, EntityAlive entity)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0f)
            {
                return true;
            }

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance))
            {
                EntityAlive hitEntity = hit.collider.GetComponentInParent<EntityAlive>();
                if (hitEntity != null && hitEntity != entity)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the given world position is inside the configured aimbot FOV.
        /// Uses screen space distance so the FOV circle and targeting logic match.
        /// </summary>
        private static bool IsWithinFov(Vector3 worldPos)
        {
            if (!Camera.main)
            {
                return true;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(worldPos);
            if (screen.z <= 0f)
            {
                return false;
            }

            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 pos2D = new Vector2(screen.x, screen.y);

            float angleRatio = Mathf.Tan(SettingsInstance.AimbotFov * Mathf.Deg2Rad * 0.5f) /
                               Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float maxDist = (Screen.height / 2f) * angleRatio;

            return Vector2.Distance(pos2D, center) <= maxDist;
        }
    }
}
