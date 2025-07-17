using UnityEngine;
using System.Collections.Generic;

namespace SevenDTDMono.Features.Magic
{
    public class MagicBullet : MonoBehaviour
    {
        private static NewSettings SettingsInstance => NewSettings.Instance;
        private static Dictionary<string, bool> BoolDict => SettingsInstance.GetChildDictionary<bool>(nameof(Dictionaries.BOOL_DICTIONARY));
        private static EntityPlayerLocal Player => NewSettings.EntityLocalPlayer;

        private readonly KeyCode activationKey = KeyCode.LeftAlt;

        private void Update()
        {
            if (!SettingsInstance.GetBoolValue(nameof(SettingsBools.MAGIC_BULLET)))
            {
                return;
            }

            if (!Input.GetKey(activationKey))
            {
                return;
            }

            if (Player == null || !NewSettings.GameManager.gameStateManager.bGameStarted)
            {
                return;
            }

            EntityAlive bestTarget = null;
            float minAngle = float.MaxValue;
            Vector3 playerHead = Player.emodel.GetHeadTransform().position;

            foreach (EntityAlive entity in NewSettings.EntityAlive)
            {
                if (entity == null || !entity.IsAlive() || entity == Player)
                {
                    continue;
                }

                Vector3 head = entity.emodel.GetHeadTransform().position;
                Vector3 direction = head - playerHead;
                float angle = Vector3.Angle(Player.transform.forward, direction);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestTarget = entity;
                }
            }

            foreach (EntityPlayer player in NewSettings.EntityPlayers)
            {
                if (player == null || !player.IsAlive() || player == Player)
                {
                    continue;
                }

                Vector3 head = player.emodel.GetHeadTransform().position;
                Vector3 direction = head - playerHead;
                float angle = Vector3.Angle(Player.transform.forward, direction);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestTarget = player;
                }
            }

            if (bestTarget != null)
            {
                // Use a generic damage type to ensure compatibility even if
                // EnumDamageTypes.Bullet is unavailable in some game versions.
                bestTarget.DamageEntity(new DamageSource(EnumDamageSource.Internal, EnumDamageTypes.Suicide), 99999, false, 1f);
            }
        }
    }
}
