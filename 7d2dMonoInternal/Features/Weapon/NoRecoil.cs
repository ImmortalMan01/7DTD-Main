using System.Reflection;
using UnityEngine;

namespace SevenDTDMono.Features.Weapon
{
    public class NoRecoil : MonoBehaviour
    {
        private static NewSettings SettingsInstance => NewSettings.Instance;
        private static EntityPlayerLocal Player => NewSettings.EntityLocalPlayer;

        private readonly string[] fieldNames =
        {
            "recoilYawMin", "recoilYawMax",
            "recoilPitchMin", "recoilPitchMax"
        };

        private void Update()
        {
            if (!SettingsInstance.GetBoolValue(nameof(SettingsBools.NO_RECOIL)))
                return;

            if (Player == null)
                return;

            var inv = Player.inventory;
            if (inv == null)
                return;

            var gun = inv.GetHoldingGun();
            if (gun == null)
                return;

            var type = gun.GetType();
            foreach (var name in fieldNames)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(float))
                {
                    field.SetValue(gun, 0f);
                }
            }
        }
    }
}
