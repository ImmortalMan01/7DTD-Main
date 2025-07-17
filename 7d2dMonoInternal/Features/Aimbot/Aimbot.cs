using UnityEngine;
using System.Collections.Generic;

namespace SevenDTDMono.Features
{
    public class Aimbot : MonoBehaviour
    {
        private static NewSettings SettingsInstance => NewSettings.Instance;
        private static Dictionary<string, bool> BoolDict => SettingsInstance.GetChildDictionary<bool>(nameof(Dictionaries.BOOL_DICTIONARY));
        private static EntityPlayerLocal Player => NewSettings.EntityLocalPlayer;

        // Controls how quickly the player rotates towards the target.
        // Higher values result in snappier aim while lower values appear smoother.
        private readonly float rotationSpeed = 10f;

        private void Update()
        {
            if (!SettingsInstance.GetBoolValue(nameof(SettingsBools.AIMBOT)))
            {
                return;
            }

            if (Player == null || !NewSettings.GameManager.gameStateManager.bGameStarted)
            {
                return;
            }

            EntityAlive bestZombie = null;
            float minAngle = float.MaxValue;
            Vector3 playerHead = Player.emodel.GetHeadTransform().position;

            foreach (EntityAlive zombie in NewSettings.EntityAlive)
            {
                if (zombie == null || !zombie.IsAlive())
                {
                    continue;
                }

                Vector3 head = zombie.emodel.GetHeadTransform().position;
                Vector3 direction = head - playerHead;
                float angle = Vector3.Angle(Player.transform.forward, direction);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestZombie = zombie;
                }
            }

            if (bestZombie != null)
            {
                Vector3 head = bestZombie.emodel.GetHeadTransform().position;
                Vector3 direction = head - playerHead;
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
