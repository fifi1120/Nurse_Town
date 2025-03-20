#if UNITY_EDITOR

using System;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Seagull.Interior_01.Utility.Inspector {
    
    [CustomEditor(typeof(Audio))]
    public class YureiInspector : Editor {
        
        private SerializedObject serializedObj;

        private void OnEnable() {
            // 当编辑器检查时，获取目标的序列化对象
            serializedObj = new SerializedObject(target);
        }
        
        public override void OnInspectorGUI() {
            // 获取 Target Type 实例
            Type type = target.GetType();

            // 获取 Target 类型的所有属性
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            // 开始检测值变化
            EditorGUI.BeginChangeCheck();
            
            foreach (var field in fields) {
                SerializedProperty prop = serializedObj.FindProperty(field.Name);
                
                // 检查属性是否为布尔型
                if (field.FieldType == typeof(UnityEvent) || field.FieldType == typeof(Action)) {
                    // 检测这个属性上有没有 YureiButton Attribute
                    YureiButtonAttribute attr = field.GetCustomAttribute<YureiButtonAttribute>();
                    if (attr != null) {
                        // 如果有 YureiButton Attribute，则绘制一个按钮
                        string name = field.Name;
                        if (attr.text != null) name = attr.text;

                        if (field.FieldType == typeof(UnityEvent) && !Application.isPlaying) 
                            name = $"{name} (Require to start the game)";
                        
                        if (GUILayout.Button(name)) {
                            // 如果按钮被按下，则执行 UnityEvent
                            if (field.FieldType == typeof(UnityEvent))
                                ((UnityEvent)field.GetValue(target)).Invoke();
                            else {
                                try {
                                    ((Action)field.GetValue(target)).Invoke();
                                }
                                catch (Exception) {
                                    MethodInfo startMethod = target.GetType().GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                    if (startMethod != null && startMethod.GetParameters().Length == 0) {
                                        startMethod.Invoke(target, null);
                                        ((Action)field.GetValue(target)).Invoke();
                                    }
                                }
                            }
                        }
                    }
                }
                
                // 绘制当前值的输入框或其他交互框
                if (field.GetCustomAttribute<IgnoreInInspectorAttribute>() == null)
                    EditorGUILayout.PropertyField(prop, true);
            }
            
            // 结束检测值变化
            EditorGUI.EndChangeCheck();
            serializedObj.ApplyModifiedProperties();
        }
    }

    
}

#endif