using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SevenDTDMono.GuiLayoutExtended
{
    public partial class NewGUILayout
    {
        private static NewSettings SettingsInstance => NewSettings.Instance;
        private static Dictionary<string, object> Settings => NewSettings.Instance.SettingsDictionary;
        
        private static Dictionary<string, bool> _boolDict = SettingsInstance.GetChildDictionary<bool>(nameof(Dictionaries.BOOL_DICTIONARY));




        // Modern color palette for UI elements
        private static readonly Color Active = new Color(0.2f, 0.8f, 1f);
        private static readonly Color Inactive = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color Hover = new Color(0.3f, 1f, 1f);
        



        //should be moved to render or utils or something.
        public static void DrawLine(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }


    }
}
