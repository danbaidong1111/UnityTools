using UnityEngine;


using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;


#if UNITY_EDITOR
using UnityEditor;
public class RGBACombineTool : EditorWindow
{
    [MenuItem("Tools/RGBA图层混合工具")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(RGBACombineTool));//显示现有窗口实例。如果没有，请创建一个。
    }
    TextureToCombine textureRToConbine = new TextureToCombine();
    TextureToCombine textureGToConbine = new TextureToCombine();
    TextureToCombine textureBToConbine = new TextureToCombine();
    TextureToCombine textureAToConbine = new TextureToCombine();
    //Texture2D textureR;
    //Texture2D textureG;
    //Texture2D textureB;
    //Texture2D textureA;
    Texture2D textureOut;

    public enum TextureToCombineChannel
    {
        R, G, B, A
    }

    class TextureToCombine
    {
        public Texture2D texture;
        public TextureToCombineChannel channel = TextureToCombineChannel.R;
        public bool revert = false;
        public bool IsAlready()
        {
            return texture != null;
        }

        public float GetTexOneChannelPixel(int x, int y)
        {
            float resultVal = 0;
            switch (channel)
            {
                case TextureToCombineChannel.R:
                    resultVal = texture.GetPixel(x, y).r;
                    break;
                case TextureToCombineChannel.G:
                    resultVal = texture.GetPixel(x, y).g;
                    break;
                case TextureToCombineChannel.B:
                    resultVal = texture.GetPixel(x, y).b;
                    break;
                case TextureToCombineChannel.A:
                    resultVal = texture.GetPixel(x, y).a;
                    break;
                default:
                    resultVal = texture.GetPixel(x, y).r;
                    break;
            }
            
            return revert? 1 - resultVal : resultVal;
        }
    }

    void OnGUI()
    {

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("R:", EditorStyles.boldLabel);
        textureRToConbine.texture = EditorGUILayout.ObjectField(textureRToConbine.texture, typeof(Texture2D), false, GUILayout.Width(300)) as Texture2D;
        textureRToConbine.channel = (TextureToCombineChannel)EditorGUILayout.EnumPopup(textureRToConbine.channel, GUILayout.Width(50));
        GUILayout.Label("revert", EditorStyles.label);  textureRToConbine.revert = EditorGUILayout.Toggle(textureRToConbine.revert);
        EditorGUILayout.EndHorizontal(); 

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("G:", EditorStyles.boldLabel);
        textureGToConbine.texture = EditorGUILayout.ObjectField(textureGToConbine.texture, typeof(Texture2D), false, GUILayout.Width(300)) as Texture2D;
        textureGToConbine.channel = (TextureToCombineChannel)EditorGUILayout.EnumPopup(textureGToConbine.channel, GUILayout.Width(50));
        GUILayout.Label("revert", EditorStyles.label); textureGToConbine.revert = EditorGUILayout.Toggle(textureGToConbine.revert);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("B:", EditorStyles.boldLabel);
        textureBToConbine.texture = EditorGUILayout.ObjectField(textureBToConbine.texture, typeof(Texture2D), false, GUILayout.Width(300)) as Texture2D;
        textureBToConbine.channel = (TextureToCombineChannel)EditorGUILayout.EnumPopup(textureBToConbine.channel, GUILayout.Width(50));
        GUILayout.Label("revert", EditorStyles.label); textureBToConbine.revert = EditorGUILayout.Toggle(textureBToConbine.revert);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("A:", EditorStyles.boldLabel);
        textureAToConbine.texture = EditorGUILayout.ObjectField(textureAToConbine.texture, typeof(Texture2D), false, GUILayout.Width(300)) as Texture2D;
        textureAToConbine.channel = (TextureToCombineChannel)EditorGUILayout.EnumPopup(textureAToConbine.channel, GUILayout.Width(50));
        GUILayout.Label("revert", EditorStyles.label); textureAToConbine.revert = EditorGUILayout.Toggle(textureAToConbine.revert);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Name: ", EditorStyles.boldLabel);
        string outTextureName = "outTexture";
        outTextureName = EditorGUILayout.TextArea(outTextureName);
        GUILayout.Space(10);
        if (GUILayout.Button("Generate"))
        {
            string path = EditorUtility.SaveFolderPanel("Select an output path", "Assets", "");
            CombineRGBA(path, outTextureName);
        }
    }

    void CombineRGBA(string path, string name)
    {
        if(textureRToConbine.IsAlready() == false ||
            textureGToConbine.IsAlready() == false ||
            textureBToConbine.IsAlready() == false ||
            textureAToConbine.IsAlready() == false)
        {
            Debug.LogError("Combine Failed:Null Texture");
            return;
        }
        int width = textureRToConbine.texture.width;
        int height = textureRToConbine.texture.height;
        if (textureGToConbine.texture.width != width || textureBToConbine.texture.width != width || textureAToConbine.texture.width != width)
        {
            Debug.LogError("Combine Failed:Texture width not consistent");
            return;
        }
        if (textureGToConbine.texture.width != height || textureBToConbine.texture.width != height || textureAToConbine.texture.width != height)
        {
            Debug.LogError("Combine Failed:Texture height not consistent");
            return;
        }

        textureOut = new Texture2D(width, height, TextureFormat.ARGB32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = Color.white;
                color.r = textureRToConbine.GetTexOneChannelPixel(x, y);
                color.g = textureGToConbine.GetTexOneChannelPixel(x, y);
                color.b = textureBToConbine.GetTexOneChannelPixel(x, y);
                color.a = textureAToConbine.GetTexOneChannelPixel(x, y);
                textureOut.SetPixel(x, y, color);
            }
        }
        textureOut.Apply();
        
        byte[] pngData = textureOut.EncodeToPNG();
        File.WriteAllBytes(path + "/" + name + ".png", pngData);
        AssetDatabase.Refresh();

    }

}


#endif