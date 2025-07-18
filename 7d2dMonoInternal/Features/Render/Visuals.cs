﻿
using System.Collections.Generic;
using UnityEngine;

namespace SevenDTDMono.Features.Render
{
    public class Visuals : MonoBehaviour 
    {


        //[DllImport("user32.dll")]
        //private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        #region vars
        private int _Color;
        private float lastChamTime;
        private Material chamsMaterial;
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private bool chamsEnabled;
        #endregion


        private void Start()
        {

            Debug.LogWarning($"Start: {nameof(Visuals)}");

            chamsEnabled = false;

            lastChamTime = Time.time + 10f;

            chamsMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) 
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            
            /* 
             * Unity hases the ID of every property name you feed it, 
             * so we're hashing it once instead of every time we want to use it.
             */
            _Color = Shader.PropertyToID("_Color");

            chamsMaterial.SetInt("_SrcBlend", 5);
            chamsMaterial.SetInt("_DstBlend", 10);
            chamsMaterial.SetInt("_Cull", 0);
            chamsMaterial.SetInt("_ZTest", 8); // 8 = see through walls.
            chamsMaterial.SetInt("_ZWrite", 0);
            chamsMaterial.SetColor(_Color, Color.magenta);
        }
        private void Update()
        {
            /*if (!Input.anyKey || !Input.anyKeyDown) {
                return;
            }*/
            if (NewSettings.GameManager.gameStateManager.bGameStarted == false && NewSettings.EntityLocalPlayer == null)
            {
                //if game is not started and player is null return
                return;
            }



            bool setting = NewSettings.Instance.GetBoolValue(nameof(SettingsBools.CHAMS));

            if (!setting && chamsEnabled)
            {
                RemoveChams();
            }

            chamsEnabled = setting;

            if (Time.time >= lastChamTime && setting)
            {
                foreach (Entity entity in FindObjectsOfType<Entity>())
                {
                    if (!entity)
                    {
                        continue;
                    }
                    switch (entity.entityType)
                    {
                        case EntityType.Zombie:
                            ApplyChams(entity, Color.red);
                            break;
                        case EntityType.Player:
                            ApplyChams(entity, Color.cyan);
                            break;
                        case EntityType.Animal:
                            ApplyChams(entity, Color.yellow);
                            break;
                        case EntityType.Unknown:
                            ApplyChams(entity, Color.white);
                            break;
                    }
                }
                lastChamTime = Time.time + 10f;
            }//Enable disable chams
        }
        private void ApplyChams(Entity entity, Color color)  //applying chams
        {
            foreach (Renderer renderer in entity.GetComponentsInChildren<Renderer>())
            {
                if (!originalMaterials.ContainsKey(renderer))
                {
                    originalMaterials[renderer] = renderer.sharedMaterials;
                }

                var mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = chamsMaterial;
                }
                renderer.materials = mats;

                renderer.material.SetColor(_Color, color);
            }
        }

        private void RemoveChams()
        {
            foreach (var pair in originalMaterials)
            {
                if (pair.Key)
                {
                    pair.Key.materials = pair.Value;
                }
            }
            originalMaterials.Clear();
        }
    }
}
