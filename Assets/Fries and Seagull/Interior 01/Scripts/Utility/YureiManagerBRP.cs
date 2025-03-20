using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
#if UNITY_EDITOR
using Seagull.Interior_01.Utility.Inspector;
#endif

namespace Seagull.Interior_01.Utility {
    public class YureiManagerBRP : MonoBehaviour {
        public GameObject postProcessVolumePrefab;

        [Tooltip("Post Process effect including glowing will only show to these cameras")]
        public List<Camera> gameCameras = new();

        [Tooltip("Which layer should Yurei Post Processor Volume use")]
        public int yureiLayer = -1;
        
        [YureiButton("Initialize")] [IgnoreInInspector]
        public Action initialize;

        private void Reset() {
            initialize = init;
        }

        private void init() {
            if (gameCameras == null || gameCameras.Count == 0) {
                Debug.LogError("Please provide at least 1 valid camera to Game Cameras field.");
                return;
            }
            
            if (string.IsNullOrEmpty(LayerMask.LayerToName(yureiLayer))) {
                Debug.LogError("Please provide a valid layer ID in Yurei Layer field.");
                return;
            }
            
            setupPostProcessorVlume();
            
            foreach (var camera in gameCameras) {
                PostProcessLayer ppl = camera.GetComponent<PostProcessLayer>();
                if (ppl) {
                    ppl.volumeLayer |= LayerMask.GetMask(LayerMask.LayerToName(yureiLayer));
                    continue;
                }
                
                ppl = camera.gameObject.AddComponent<PostProcessLayer>();
                ppl.volumeLayer = LayerMask.GetMask(LayerMask.LayerToName(yureiLayer));
            }
            Debug.Log($"[Yurei] Init post-processor settings for Built-in Rendering Pipeline successfully.");
        }

        private void setupPostProcessorVlume() {
            // 检查现在有没有 Yurei Post Processor
            bool hasValidPostProcessor = false;
            GameObject globalPPVGobj = GameObject.Find("Yurei Post Process Volume");
            PostProcessVolume globalPPV = null;
            if (globalPPVGobj != null) {
                globalPPV = globalPPVGobj.GetComponent<PostProcessVolume>();
                YureiPostProcessorIdentifier yppi = globalPPVGobj.GetComponent<YureiPostProcessorIdentifier>();
                if (yppi != null) hasValidPostProcessor = true;
            }

            // 如果没有，则创建 Yurei Post Process Volume
            if (!hasValidPostProcessor) {
                GameObject postProcessor = GameObject.Instantiate(postProcessVolumePrefab);
                postProcessor.layer = yureiLayer;
                postProcessor.name = "Yurei Post Process Volume";
            }
            // 如果有 则检查它的完整性
            else {
                globalPPVGobj.layer = yureiLayer;
                
                if (globalPPV.sharedProfile == null) {
                    globalPPV.sharedProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
                    // 保存资产到指定路径
                    #if UNITY_EDITOR
                    AssetDatabase.CreateAsset(globalPPV.sharedProfile, "Assets/Yurei Post Process Volume.asset");
                    AssetDatabase.SaveAssets();
                    #endif
                }

                if (globalPPV.sharedProfile.GetSetting<Bloom>() == null)
                    globalPPV.sharedProfile.AddSettings<Bloom>();
                
                Bloom b = globalPPV.sharedProfile.GetSetting<Bloom>();
                if (!b.active) b.active = true;
                if (!b.enabled) b.enabled.value = true;
                
                b.intensity.value = 14.5f;
                b.intensity.overrideState = true;
                b.threshold.value = 2f;
                b.threshold.overrideState = true;
            }
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(YureiManagerBRP))]
    public class YureiInitializerInspector : YureiInspector { }
    #endif
}