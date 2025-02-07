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
        public Type c_MemberType { get; private set; }

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

    private PoseGenerator poseGenerator;
    private DatasetGenerator datasetGenerator;

    private Dictionary<Type, bool> groupFoldouts = new Dictionary<Type, bool>();
    private Dictionary<string, bool> labelFoldouts = new Dictionary<string, bool>();

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
        //"randomizePosition",
        //"randomizeRotation",

        // AmbientLightRandomizer
        "ambientIntensityRange",

        // DiceRandomizer
        "meshData",
        "diceProbability",
        "diceScaleRange",
        "diceColors",
        "diceMetallic",
        "diceSmoothness",
        "dotColors",
        "dotMetallic",
        "dotSmoothness",
        "meshContribution",
        "debugMode",

        // ColorRandomizer
        "randomColors",

        // DatasetGenerator
        "width",
        "height",
        "minimumDiceSurface",
        "imageEncoding",
        "datasetSize",
        "yoloFormat",
        "boundingBoxType"
    };

    [MenuItem("SyntheticDice/Simulator")]
    public static void ShowWindow()
    {
        GetWindow<SimulatorEditorWindow>("Simulator");
    }

    private void OnEnable()
    {
        RefreshCache();
    }

    private void RefreshCache()
    {
        groupedRandomizers.Clear();
        groupFoldouts.Clear();
        labelFoldouts.Clear();

        var allRandomizers = FindObjectsOfType<Randomizer>(true);
        poseGenerator = FindObjectOfType<PoseGenerator>();
        datasetGenerator = FindObjectOfType<DatasetGenerator>();

        foreach (var randomizer in allRandomizers)
        {
            var type = randomizer.GetType();

            if (!groupedRandomizers.ContainsKey(type))
            {
                groupedRandomizers[type] = new List<Randomizer>();
                groupFoldouts[type] = false; // Defaults group to folded
            }

            groupedRandomizers[type].Add(randomizer);
            labelFoldouts[randomizer.name] = false; // Defaults randomizer to folded
        }
        // Sort the groups by name
        groupedRandomizers = groupedRandomizers.OrderBy(kv => kv.Key.Name).ToDictionary(kv => kv.Key, kv => kv.Value);

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
        if (GUILayout.Button(new GUIContent("Refresh", "Refresh the cached objects.")))
        {
            RefreshCache();
        }
        EditorGUILayout.Space(8f);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Pose Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        foreach (var group in groupedRandomizers)
        {
            Type childType = group.Key;
            List<Randomizer> randomizers = group.Value;

            int count = randomizers.Count;
            string groupName = FormatLabel(childType.Name) + (count > 1 ? "s" : "");
            groupFoldouts[childType] = EditorGUILayout.Foldout(groupFoldouts[childType], $"{groupName} ({randomizers.Count})", true);

            if (groupFoldouts[childType])
            {
                EditorGUI.indentLevel++;

                foreach (var randomizer in randomizers)
                {
                    string randomizerName = FormatLabel(randomizer.name);
                    labelFoldouts[randomizer.name] = EditorGUILayout.Foldout(labelFoldouts[randomizer.name], randomizerName, true);

                    if (labelFoldouts[randomizer.name])
                    {
                        EditorGUI.indentLevel++;
                        DisplayTargetProperties(randomizer, randomizer.GetType());
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space(4f);

        EditorGUI.indentLevel--;
        if (poseGenerator != null)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Randomize Pose", "Choose values from all randomizers and apply them to the scene.")))
            {
                poseGenerator.Generate();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Dataset Settings", EditorStyles.boldLabel);

        if (datasetGenerator != null)
        {
            EditorGUI.indentLevel++;
            DisplayTargetProperties(datasetGenerator, datasetGenerator.GetType());
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (datasetGenerator.CurrentCoroutine == null)
            {
                EditorApplication.update -= Repaint;
                if (GUILayout.Button(new GUIContent("Generate", "Generate a dataset using the current settings.")))
                {
                    datasetGenerator.Generate();
                    EditorApplication.update += Repaint;
                }
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Stop", "Stop the ongoing dataset generation.")))
                {
                    datasetGenerator.Stop();
                    EditorApplication.update -= Repaint;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (datasetGenerator.CurrentCoroutine != null)
            {
                float progress = datasetGenerator.CoroutineProgress;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{progress * 100f:F2}%");
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DisplayTargetProperties(UnityEngine.Object obj, Type type)
    {
        Undo.RecordObject(obj, "Modify " + obj.name); // Record changes for undo

        // Display targeted fields
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (TargetProperties.Contains(field.Name))
            {
                object value = field.GetValue(obj);
                object newValue = DrawField(field, value);

                if (!Equals(value, newValue))
                {
                    field.SetValue(obj, newValue);
                    EditorUtility.SetDirty(obj); // Mark as dirty for Undo

                    SerializedObject so = new SerializedObject(obj);
                    so.ApplyModifiedProperties();
                }
            }
        }

        // Display targeted properties
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.CanRead && property.CanWrite && TargetProperties.Contains(property.Name))
            {
                object value = property.GetValue(obj);
                object newValue = DrawField(property, value);

                if (!Equals(value, newValue))
                {
                    property.SetValue(obj, newValue);
                    EditorUtility.SetDirty(obj); // Mark as dirty for Undo

                    SerializedObject so = new SerializedObject(obj);
                    so.ApplyModifiedProperties();
                }
            }
        }
    }

    private object DrawField(MemberInfo member, object value)
    {
        string label = member.Name;
        string formattedLabel = FormatLabel(label);

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
                if (customAttribute is RangeAttribute rangeAttribute) {
                    if (type == typeof(int))
                    {
                        return EditorGUILayout.IntSlider(formattedLabel, (int)value, (int)rangeAttribute.min, (int)rangeAttribute.max);
                    }
                    if (type == typeof(float))
                    {
                        return EditorGUILayout.Slider(formattedLabel, (float)value, rangeAttribute.min, rangeAttribute.max);
                    }
                }
                if (customAttribute is FloatRangeSliderAttribute floatRangeSlider)
                {
                    return DrawFloatRangeSlider(formattedLabel, value, floatRangeSlider);
                }
            }
        }

        // Check and handle custom types
        if (type == typeof(Vector3Range))
        {
            return DrawVector3Range(formattedLabel, value);
        }
        if (type == typeof(DiceMeshData))
        {
            return DrawDiceMeshData(formattedLabel, value);
        }

        if (type == typeof(int))
        {
            return EditorGUILayout.IntField(formattedLabel, (int)value);
        }
        if (type == typeof(float))
        {
            return EditorGUILayout.FloatField(formattedLabel, (float)value);
        }
        if (type == typeof(string))
        {
            return EditorGUILayout.TextField(formattedLabel, (string)value);
        }
        if (type == typeof(bool))
        {
            return EditorGUILayout.Toggle(formattedLabel, (bool)value);
        }
        if (type == typeof(Vector3))
        {
            return EditorGUILayout.Vector3Field(formattedLabel, (Vector3)value);
        }
        if (type == typeof(Color))
        {
            return EditorGUILayout.ColorField(formattedLabel, (Color)value);
        }
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return EditorGUILayout.ObjectField(formattedLabel, (UnityEngine.Object)value, type, true);
        }
        if (type.IsEnum)
        {
            return EditorGUILayout.EnumPopup(formattedLabel, (Enum)value);
        }
        if (type.IsArray)
        {
            return DrawArrayField(formattedLabel, value, type);
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return DrawListField(formattedLabel, value, type);
        }

        EditorGUILayout.LabelField($"{formattedLabel}: {value} (Unsupported type)");
        return value;
    }

    private object DrawArrayField(string label, object value, Type arrayType)
    {
        Type elementType = arrayType.GetElementType();
        var array = (Array)value;

        string formattedLabel = label.Split('>').Last(); // Remove parent label
        formattedLabel = FormatLabel(formattedLabel); // Apply Unity label formatting

        if (!labelFoldouts.ContainsKey(label))
        {
            labelFoldouts[label] = false; // Defaults array to folded
        }

        labelFoldouts[label] = EditorGUILayout.Foldout(labelFoldouts[label], formattedLabel, true);
        if (!labelFoldouts[label])
        {
            return array;
        }

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
            object newElement = DrawField(new MemberInfoWrapper($"{formattedLabel} [{i}]", elementType), element);

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

        string formattedLabel = label.Split('>').Last(); // Remove parent label
        formattedLabel = FormatLabel(formattedLabel); // Apply Unity label formatting

        if (!labelFoldouts.ContainsKey(label))
        {
            labelFoldouts[label] = false; // Defaults list to folded
        }

        labelFoldouts[label] = EditorGUILayout.Foldout(labelFoldouts[label], formattedLabel, true);
        if (!labelFoldouts[label])
        {
            return list;
        }

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
            object newElement = DrawField(new MemberInfoWrapper($"{formattedLabel} [{i}]", elementType), element);

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
        EditorGUILayout.BeginHorizontal();

        minValue = EditorGUILayout.FloatField("", minValue, GUILayout.MaxWidth(110f), GUILayout.ExpandWidth(false));
        EditorGUILayout.MinMaxSlider(
            ref minValue, ref maxValue, limit.Min, limit.Max, GUILayout.MinWidth(100f), GUILayout.ExpandWidth(true)
        );
        maxValue = EditorGUILayout.FloatField("", maxValue, GUILayout.MaxWidth(110f), GUILayout.ExpandWidth(false));

        //GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

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

    private object DrawDiceMeshData(string label, object value, bool foldable = false)
    {
        DiceMeshData diceMeshData = value as DiceMeshData;

        if (!labelFoldouts.ContainsKey(label))
        {
            labelFoldouts[label] = false; // Defaults mesh data to folded
        }

        labelFoldouts[label] = EditorGUILayout.Foldout(labelFoldouts[label], label, true);
        if (!labelFoldouts[label])
        {
            return diceMeshData;
        }

        EditorGUI.indentLevel++;
        diceMeshData.Mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", diceMeshData.Mesh, typeof(Mesh), false);
        DrawArrayField($"{label}>ValueToRotation", diceMeshData.ValueToRotation, typeof(Vector3[]));
        EditorGUI.indentLevel--;

        return diceMeshData;
    }

    private string FormatLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return label;
        }

        var formattedLabel = System.Text.RegularExpressions.Regex.Replace(label, "(\\B[A-Z])", " $1");
        return char.ToUpper(formattedLabel[0]) + formattedLabel.Substring(1);
    }
}
