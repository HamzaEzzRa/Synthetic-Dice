using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SimulatorEditorWindow : EditorWindow
{
    private class MemberInfoWrapper : MemberInfo
    {
        public string c_Name { get; private set; }
        public Type c_MemberType { get; private set;  }

        public MemberInfoWrapper(string name, Type memberType)
        {
            c_Name = name;
            c_MemberType = memberType;
        }

        public override string Name => c_Name;
        public override Type DeclaringType => null;
        public override MemberTypes MemberType => MemberTypes.Custom;
        public override Type ReflectedType => null;
        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();
        public override bool IsDefined(Type attributeType, bool inherit) => false;
    }

    private Vector2 scrollPosition;
    private Dictionary<Type, List<Randomizer>> groupedRandomizers = new Dictionary<Type, List<Randomizer>>();
    private Dictionary<Type, bool> groupFoldouts = new Dictionary<Type, bool>();

    // Define list of properties/fields to display by name
    private static readonly HashSet<string> TargetProperties = new HashSet<string>
    {
        // CameraRandomizer
        "positionRange",
        "fieldOfViewRange",
        "grainIntensityRange",
        "grainSizeRange",
        "bloomIntensityRange",
        "bloomThresholdRange",

        // LightRandomizer
        "intensityRange",
        "positionRange",
        "eulerAnglesRange",
        "randomizePosition",
        "randomizeRotation",

        // DiceRandomizer

        // ColorRandomizer
        "randomColors",
    };

    [MenuItem("SyntheticDice/Simulator")]
    public static void ShowWindow()
    {
        GetWindow<SimulatorEditorWindow>("Simulator");
    }

    private void OnEnable()
    {
        CacheRandomizers();
    }

    private void CacheRandomizers()
    {
        groupedRandomizers.Clear();
        groupFoldouts.Clear();

        var allRandomizers = FindObjectsOfType<Randomizer>();

        foreach (var randomizer in allRandomizers)
        {
            var type = randomizer.GetType();

            if (!groupedRandomizers.ContainsKey(type))
            {
                groupedRandomizers[type] = new List<Randomizer>();
                groupFoldouts[type] = false; // Defaults to folded
            }

            groupedRandomizers[type].Add(randomizer);
        }

        // Sort the objects in each group by name
        var sortedRandomizers = new Dictionary<Type, List<Randomizer>>();
        foreach (var group in groupedRandomizers)
        {
            sortedRandomizers[group.Key] = group.Value.OrderBy(r => r.gameObject.name).ToList();
        }
        groupedRandomizers = sortedRandomizers;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Refresh"))
        {
            CacheRandomizers();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var group in groupedRandomizers)
        {
            Type childType = group.Key;
            List<Randomizer> randomizers = group.Value;

            groupFoldouts[childType] = EditorGUILayout.Foldout(groupFoldouts[childType], $"{childType.Name} ({randomizers.Count})");

            if (groupFoldouts[childType])
            {
                EditorGUI.indentLevel++;

                foreach (var randomizer in randomizers)
                {
                    EditorGUILayout.LabelField($"{randomizer.name}", EditorStyles.boldLabel);
                    DisplayTargetedProperties(randomizer);
                }

                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DisplayTargetedProperties(Randomizer randomizer)
    {
        var type = randomizer.GetType();

        // Display targeted fields
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (TargetProperties.Contains(field.Name))
            {
                object value = field.GetValue(randomizer);
                object newValue = DrawField(field, value);

                if (!Equals(value, newValue))
                {
                    field.SetValue(randomizer, newValue);
                    EditorUtility.SetDirty(randomizer); // Mark as dirty for Undo
                }
            }
        }

        // Display targeted properties
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.CanRead && property.CanWrite && TargetProperties.Contains(property.Name))
            {
                object value = property.GetValue(randomizer);
                object newValue = DrawField(property, value);

                if (!Equals(value, newValue))
                {
                    property.SetValue(randomizer, newValue);
                    EditorUtility.SetDirty(randomizer); // Mark as dirty for Undo
                }
            }
        }
    }

    private object DrawField(MemberInfo member, object value)
    {
        string label = member.Name;
        Type type;
        if (member is PropertyInfo pi)
        {
            type = pi.PropertyType;
        }
        else if (member is FieldInfo fi)
        {
            type = fi.FieldType;
        }
        else if (member is MemberInfoWrapper miw)
        {
            type = miw.c_MemberType;
        }
        else
        {
            throw new ArgumentException("Member must be a PropertyInfo or FieldInfo or MemberInfoWrapper");
        }

        // Check and handle custom attributes
        object[] customAttributes = member.GetCustomAttributes(typeof(PropertyAttribute), true);
        if (customAttributes.Length > 0)
        {
            foreach (var customAttribute in customAttributes)
            {
                if (customAttribute is FloatRangeSliderAttribute floatRangeSlider)
                {
                    return DrawFloatRangeSlider(label, value, floatRangeSlider);
                }
            }
        }

        // Check and handle custom types
        if (type == typeof(Vector3Range))
        {
            return DrawVector3Range(label, value);
        }

        if (type == typeof(int))
        {
            return EditorGUILayout.IntField(label, (int)value);
        }
        if (type == typeof(float))
        {
            return EditorGUILayout.FloatField(label, (float)value);
        }
        if (type == typeof(string))
        {
            return EditorGUILayout.TextField(label, (string)value);
        }
        if (type == typeof(bool))
        {
            return EditorGUILayout.Toggle(label, (bool)value);
        }
        if (type == typeof(Vector3))
        {
            return EditorGUILayout.Vector3Field(label, (Vector3)value);
        }
        if (type == typeof(Color))
        {
            return EditorGUILayout.ColorField(label, (Color)value);
        }
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return EditorGUILayout.ObjectField(label, (UnityEngine.Object)value, type, true);
        }
        if (type.IsArray)
        {
            return DrawArrayField(label, value, type);
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return DrawListField(label, value, type);
        }

        EditorGUILayout.LabelField($"{label}: {value} (Unsupported type)");
        return value;
    }

    private object DrawArrayField(string label, object value, Type arrayType)
    {
        Type elementType = arrayType.GetElementType();
        var array = (Array)value;

        EditorGUILayout.LabelField(label);
        EditorGUI.indentLevel++;

        if (array == null)
        {
            EditorGUILayout.LabelField("Array is null.");
            EditorGUI.indentLevel--;
            return null;
        }

        // Render the size field and resize the array if necessary
        int arraySize = array.Length;
        int newSize = EditorGUILayout.IntField("Size", arraySize);

        if (newSize != arraySize)
        {
            var newArray = Array.CreateInstance(elementType, newSize);
            for (int i = 0; i < newSize; i++)
            {
                if (i < arraySize)
                {
                    newArray.SetValue(array.GetValue(i), i);
                }
                else
                {
                    newArray.SetValue(Activator.CreateInstance(elementType), i);
                }
            }
            array = newArray;
        }

        // Call DrawField for each element in the array
        for (int i = 0; i < array.Length; i++)
        {
            object element = array.GetValue(i);
            object newElement = DrawField(new MemberInfoWrapper($"Element {i}", elementType), element);

            if (!Equals(element, newElement))
            {
                array.SetValue(newElement, i);
            }
        }

        EditorGUI.indentLevel--;
        return array;
    }

    private object DrawListField(string label, object value, Type listType)
    {
        Type elementType = listType.GetGenericArguments()[0];
        var list = (System.Collections.IList)value;

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (list == null)
        {
            EditorGUILayout.LabelField("List is null.");
            EditorGUI.indentLevel--;
            return null;
        }

        // Render the size field and resize the list if necessary
        int listSize = list.Count;
        int newSize = EditorGUILayout.IntField("Size", listSize);

        if (newSize != listSize)
        {
            var newList = (System.Collections.IList)Activator.CreateInstance(listType);
            for (int i = 0; i < newSize; i++)
            {
                newList.Add(i < listSize ? list[i] : Activator.CreateInstance(elementType));
            }
            list = newList;
        }

        for (int i = 0; i < list.Count; i++)
        {
            object element = list[i];
            object newElement = DrawField(new MemberInfoWrapper($"Element {i}", elementType), element);

            if (!Equals(element, newElement))
            {
                list[i] = newElement;
            }
        }

        EditorGUI.indentLevel--;
        return list;
    }

    private object DrawFloatRangeSlider(string label, object value, FloatRangeSliderAttribute attribute)
    {
        FloatRange range = (FloatRange)value;
        float minValue = range.Min;
        float maxValue = range.Max;

        EditorGUILayout.LabelField(label);
        EditorGUI.indentLevel++;

        // Min-Max slider logic
        FloatRangeSliderAttribute limit = attribute;
        EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, limit.Min, limit.Max);

        minValue = EditorGUILayout.FloatField("Min Value", minValue);
        maxValue = EditorGUILayout.FloatField("Max Value", maxValue);

        if (minValue < limit.Min)
        {
            minValue = limit.Min;
        }

        if (maxValue < minValue)
        {
            maxValue = minValue;
        }
        else if (maxValue > limit.Max)
        {
            maxValue = limit.Max;
        }

        value = new FloatRange(minValue, maxValue);

        EditorGUI.indentLevel--;
        return value;
    }

    private object DrawVector3Range(string label, object value)
    {
        Vector3Range range = (Vector3Range)value;
        Vector3 minValue = range.Min;
        Vector3 maxValue = range.Max;

        EditorGUILayout.LabelField(label);
        EditorGUI.indentLevel++;

        minValue = EditorGUILayout.Vector3Field("Min", minValue);
        maxValue = EditorGUILayout.Vector3Field("Max", maxValue);

        maxValue.x = Mathf.Max(minValue.x, maxValue.x);
        maxValue.y = Mathf.Max(minValue.y, maxValue.y);
        maxValue.z = Mathf.Max(minValue.z, maxValue.z);

        value = new Vector3Range(minValue, maxValue);

        EditorGUI.indentLevel--;
        return value;
    }
}
