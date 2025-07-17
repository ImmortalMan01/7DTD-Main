using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using SevenDTDMono.Features.Render;

namespace SevenDTDMono.Features.Aimbot
{
    public class ZombieAimbot : MonoBehaviour
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private Camera _cam;

        private void Start()
        {
            _cam = ESP.mainCam ?? Camera.main;
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && NewSettings.Instance.GetBoolValue(nameof(SettingsBools.AIMBOT)))
            {
                AimAtZombie();
            }
        }

        private void AimAtZombie()
        {
            if (_cam == null)
            {
                _cam = ESP.mainCam ?? Camera.main;
            }

            var zombies = NewSettings.EntityAlive
                .Where(e => e is EntityZombie && e.IsAlive())
                .Cast<EntityZombie>();

            if (!zombies.Any())
                return;

            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            float minDist = float.MaxValue;
            Vector2 target = Vector2.zero;

            foreach (var zombie in zombies)
            {
                Vector3 head = zombie.emodel.GetHeadTransform().position;
                Vector3 w2s = _cam.WorldToScreenPoint(head);
                Vector2 screenPos = new Vector2(w2s.x, Screen.height - w2s.y);
                if (!RenderUtils.IsOnScreen(w2s))
                    continue;

                float dist = Vector2.Distance(center, screenPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    target = screenPos;
                }
            }

            if (target != Vector2.zero)
            {
                Vector2 delta = target - center;
                mouse_event(0x0001, (int)(delta.x / 10f), (int)(delta.y / 10f), 0, 0);
            }
        }
    }
}
