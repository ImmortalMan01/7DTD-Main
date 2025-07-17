using System.Linq;
using UnityEngine;

namespace SevenDTDMono.Features.Aimbot
{
    /// <summary>
    /// Simple aimbot that targets the closest living zombie when holding LeftAlt.
    /// Intended for single player use only.
    /// </summary>
    public class ZombieAimbot : MonoBehaviour
    {
        private KeyCode _activationKey = KeyCode.LeftAlt;

        private void Update()
        {
            if (!NewSettings.ZombieAimbot)
                return;

            if (!Input.GetKey(_activationKey))
                return;

            var player = NewSettings.EntityLocalPlayer;
            if (player == null)
                return;

            var zombie = FindClosestZombie(player.transform.position);
            if (zombie == null)
                return;

            Vector3 targetPos = zombie.emodel.GetHeadTransform().position;
            Vector3 dir = targetPos - player.emodel.GetHeadTransform().position;
            if (dir.sqrMagnitude > 0f)
                player.transform.forward = dir.normalized;
        }

        private EntityZombie FindClosestZombie(Vector3 playerPos)
        {
            EntityZombie best = null;
            float bestDist = float.MaxValue;
            foreach (var entity in NewSettings.EntityAlive)
            {
                if (entity is EntityZombie z && z.IsAlive())
                {
                    float d = Vector3.SqrMagnitude(z.GetPosition() - playerPos);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = z;
                    }
                }
            }
            return best;
        }
    }
}
