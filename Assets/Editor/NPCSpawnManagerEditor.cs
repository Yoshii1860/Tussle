using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(NPCSpawnerManager))]
public class NPCSpawnerManagerEditor : Editor
{
    private Dictionary<string, bool> sectionFoldouts = new();
    private Dictionary<string, bool> subSectionFoldouts = new();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "fixedSpawns");

        var fixedSpawnsProp = serializedObject.FindProperty("fixedSpawns");
        // Group by section and subsection
        var sectionGroups = new Dictionary<string, Dictionary<string, List<int>>>();

        for (int i = 0; i < fixedSpawnsProp.arraySize; i++)
        {
            var element = fixedSpawnsProp.GetArrayElementAtIndex(i);
            string section = element.FindPropertyRelative("sectionName")?.stringValue ?? "Default";
            string subSection = element.FindPropertyRelative("subSectionName")?.stringValue ?? "Default";

            if (!sectionGroups.ContainsKey(section))
                sectionGroups[section] = new Dictionary<string, List<int>>();
            if (!sectionGroups[section].ContainsKey(subSection))
                sectionGroups[section][subSection] = new List<int>();
            sectionGroups[section][subSection].Add(i);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fixed Spawns", EditorStyles.boldLabel);

        foreach (var sectionKvp in sectionGroups)
        {
            if (!sectionFoldouts.ContainsKey(sectionKvp.Key))
                sectionFoldouts[sectionKvp.Key] = true;

            sectionFoldouts[sectionKvp.Key] = EditorGUILayout.Foldout(sectionFoldouts[sectionKvp.Key], sectionKvp.Key, true);
            if (sectionFoldouts[sectionKvp.Key])
            {
                EditorGUI.indentLevel++;
                foreach (var subSectionKvp in sectionKvp.Value)
                {
                    string subKey = sectionKvp.Key + "/" + subSectionKvp.Key;
                    if (!subSectionFoldouts.ContainsKey(subKey))
                        subSectionFoldouts[subKey] = true;

                    subSectionFoldouts[subKey] = EditorGUILayout.Foldout(subSectionFoldouts[subKey], subSectionKvp.Key, true);
                    if (subSectionFoldouts[subKey])
                    {
                        EditorGUI.indentLevel++;
                        foreach (int idx in subSectionKvp.Value)
                        {
                            EditorGUILayout.PropertyField(fixedSpawnsProp.GetArrayElementAtIndex(idx), new GUIContent($"Spawn {idx}"), true);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}