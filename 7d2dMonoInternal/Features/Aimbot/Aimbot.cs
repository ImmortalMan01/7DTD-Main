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

            float maxAngle = SettingsInstance.AimbotFov * 0.5f;

            foreach (EntityAlive zombie in NewSettings.EntityAlive)
            {
                if (zombie == null || !zombie.IsAlive())
                {
                    continue;
                }

                Vector3 head = zombie.emodel.GetHeadTransform().position;
                Vector3 direction = head - referencePos;
                float angle = Vector3.Angle(referenceForward, direction);
                if (angle > maxAngle)
                {
                    continue;
                }
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestZombie = zombie;
                }
            }

            if (bestZombie != null)
            {
                Vector3 targetPos = bestZombie.emodel.GetHeadTransform().position;

                // Calculate the approximate height of the entity to create
                // proportional offsets for chest and leg aiming. Using fixed
                // offsets caused the aim to always end up on the head for
                // entities with different sizes.
                float entityHeight = targetPos.y - bestZombie.transform.position.y;

                switch (SettingsInstance.SelectedAimbotTarget)
                {
                    case AimbotTarget.Chest:
                        // Roughly aim at the middle of the body
                        targetPos -= Vector3.up * entityHeight * 0.4f;
                        break;
                    case AimbotTarget.Leg:
                        // Aim lower towards the legs
                        targetPos -= Vector3.up * entityHeight * 0.75f;
                        break;
                }

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
            }
        }
    }
}
