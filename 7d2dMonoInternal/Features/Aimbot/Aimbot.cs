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

        // Approximated projectile speed used for movement prediction.
        // The value is updated each frame based on the currently held gun
        // to improve accuracy of movement and bullet drop compensation.
        private float projectileSpeed = 60f;

        // Potential field names for projectile velocity on the gun class
        private static readonly string[] projectileSpeedFieldNames =
        {
            "projectileVelocity",
            "bulletVelocity",
            "muzzleVelocity",
            "velocity"
        };

        // Cache previous positions to estimate entity velocity
        private readonly Dictionary<int, Vector3> previousPositions = new Dictionary<int, Vector3>();

        // Hold this key for the aimbot to activate
        private readonly KeyCode activationKey = KeyCode.LeftAlt;

        // Controls how quickly the player rotates towards the target.
        // Higher values result in snappier aim while lower values appear smoother.
        private readonly float rotationSpeed = 10f;

        // Currently locked target while the activation key is held
        private EntityAlive lockedZombie = null;

        private void Update()
        {
            // Refresh projectile speed from the currently held weapon so that
            // movement prediction and bullet drop compensation remain accurate
            UpdateProjectileSpeed();
            if (!SettingsInstance.GetBoolValue(nameof(SettingsBools.AIMBOT)))
            {
                return;
            }

            bool holding = Input.GetKey(activationKey);
            // Only activate aimbot while the activation key is held
            if (!holding)
            {
                lockedZombie = null;
                return;
            }

            if (Player == null || !NewSettings.GameManager.gameStateManager.bGameStarted)
            {
                lockedZombie = null;
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

            if (lockedZombie != null && lockedZombie.IsAlive())
            {
                // Verify the locked target still meets FOV and line of sight requirements
                Vector3 lockedTarget = GetTargetPositionSimple(lockedZombie);
                if (IsWithinFov(lockedTarget) &&
                    HasLineOfSight(referencePos, lockedTarget, lockedZombie))
                {
                    bestZombie = lockedZombie;
                    minAngle = Vector3.Angle(referenceForward, lockedTarget - referencePos);
                }
                else
                {
                    lockedZombie = null;
                }
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

            lockedZombie = bestZombie;

            if (bestZombie != null)
            {
                Vector3 targetPos = GetTargetPosition(bestZombie);
                Vector3 direction = targetPos - referencePos;
                Quaternion look = Quaternion.LookRotation(direction);

                if (SettingsInstance.SelectedAimbotMode == AimbotMode.Normal)
                {
                    // Smoothly rotate the player and camera to face the target
                    Player.transform.rotation = Quaternion.Slerp(
                        Player.transform.rotation,
                        look,
                        rotationSpeed * Time.deltaTime);

                    if (Camera.main)
                    {
                        Camera.main.transform.rotation = Quaternion.Slerp(
                            Camera.main.transform.rotation,
                            look,
                            rotationSpeed * Time.deltaTime);
                    }
                }
                else if (SettingsInstance.SelectedAimbotMode == AimbotMode.Silent && Input.GetMouseButtonDown(0))
                {
                    // Directly damage the target when firing without adjusting the view
                    bestZombie.DamageEntity(new DamageSource(EnumDamageSource.Internal, EnumDamageTypes.Suicide), 99999, false, 1f);
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
            // Only apply when movement prediction is enabled so disabling the
            // feature results in fully "raw" aiming.
            if (Camera.main &&
                SettingsInstance.GetBoolValue(nameof(SettingsBools.MOVEMENT_PREDICTION)))
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
        /// Uses a pure angle check so off-screen calculations match the drawn circle.
        /// </summary>
        private static bool IsWithinFov(Vector3 worldPos)
        {
            Vector3 origin;
            Vector3 forward;

            if (Camera.main)
            {
                origin = Camera.main.transform.position;
                forward = Camera.main.transform.forward;
            }
            else if (Player != null)
            {
                origin = Player.emodel.GetHeadTransform().position;
                forward = Player.transform.forward;
            }
            else
            {
                return true;
            }

            Vector3 direction = worldPos - origin;
            if (direction.sqrMagnitude <= 0f)
            {
                return true;
            }

            float angle = Vector3.Angle(forward, direction);
            return angle <= SettingsInstance.AimbotFov * 0.5f;
        }

        // Attempts to read the projectile velocity from the currently held gun.
        // If the value cannot be retrieved, the previously cached speed is kept.
        private void UpdateProjectileSpeed()
        {
            try
            {
                var inv = Player?.inventory;
                if (inv == null) return;

                var gun = inv.GetHoldingGun();
                if (gun == null) return;

                var type = gun.GetType();
                foreach (var name in projectileSpeedFieldNames)
                {
                    var field = type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(float))
                    {
                        float value = (float)field.GetValue(gun);
                        if (value > 0f)
                        {
                            projectileSpeed = value;
                        }
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
