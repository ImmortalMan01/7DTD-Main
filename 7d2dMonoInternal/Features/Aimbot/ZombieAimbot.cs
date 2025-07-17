using UnityEngine;
using System.Collections.Generic;
using SETT = SevenDTDMono.NewSettings;

namespace SevenDTDMono.Features.Aimbot
{
    public class ZombieAimbot : MonoBehaviour
    {
        private Dictionary<string, bool> _boolDict;
        private Camera _cam;

        private void Start()
        {
            _boolDict = SETT.Instance.GetChildDictionary<bool>(nameof(Dictionaries.BOOL_DICTIONARY));
            _cam = Camera.main;
        }

        private void Update()
        {
            if (_boolDict == null || !_boolDict[nameof(SettingsBools.AIMBOT)])
                return;

            if (SETT.EntityLocalPlayer == null)
                return;

            if (_cam == null)
                _cam = Camera.main;

            var target = FindTarget();
            if (target != null)
                AimAt(target);
        }

        private EntityZombie FindTarget()
        {
            float best = float.MaxValue;
            EntityZombie bestZombie = null;

            foreach (var alive in SETT.EntityAlive)
            {
                var z = alive as EntityZombie;
                if (z == null || !z.IsAlive())
                    continue;

                Vector3 screenPos = _cam.WorldToScreenPoint(z.emodel.GetHeadTransform().position);
                if (screenPos.z < 0)
                    continue;

                float dist = Vector2.Distance(new Vector2(Screen.width / 2f, Screen.height / 2f), new Vector2(screenPos.x, screenPos.y));
                if (dist < 150f && dist < best)
                {
                    best = dist;
                    bestZombie = z;
                }
            }

            return bestZombie;
        }

        private void AimAt(EntityZombie target)
        {
            Vector3 head = target.emodel.GetHeadTransform().position;
            Vector3 dir = head - SETT.EntityLocalPlayer.emodel.transform.position;
            Quaternion rot = Quaternion.LookRotation(dir);
            SETT.EntityLocalPlayer.emodel.transform.rotation = Quaternion.Slerp(SETT.EntityLocalPlayer.emodel.transform.rotation, rot, Time.deltaTime * 12f);
        }
    }
}
