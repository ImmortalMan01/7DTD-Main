using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SevenDTDMono.Features
{
    public class AutoQuit : MonoBehaviour
    {
        private float timer = 0f;
        private bool triggered = false;

        [DllImport("user32.dll")]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        private void Update()
        {
            timer += Time.deltaTime;
            if (!triggered && timer >= 180f)
            {
                triggered = true;
                MessageBox(IntPtr.Zero, "Yayyamla oyna", "Uyarı", 0);
                Application.Quit();
            }
        }
    }
}
