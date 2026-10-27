using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Crea Assets/Audio/Resources/AudioConfig.asset y vincula cada campo con su objeto de Wwise por GUID,
// leyendo Audio/Tools/wwise_objects.json (lo exporta Audio/Tools/wwise_build.py).
// Corre solo al abrir el editor si el json cambió; también desde Tools > Audio.
[InitializeOnLoad]
public static class AudioSetup
{
    private const string ConfigFolder = "Assets/Audio/Resources";
    private const string ConfigPath = ConfigFolder + "/AudioConfig.asset";
    private const string InitSettingsPath = "Assets/Wwise/ScriptableObjects/AkWwiseInitializationSettings.asset";
    private const string LastBindKey = "AudioSetup.LastBoundJsonTicks";

    private static string JsonPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audio", "Tools", "wwise_objects.json"));

    static AudioSetup()
    {
        EditorApplication.delayCall += BindIfJsonChanged;
    }

    private static void BindIfJsonChanged()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(JsonPath))
            return;

        long ticks = File.GetLastWriteTimeUtc(JsonPath).Ticks;

        if (AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath) != null &&
            EditorPrefs.GetString(LastBindKey, "") == ticks.ToString())
            return;

        if (Bind(log: true))
            EditorPrefs.SetString(LastBindKey, ticks.ToString());
    }

    [MenuItem("Tools/Audio/Vincular objetos de Wwise")]
    private static void BindMenu()
    {
        if (Bind(log: true))
            EditorPrefs.SetString(LastBindKey, File.GetLastWriteTimeUtc(JsonPath).Ticks.ToString());
    }

    [MenuItem("Tools/Audio/Seleccionar AudioConfig")]
    private static void SelectConfig()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
    }

    private static bool Bind(bool log)
    {
        if (!File.Exists(JsonPath))
        {
            Debug.LogError($"[AudioSetup] No existe {JsonPath}. Corré Audio/Tools/wwise_build.py con Wwise abierto.");
            return false;
        }

        WwiseObjects objects = JsonUtility.FromJson<WwiseObjects>(File.ReadAllText(JsonPath));
        AudioConfig config = LoadOrCreateConfig();

        var missing = new List<string>();
        int bound = 0;

        foreach (FieldInfo field in typeof(AudioConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            WwiseObjectReference reference = field.FieldType switch
            {
                Type t when t == typeof(AK.Wwise.Event) => Find(objects.Event, field.Name, WwiseObjectType.Event),
                Type t when t == typeof(AK.Wwise.Bank) => Find(objects.Soundbank, field.Name, WwiseObjectType.Soundbank),
                Type t when t == typeof(AK.Wwise.RTPC) => Find(objects.GameParameter, field.Name, WwiseObjectType.GameParameter),
                Type t when t == typeof(AK.Wwise.State) => FindState(objects, field.Name),
                _ => null
            };

            if (!typeof(AK.Wwise.BaseType).IsAssignableFrom(field.FieldType))
                continue;

            if (reference == null)
            {
                missing.Add(field.Name);
                continue;
            }

            var value = (AK.Wwise.BaseType)(field.GetValue(config) ?? Activator.CreateInstance(field.FieldType));
            value.ObjectReference = reference;
            field.SetValue(config, value);
            bound++;
        }

        if (config.wwiseInitializationSettings == null)
            config.wwiseInitializationSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(InitSettingsPath);

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        if (log)
        {
            Debug.Log($"[AudioSetup] AudioConfig: {bound} objetos de Wwise vinculados." +
                      (missing.Count > 0 ? $" Sin vincular: {string.Join(", ", missing)}" : ""), config);

            WarnMissingBanks(config);
        }

        return missing.Count == 0;
    }

    private static AudioConfig LoadOrCreateConfig()
    {
        AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);

        if (config != null)
            return config;

        Directory.CreateDirectory(ConfigFolder);
        config = ScriptableObject.CreateInstance<AudioConfig>();
        AssetDatabase.CreateAsset(config, ConfigPath);

        return config;
    }

    private static WwiseObjectReference Find(List<Entry> entries, string name, WwiseObjectType type)
    {
        Entry entry = entries.Find(e => e.name == name);
        return entry == null ? null : WwiseObjectReference.FindOrCreateWwiseObject(type, name, new Guid(entry.guid));
    }

    // Campo "Grupo_State" -> State "State" del grupo "Grupo"
    private static WwiseObjectReference FindState(WwiseObjects objects, string fieldName)
    {
        int split = fieldName.IndexOf('_');

        if (split <= 0)
            return null;

        string group = fieldName[..split];
        string state = fieldName[(split + 1)..];

        Entry stateEntry = objects.State.Find(e => e.name == $"{group}/{state}");
        Entry groupEntry = objects.StateGroup.Find(e => e.name == group);

        if (stateEntry == null || groupEntry == null)
            return null;

        var reference = (WwiseStateReference)WwiseObjectReference.FindOrCreateWwiseObject(
            WwiseObjectType.State, state, new Guid(stateEntry.guid));
        reference.SetupGroupObjectReference(group, new Guid(groupEntry.guid));

        return reference;
    }

    // Los bancos se resuelven contra los assets que crea el importador de Addressables al importar los .bnk
    private static void WarnMissingBanks(AudioConfig config)
    {
        foreach (FieldInfo field in typeof(AudioConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetValue(config) is AK.Wwise.Bank bank && !bank.IsValid())
            {
                Debug.LogWarning($"[AudioSetup] El banco {field.Name} todavía no tiene su asset de Addressables " +
                                 "(Assets/WwiseData/Bank). Generá los SoundBanks y volvé a correr Tools > Audio > Vincular objetos de Wwise.");
            }
        }
    }

    [Serializable]
    private sealed class Entry
    {
        public string name;
        public string guid;
    }

    [Serializable]
    private sealed class WwiseObjects
    {
        public List<Entry> Event = new();
        public List<Entry> Soundbank = new();
        public List<Entry> State = new();
        public List<Entry> StateGroup = new();
        public List<Entry> GameParameter = new();
    }
}
