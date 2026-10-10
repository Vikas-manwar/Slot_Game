using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlotGame.EditorTools
{
    internal static class SerializedWiring
    {
        public static void Wire(Object target, params (string field, object value)[] assignments)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in assignments)
            {
                var property = so.FindProperty(field);
                if (property == null)
                    throw new ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
                Assign(property, value);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(SerializedProperty property, object value)
        {
            switch (value)
            {
                case null:
                    property.objectReferenceValue = null;
                    break;
                case Object unityObject:
                    property.objectReferenceValue = unityObject;
                    break;
                case float f:
                    property.floatValue = f;
                    break;
                case int i:
                    property.intValue = i;
                    break;
                case bool b:
                    property.boolValue = b;
                    break;
                case Vector2 v:
                    property.vector2Value = v;
                    break;
                case Object[] array:
                    property.arraySize = array.Length;
                    for (int n = 0; n < array.Length; n++)
                        property.GetArrayElementAtIndex(n).objectReferenceValue = array[n];
                    break;
                default:
                    throw new NotSupportedException($"Cannot assign {value.GetType().Name} to '{property.name}'.");
            }
        }
    }
}
