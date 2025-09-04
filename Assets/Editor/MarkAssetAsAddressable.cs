using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.IO;

public class MarkAssetsAsAddressables
{
    [MenuItem("Tools/Addressables/Mark FBX + Audio as Addressables")]
    public static void MarkAllAssets()
    {
        string[] audioExtensions = new[] { ".wav", ".ogg" };
        string[] foldersToScan = new[]
        {
            "Assets/Resources_moved/Characters/",
            "Assets/Resources_moved/Audio/male/",
            "Assets/Resources_moved/Audio/female/"
        };

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings not found. Create them via 'Window → Asset Management → Addressables → Groups'");
            return;
        }

        var group = settings.DefaultGroup;
        int totalMarked = 0;

        foreach (string folder in foldersToScan)
        {
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"Folder not found: {folder}");
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { folder });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string extension = Path.GetExtension(assetPath).ToLower();

                if (assetPath.EndsWith(".meta")) continue;

                bool isFbx = extension == ".fbx";
                bool isAudio = System.Array.Exists(audioExtensions, ext => ext == extension);

                if (!isFbx && !isAudio) continue;

                string relativePath = assetPath.Replace("Assets/Resources_moved/", "").Replace("\\", "/");
                string addressKey = relativePath.Substring(0, relativePath.LastIndexOf('.')); // Remove extension

                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = addressKey;

                totalMarked++;
            }
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"✔ Marked {totalMarked} assets (FBX + Audio) as Addressables.");
    }
}
