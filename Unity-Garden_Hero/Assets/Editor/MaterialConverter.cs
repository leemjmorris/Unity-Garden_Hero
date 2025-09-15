using UnityEngine;
using UnityEditor;

public class MaterialConverter
{
    [MenuItem("Tools/Convert URP to Built-in")]
    public static void ConvertURPMaterials()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (material.shader.name.Contains("Universal Render Pipeline"))
            {
                if (material.shader.name.Contains("Lit"))
                    material.shader = Shader.Find("Standard");
                else if (material.shader.name.Contains("Unlit"))
                    material.shader = Shader.Find("Unlit/Texture");
                
                EditorUtility.SetDirty(material);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("Material conversion completed!");
    }
}