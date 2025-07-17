using UnityEngine;

using System.Collections.Generic;
using SevenDTDMono.Features;

namespace SevenDTDMono.Features.Render
{
    internal class Render : MonoBehaviour
    {


        #region Local settings
        private static Camera _mainCam;



        private Color _colorBlack;
        private Color _entityBoxColor;
        private Color _crossColor;
        private readonly float _crossScale = 14f;
        private readonly float _crossLineThickness = 1.75f;
        //private static Dictionary<string, object> Settings => NewSettings.Instance.SettingsDictionary; //get instance of SettingsDictionary
        private static NewSettings SettingsInstance => NewSettings.Instance;



        #endregion




        private void Start()
        {
            Debug.LogWarning($"Start: {nameof(Render)}");
            // Camera.main is a very expensive getter, so we want to do it once and cache the result.
            _mainCam = Camera.main;

            _colorBlack = new Color(0f, 0f, 0f, 120f);
            _entityBoxColor = new Color(0.42f, 0.36f, 0.90f, 1f);
            _crossColor = new Color32(30, 144, 255, 255);
        }

        private void OnGUI()
        {

            if (NewSettings.GameManager.gameStateManager.bGameStarted == false && NewSettings.EntityLocalPlayer == null)
            {
                //if game is not started and player is null return
                return;
            }
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!_mainCam)
            {
                _mainCam = Camera.main;
            }


            if (SettingsInstance.GetBoolValue(nameof(SettingsBools.CROSS_HAIR))) //crosshair Function
            {
                // Constantly redefining these vectors so that you can change your resolution and the crosshair will still be in the middle.
                Vector2 lineHorizontalStart = new Vector2(Screen.width / 2 - _crossScale, Screen.height / 2);
                Vector2 lineHorizontalEnd = new Vector2(Screen.width / 2 + _crossScale, Screen.height / 2);

                Vector2 lineVerticalStart = new Vector2(Screen.width / 2, Screen.height / 2 - _crossScale);
                Vector2 lineVerticalEnd = new Vector2(Screen.width / 2, Screen.height / 2 + _crossScale);

                RenderUtils.DrawLine(lineHorizontalStart, lineHorizontalEnd, _crossColor, _crossLineThickness);
                RenderUtils.DrawLine(lineVerticalStart, lineVerticalEnd, _crossColor, _crossLineThickness);
            }
            if (SettingsInstance.GetBoolValue(nameof(SettingsBools.FOV_CIRCLE)))
            {
                float radius = 150f;
                if (Camera.main)
                {
                    float angleRatio = Mathf.Tan(SettingsInstance.AimbotFov * Mathf.Deg2Rad * 0.5f) /
                                       Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f);
                    radius = (Screen.height / 2f) * angleRatio;
                }

                // Outline
                RenderUtils.DrawCircle(Color.black, new Vector2(Screen.width / 2, Screen.height / 2), radius - 1f);
                RenderUtils.DrawCircle(Color.black, new Vector2(Screen.width / 2, Screen.height / 2), radius + 1f);
                RenderUtils.DrawCircle(new Color32(30, 144, 255, 255), new Vector2(Screen.width / 2, Screen.height / 2), radius);
            }

            if (SettingsInstance.GetBoolValue(nameof(SettingsBools.BULLET_PATH)) && Aimbot.HasTarget && Camera.main)
            {
                Vector3 startScreen = Camera.main.WorldToScreenPoint(Aimbot.StartPos);
                Vector3 endScreen = Camera.main.WorldToScreenPoint(Aimbot.TargetPos);
                if (RenderUtils.IsOnScreen(startScreen) && RenderUtils.IsOnScreen(endScreen))
                {
                    Vector2 start = new Vector2(startScreen.x, Screen.height - startScreen.y);
                    Vector2 end = new Vector2(endScreen.x, Screen.height - endScreen.y);
                    RenderUtils.DrawLine(start, end, Color.red, 2f);
                }
            }




        }




    }
}
