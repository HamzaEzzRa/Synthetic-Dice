using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static UnityEngine.GraphicsBuffer;

public class SimulatorEditorWindow : EditorWindow
{
    [Serializable]
    private class SerializablePrimitive<T>
    {
        public T value;

        public SerializablePrimitive(T value)
        {
            this.value = value;
        }
    }

    [Serializable]
    private class SerializableArray<T>
    {
        public T[] data;
    }

    private class MemberInfoWrapper : MemberInfo
    {
        public string c_Name { get; private set; }
        public Type c_MemberType { get; private set; }
        public object[] c_CustomAttributes { get; private set; }
        public UnityEngine.Object c_UnityObject { get; private set; }

        public MemberInfoWrapper(string name, Type memberType, object[] customAttributes = null, UnityEngine.Object unityObject = null)
        {
            c_Name = name;
            c_MemberType = memberType;
            c_CustomAttributes = customAttributes ?? Array.Empty<object>();
            c_UnityObject = unityObject;
        }

        public override string Name => c_Name;
        public override Type DeclaringType => null;
        public override MemberTypes MemberType => MemberTypes.Custom;
        public override Type ReflectedType => null;

        public override object[] GetCustomAttributes(bool inherit) => c_CustomAttributes;

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return c_CustomAttributes.Where(
                attr => attributeType.IsAssignableFrom(attr.GetType()) || attr.GetType().IsSubclassOf(attributeType)
            ).ToArray();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return c_CustomAttributes.Any(
                attr => attributeType.IsAssignableFrom(attr.GetType()) || attr.GetType().IsSubclassOf(attributeType)
            );
        }

        public UnityEngine.Object GetUnityObject() => c_UnityObject;
    }

    private Vector2 scrollPosition;
    private Dictionary<Type, List<Randomizer>> groupedRandomizers = new Dictionary<Type, List<Randomizer>>();

    private PoseGenerator poseGenerator;
    private DatasetGenerator datasetGenerator;

    private Dictionary<Type, bool> groupFoldouts = new Dictionary<Type, bool>();
    private Dictionary<string, bool> labelFoldouts = new Dictionary<string, bool>();

    private static string copiedDataJson = null;

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
        //"meshData",
        "diceProbability",
        "diceScaleRange",
        "diceColors",
        "diceMetallic",
        "diceSmoothness",
        "dotColors",
        "dotMetallic",
        "dotSmoothness",
        //"meshContribution",
        //"debugMode",

        // ColorRandomizer
        "randomColors",

        // DatasetGenerator
        "width",
        "height",
        "minimumDiceSurface",
        "imageEncoding",
        "datasetSize",
        //"yoloFormat",
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
                    labelFoldouts[randomizer.name] = DrawFoldoutWithContextMenu(
                        labelFoldouts[randomizer.name], randomizer.name, true, randomizer
                    );

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
        FieldInfo[] objectFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (TargetProperties.Contains(field.Name))
            {
                object value = field.GetValue(obj);
                object newValue = DrawField(
                    new MemberInfoWrapper(
                        $"{obj.name}>{field.Name}",
                        field.FieldType,
                        field.GetCustomAttributes(true),
                        obj
                    ), 
                    value
                );

                if (!Equals(value, newValue))
                {
                    field.SetValue(obj, newValue);
                    EditorUtility.SetDirty(obj); // Mark as dirty for Undo

                    SerializedObject so = new SerializedObject(obj);
                    so.ApplyModifiedProperties();

                    // Send message to the object to force script OnValidate method to be called
                    MethodInfo onValidate = type.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (onValidate != null)
                    {
                        onValidate.Invoke(obj, null);
                    }
                }
            }
        }

        // Display targeted properties
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.CanRead && property.CanWrite && TargetProperties.Contains(property.Name))
            {
                object value = property.GetValue(obj);
                object newValue = DrawField(
                    new MemberInfoWrapper(
                        $"{obj.name}>{property.Name}",
                        property.PropertyType,
                        property.GetCustomAttributes(true),
                        obj
                    ),
                    value
                );

                if (!Equals(value, newValue))
                {
                    property.SetValue(obj, newValue);
                    EditorUtility.SetDirty(obj); // Mark as dirty for Undo

                    SerializedObject so = new SerializedObject(obj);
                    so.ApplyModifiedProperties();

                    // Send OnValidate message to the object
                    MethodInfo onValidate = type.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (onValidate != null)
                    {
                        onValidate.Invoke(obj, null);
                    }
                }
            }
        }
    }

    private object DrawField(MemberInfoWrapper member, object value)
    {
        string label = member.Name;
        string formattedLabel = label.Split('>').Last(); // Remove parent label
        formattedLabel = FormatLabel(formattedLabel); // Apply Unity label formatting

        Type type = member.c_MemberType;

        // Check and handle custom types
        if (type == typeof(Vector3Range))
        {
            return DrawVector3Range(label, value, member.GetUnityObject());
        }
        if (type == typeof(DiceMeshData)) // Already serializable, so no need to pass Unity object
        {
            return DrawDiceMeshData(label, value);
        }
        if (type.IsArray)
        {
            return DrawArrayField(label, value, type, member.GetUnityObject());
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return DrawListField(label, value, type, member.GetUnityObject());
        }

        object newValue = null;

        // Check and handle custom attributes
        object[] customAttributes = member.GetCustomAttributes(typeof(PropertyAttribute), true);
        if (customAttributes.Length > 0)
        {
            foreach (var customAttribute in customAttributes)
            {
                if (customAttribute is RangeAttribute rangeAttribute) {
                    if (type == typeof(int))
                    {
                        newValue = EditorGUILayout.IntSlider(formattedLabel, (int)value, (int)rangeAttribute.min, (int)rangeAttribute.max);
                    }
                    else if (type == typeof(float))
                    {
                        newValue = EditorGUILayout.Slider(formattedLabel, (float)value, rangeAttribute.min, rangeAttribute.max);
                    }
                }
                else if (customAttribute is FloatRangeSliderAttribute floatRangeSlider)
                {
                    return DrawFloatRangeSlider(formattedLabel, value, floatRangeSlider);
                }
            }
        }

        if (newValue == null)
        {
            if (type == typeof(int))
            {
                newValue = EditorGUILayout.IntField(formattedLabel, (int)value);
            }
            else if (type == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(formattedLabel, (float)value);
            }
            else if (type == typeof(string))
            {
                newValue = EditorGUILayout.TextField(formattedLabel, (string)value);
            }
            else if (type == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(formattedLabel, (bool)value);
            }
            else if (type == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(formattedLabel, (Vector3)value);
            }
            else if (type == typeof(Color))
            {
                newValue = EditorGUILayout.ColorField(formattedLabel, (Color)value);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                newValue = EditorGUILayout.ObjectField(formattedLabel, (UnityEngine.Object)value, type, true);
            }
            else if (type.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(formattedLabel, (Enum)value);
            }
        }

        if (newValue != null)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
            {
                ShowContextMenu(newValue, member.GetUnityObject());
                Event.current.Use();
            }

            return newValue;
        }
        else
        {
            EditorGUILayout.LabelField($"{formattedLabel}: {value} (Unsupported type)");
            return value;
        }
    }

    private object DrawArrayField(string label, object value, Type arrayType, UnityEngine.Object obj = null)
    {
        Type elementType = arrayType.GetElementType();
        var array = (Array)value;

        string formattedLabel = label.Split('>').Last(); // Remove parent label
        formattedLabel = FormatLabel(formattedLabel); // Apply Unity label formatting

        if (!labelFoldouts.ContainsKey(label))
        {
            labelFoldouts[label] = false; // Defaults array to folded
        }

        labelFoldouts[label] = DrawFoldoutWithContextMenu(labelFoldouts[label], formattedLabel, true, value, obj);
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
            object newElement = DrawField(new MemberInfoWrapper($"{formattedLabel} [{i}]", elementType, null, obj), element);

            if (!Equals(element, newElement))
            {
                array.SetValue(newElement, i);
            }
        }

        EditorGUI.indentLevel--;
        return array;
    }

    private object DrawListField(string label, object value, Type listType, UnityEngine.Object obj = null)
    {
        Type elementType = listType.GetGenericArguments()[0];
        var list = (System.Collections.IList)value;

        string formattedLabel = label.Split('>').Last(); // Remove parent label
        formattedLabel = FormatLabel(formattedLabel); // Apply Unity label formatting

        if (!labelFoldouts.ContainsKey(label))
        {
            labelFoldouts[label] = false; // Defaults list to folded
        }

        labelFoldouts[label] = DrawFoldoutWithContextMenu(labelFoldouts[label], formattedLabel, true, value, obj);
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
            object newElement = DrawField(new MemberInfoWrapper($"{formattedLabel} [{i}]", elementType, null, obj), element);

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

        DrawLabelWithContextMenu(label, value);

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
        EditorGUI.indentLevel--;

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

        return value;
    }

    private object DrawVector3Range(string label, object value, UnityEngine.Object obj = null)
    {
        Vector3Range range = (Vector3Range)value;
        Vector3 minValue = range.Min;
        Vector3 maxValue = range.Max;

        string formattedLabel = label.Split('>').Last();
        formattedLabel = FormatLabel(formattedLabel);
        DrawLabelWithContextMenu(formattedLabel, value, obj);
        
        EditorGUI.indentLevel++;
        minValue = EditorGUILayout.Vector3Field("Min", minValue);
        maxValue = EditorGUILayout.Vector3Field("Max", maxValue);
        EditorGUI.indentLevel--;

        maxValue.x = Mathf.Max(minValue.x, maxValue.x);
        maxValue.y = Mathf.Max(minValue.y, maxValue.y);
        maxValue.z = Mathf.Max(minValue.z, maxValue.z);

        value = new Vector3Range(minValue, maxValue);

        return value;
    }

    private object DrawDiceMeshData(string label, object value)
    {
        DiceMeshData diceMeshData = value as DiceMeshData;

        if (!labelFoldouts.ContainsKey(label))
        {
            labelFoldouts[label] = false; // Defaults mesh data to folded
        }

        labelFoldouts[label] = DrawFoldoutWithContextMenu(labelFoldouts[label], label, true, value);
        if (labelFoldouts[label])
        {
            EditorGUI.indentLevel++;
            diceMeshData.Mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", diceMeshData.Mesh, typeof(Mesh), false);
            DrawArrayField($"{label}>ValueToRotation", diceMeshData.ValueToRotation, typeof(Vector3[]));
            EditorGUI.indentLevel--;
        }

        return diceMeshData;
    }

    private void DrawLabelWithContextMenu(string label, object value, UnityEngine.Object obj = null)
    {
        string formattedLabel = FormatLabel(label);

        EditorGUILayout.LabelField(formattedLabel);
        Rect labelRect = GUILayoutUtility.GetLastRect();

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && labelRect.Contains(Event.current.mousePosition))
        {
            ShowContextMenu(value, obj);
            Event.current.Use();
        }
    }

    private bool DrawFoldoutWithContextMenu(bool foldout, string content, bool toggleOnLabelClick, object value, UnityEngine.Object obj = null)
    {
        string formattedContent = FormatLabel(content);

        bool foldoutState = EditorGUILayout.Foldout(foldout, formattedContent, toggleOnLabelClick);
        Rect foldoutRect = GUILayoutUtility.GetLastRect();

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && foldoutRect.Contains(Event.current.mousePosition))
        {
            ShowContextMenu(value, obj);
            Event.current.Use();
        }

        return foldoutState;
    }

    private void ShowContextMenu(object value, UnityEngine.Object obj)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Copy"), false, () => CopyObject(value));

        if (!string.IsNullOrEmpty(copiedDataJson))
        {
            menu.AddItem(new GUIContent("Paste"), false, () => PasteObject(value, obj));
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("Paste")); // Greyed-out option
        }

        menu.ShowAsContext();
    }

    private void CopyObject(object target)
    {
        if (target == null)
        {
            return;
        }

        if (target.GetType().IsPrimitive || target is string || target is Color)
        {
            Type type = target.GetType();
            Type wrapperType = typeof(SerializablePrimitive<>).MakeGenericType(type);
            object wrapperInstance = Activator.CreateInstance(wrapperType, target);
            wrapperType.GetField("value").SetValue(wrapperInstance, target);

            copiedDataJson = JsonUtility.ToJson(wrapperInstance);
        }
        else if (target is Array targetArray)
        {
            Type elementType = targetArray.GetType().GetElementType();
            Type wrapperType = typeof(SerializableArray<>).MakeGenericType(elementType);
            object wrapperInstance = Activator.CreateInstance(wrapperType);
            wrapperType.GetField("data").SetValue(wrapperInstance, target);

            copiedDataJson = JsonUtility.ToJson(wrapperInstance);
        }
        else
        {
            copiedDataJson = JsonUtility.ToJson(target);
        }

        Debug.Log($"Copying ({target.GetType()}): {copiedDataJson}");
    }

    private void PasteObject(object value, UnityEngine.Object obj = null)
    {
        if (value == null || string.IsNullOrEmpty(copiedDataJson))
        {
            return;
        }

        Debug.Log($"Pasting ({value.GetType()}): {copiedDataJson}");

        if (obj != null)
        {
            Undo.RecordObject(obj, $"Paste {obj.name} ({value.ToString()})");
        }
        else if (value is UnityEngine.Object uo)
        {
            Undo.RecordObject(uo, "Paste " + uo.name);
        }

        if (value is Single targetValue)
        {
            Type type = value.GetType();
            Type wrapperType = typeof(SerializablePrimitive<>).MakeGenericType(type);
            object wrapperInstance = JsonUtility.FromJson(copiedDataJson, wrapperType);

            FieldInfo valueField = wrapperType.GetField("value");
            if (valueField != null)
            {
                targetValue = (Single)valueField.GetValue(wrapperInstance);
            }
        }
        else if (value is Array targetArray)
        {
            Type elementType = targetArray.GetType().GetElementType();
            Type wrapperType = typeof(SerializableArray<>).MakeGenericType(elementType);
            object wrapperInstance = JsonUtility.FromJson(copiedDataJson, wrapperType);

            Array newArray = (Array)wrapperType.GetField("data").GetValue(wrapperInstance);
            if (newArray != null)
            {
                targetArray = newArray;
            }
        }
        else
        {
            JsonUtility.FromJsonOverwrite(copiedDataJson, value);
        }

        // Mark as dirty for Undo
        if (obj != null)
        {
            EditorUtility.SetDirty(obj);
            MethodInfo onValidate = obj.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (onValidate != null)
            {
                onValidate.Invoke(obj, null);
            }
        }
        else if (value is UnityEngine.Object uo)
        {
            EditorUtility.SetDirty(uo);
            MethodInfo onValidate = uo.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (onValidate != null)
            {
                onValidate.Invoke(uo, null);
            }
        }
    }

    private string FormatLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return label;
        }

        string formattedLabel = System.Text.RegularExpressions.Regex.Replace(label, "(\\B[A-Z])", " $1");
        return char.ToUpper(formattedLabel[0]) + formattedLabel.Substring(1);
    }
}
