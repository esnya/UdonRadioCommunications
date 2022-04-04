using UnityEngine;
using UdonSharp;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.Linq;
#endif

namespace UdonRadioCommunication
{
    public class URCUtility
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public static void UdonPublicEventField(SerializedProperty udonProperty, SerializedProperty valueProperty, GUIContent label)
        {
            valueProperty.stringValue = UdonPublicEventField(label, udonProperty.objectReferenceValue as UdonSharpBehaviour, valueProperty.stringValue);
        }
        public static void UdonPublicEventField(SerializedProperty udonProperty, SerializedProperty valueProperty)
        {
            valueProperty.stringValue = UdonPublicEventField(new GUIContent(valueProperty.displayName), udonProperty.objectReferenceValue as UdonSharpBehaviour, valueProperty.stringValue);
        }
        public static void UdonPublicEventField(UdonSharpBehaviour udon, SerializedProperty property)
        {
            property.stringValue = UdonPublicEventField(new GUIContent(property.displayName), udon, property.stringValue);
        }
        public static string UdonPublicEventField(GUIContent label, UdonSharpBehaviour udon, string value)
        {
            if (udon == null) return EditorGUILayout.TextField(label, value);

            var events = udon.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Select(m => m.Name).Prepend("(null)").ToList();
            if (events.Count == 0) return EditorGUILayout.TextField(label, value);

            var index = Mathf.Max(events.FindIndex(e => e == value), 0);
            index = EditorGUILayout.Popup(label, index, events.ToArray());

            var newValue = events.Skip(index).FirstOrDefault();
            return newValue == "(null)" ? null : newValue;
        }
#endif
    }
}
