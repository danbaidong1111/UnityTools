using UnityEngine;
using System.IO;
using Unity.Mathematics;
using System;
using System.Diagnostics;


#if UNITY_EDITOR
using UnityEditor;
public class GenerateSDFTool : EditorWindow
{
    private SerializedObject serializedObject;
    private SerializedProperty textureArrayProperty;
    private SerializedProperty baseTexProperty;
    private SerializedProperty texToGenerateProperty;
    class TextureToCombine
    {
        public Texture2D texture;
        public TextureToCombineChannel channel = TextureToCombineChannel.R;

        public TextureToCombine(TextureToCombineChannel c)
        {
            channel = c;
        }
        public bool IsAlready()
        {
            return texture != null;
        }

        public float GetTexOneChannelPixel(int x, int y)
        {
            switch (channel)
            {
                case TextureToCombineChannel.R:
                    return texture.GetPixel(x, y).r;
                //break;
                case TextureToCombineChannel.G:
                    return texture.GetPixel(x, y).g;
                //break;
                case TextureToCombineChannel.B:
                    return texture.GetPixel(x, y).b;
                //break;   
                case TextureToCombineChannel.A:
                    return texture.GetPixel(x, y).a;
                //break;
                default:
                    return texture.GetPixel(x, y).r;
            }

        }
    }

    //TextureToCombine m_BaseTex = new TextureToCombine(TextureToCombineChannel.R);

    public Texture2D m_BaseTex;

    public Texture2D[] m_TexturesToGenerate;
    public Texture2D[] m_TexturesToLerp;

    private Texture2D m_TextureOut;
    private ComputeShader m_SDFComputeShader;
    private static GUID m_SDFComputeGUID = new GUID("8f34a9b3d57226d47ae8b84ba3581fd6");
    private bool isCalculating = false;
    public string outTexPath = "Assets/SDFGenerate/";
    public string outTexName = "outTexName";

    private int m_TexWidth;
    private int m_TexHeight;
    public enum TextureToCombineChannel
    {
        R, G, B, A
    }

    [MenuItem("Tools/GenerateSDFTool")]
    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow(typeof(GenerateSDFTool));//显示现有窗口实例。如果没有，请创建一个。
        window.position = new Rect(800, 300, 600, 800);
    }

    private void OnEnable()
    {
        // 创建SerializedObject以便编辑
        serializedObject = new SerializedObject(this);
        // 获取Texture2D数组的SerializedProperty
        texToGenerateProperty = serializedObject.FindProperty("m_TexturesToGenerate");
        textureArrayProperty = serializedObject.FindProperty("m_TexturesToLerp");
    }


    void OnGUI()
    {
        serializedObject.Update();

        GUILayout.Space(10);


        GUILayout.BeginHorizontal();
        FindSDFCompute(ref m_SDFComputeShader);
        GUILayout.EndHorizontal();
        
        #region TexArray
        GUILayout.Space(10);
        EditorGUILayout.PropertyField(texToGenerateProperty);
        if (m_TexturesToGenerate != null && m_TexturesToGenerate.Length > 0)
        {
            m_TexWidth = m_TexturesToGenerate[0].width;
            m_TexHeight = m_TexturesToGenerate[0].height;

            bool check = CheckTexArraySizeUniform(m_TexturesToGenerate);

            if (check)
            {
                

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                for (int i = 0; i < m_TexturesToGenerate.Length; i++)
                {
                    GUILayout.Label(m_TexturesToGenerate[i], GUILayout.Width(100), GUILayout.Height(100));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }
        #endregion



        #region SaveFileGUI
        GUILayout.Space(10);

        Rect rect = EditorGUILayout.GetControlRect(false, 10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("SavePath: ", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.TextArea(outTexPath, GUILayout.Width(300));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("OutName: ", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.TextArea(outTexName, GUILayout.Width(300));
        GUILayout.EndHorizontal();
        
        rect.width = 100;
        rect.height = 40;
        rect.y += 10;
        rect.x += 450;
        
        
        if (GUI.Button(rect, "Select Path"))
        {
            string selectedPath = EditorUtility.SaveFolderPanel("Select an output path", "Assets", "defaultFold");
            if (!String.IsNullOrEmpty(selectedPath))
            {
                selectedPath = ConvertToRelativePath(selectedPath);
                outTexPath = selectedPath + "/";
            }
        }
        #endregion


        GUILayout.Space(10);
        if (GUILayout.Button("Generate"))
        {
            //GUILayout.Label("SetTexArrayTexturesImporterSettings");
            //SetTexArrayTexturesImporterSettings(m_TexturesToGenerate);

            // CPU Generate
            //Grid grid1 = new Grid(baseTex.texture.width, baseTex.texture.height);
            //Grid grid2 = new Grid(baseTex.texture.width, baseTex.texture.height);
            //LoadImage(ref grid1, ref grid2, ref baseTex.texture);
            //GenerateSDF(ref grid1);
            //GenerateSDF(ref grid2);
            //SaveImage(outTexPath, ref grid1, ref grid2, ref baseTex.texture);

            // GPU Generate
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(m_TexWidth, m_TexHeight, RenderTextureFormat.R16);
            rtd.enableRandomWrite = true;
            rtd.useMipMap = false;
            RenderTexture tempRT = RenderTexture.GetTemporary(rtd);
            tempRT.filterMode = FilterMode.Point;
            tempRT.Create();

            for(int i = 0; i < m_TexturesToGenerate.Length; i++)
            {
                Texture2D curTex = m_TexturesToGenerate[i];

                if (GenerateSDFWithComputeShader(m_SDFComputeShader, tempRT, curTex))
                {
                    SaveImageFromGPU(outTexPath, outTexName + "_SDF" + i + ".png", m_TexWidth, m_TexHeight, tempRT);
                }

            }
            AssetDatabase.Refresh();

            tempRT.Release();
        }


        #region LerpTextureGUI

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Lerp SDF Textures", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(textureArrayProperty);
        serializedObject.ApplyModifiedProperties();

        if(m_TexturesToLerp != null && m_TexturesToLerp.Length > 0)
        {
            bool check = CheckTexArraySizeUniform(m_TexturesToLerp);
            if (check)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                for (int i = 0; i < m_TexturesToLerp.Length; i++)
                {
                    GUILayout.Label(m_TexturesToLerp[i], GUILayout.Width(100), GUILayout.Height(100));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

        }

        if (GUILayout.Button("Generate Lerp"))
        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(m_TexWidth, m_TexHeight, RenderTextureFormat.R16);
            rtd.enableRandomWrite = true;
            rtd.useMipMap = false;
            RenderTexture tempRT = RenderTexture.GetTemporary(rtd);
            tempRT.filterMode = FilterMode.Point;
            tempRT.Create();
            if (GenerateLerpTexturesWithComputeShader(m_SDFComputeShader, tempRT, m_TexturesToLerp))
            {
                SaveImageFromGPU(outTexPath, outTexName + "_Lerp.png", m_TexWidth, m_TexHeight, tempRT);
                AssetDatabase.Refresh();
            }

            tempRT.Release();
        }

        #endregion
    }

    private bool CheckTexArraySizeUniform(Texture2D[] texArray)
    {
        if (texArray == null || !(texArray.Length > 0))
        {
            UnityEngine.Debug.LogError("TexArray null");
            return false;
        }

        int texWidth;
        int texHeight;
        texWidth = texArray[0].width;
        texHeight = texArray[0].height;
        GUIStyle fontStyle = new GUIStyle(EditorStyles.boldLabel);
        fontStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

        for (int i = 0; i < texArray.Length; i++)
        {
            if (texArray[i] == null)
            {
                GUILayout.Label("Array has null element at index: " + i, fontStyle);
                UnityEngine.Debug.LogError("Array has null element at index: " + i);
                return false;
            }
            if (texWidth != texArray[i].width || texHeight != texArray[i].height)
            {
                fontStyle.normal.textColor = Color.red;
                GUILayout.Label("TexArray has un uniform size texture at index: " + i, fontStyle);
                UnityEngine.Debug.LogError("TexArray has un uniform size texture at index: " + i);
                return false;
            }
            
        }


        GUILayout.Label("width:" + m_TexWidth + ", Height:" + m_TexHeight, fontStyle);

        return true;
    }

    private void SetTexArrayTexturesImporterSettings(Texture2D[] texArray)
    {
        for (int i = 0; i < texArray.Length; i++)
        {
            SetTextureImporterSettings(AssetDatabase.GetAssetPath(texArray[i]), TextureImporterCompression.Uncompressed, false, true, false);
        }
    }
    private string ConvertToRelativePath(string absolutePath)
    {
        string relativePath = absolutePath;

        if (absolutePath.StartsWith(Application.dataPath))
        {
            relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }

        return relativePath;
    }

    private void SetTextureImporterSettings(string path, TextureImporterCompression compressFormat, bool sRGBTexture, bool isReadable, bool mipmapEnabled)
    {
        AssetDatabase.Refresh();
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            if(importer.textureCompression == compressFormat
                && importer.sRGBTexture == sRGBTexture
                && importer.isReadable == isReadable
                && importer.mipmapEnabled == mipmapEnabled)
            {
                return;
            }
            importer.textureCompression = compressFormat;
            importer.sRGBTexture = sRGBTexture;
            importer.isReadable = isReadable;
            importer.mipmapEnabled = mipmapEnabled;
            UnityEngine.Debug.Log("Reset importer setting:" + path);
        }
        else
        {
            UnityEngine.Debug.LogError("Reset importer Filed:" + path);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    #region CPU Generate SDF
    class Point
    {
        public int dx;
        public int dy;
        public Point(int dxx, int dyy)
        {
            dx = dxx;
            dy = dyy;
        }
        public int DistSq()
        {
            return dx * dx + dy * dy;
        }

        public static Point inside
        {
            get
            {
                return new Point(0, 0);
            }
        }
        public static Point empty
        {
            get
            {
                return new Point(9999, 9999);
            }
        }
    }

    class Grid
    {
        Point[] points;

        public int width = -1;
        public int height = -1;
        public Grid(int width, int height)
        {
            this.width = width;
            this.height = height;
            points = new Point[width * height];
        }

        public void Set(int x, int y, Point p)
        {
            if (width == -1 || height == -1)
            {
                UnityEngine.Debug.LogError(width + "" + height);
            }

            points[x + width * y] = p;
        }
        public Point Get(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                return points[x + width * y];
            }
            else
            {
                return Point.empty;
            }
        }
    }

    void Compare(ref Grid g, ref Point p, int x, int y, int offsetx, int offsety)
    {
        Point other = g.Get(x + offsetx, y + offsety);
        other.dx += offsetx;
        other.dy += offsety;

        if (other.DistSq() < p.DistSq())
        {
            p.dx = other.dx;
            p.dy = other.dy;
        }
    }
    void LoadImage(ref Grid grid1, ref Grid grid2, ref Texture2D tex)
    {
        int imageWidth = tex.width;
        int imageHeight = tex.height;
        for (int y = 0; y < imageHeight; ++y)
        {
            for (int x = 0; x < imageWidth; ++x)
            {
                Color pixelCol = tex.GetPixel(x, y);
                if(pixelCol.r < 0.5f)
                {
                    grid1.Set(x, y, Point.inside);
                    grid2.Set(x, y, Point.empty);
                }
                else
                {
                    grid2.Set(x, y, Point.inside);
                    grid1.Set(x, y, Point.empty);
                }
            }

        }

    }

    void SaveImage(string path, ref Grid grid1, ref Grid grid2, ref Texture2D tex)
    {
        int width = tex.width;
        int height = tex.height;

        m_TextureOut = new Texture2D(width, height, TextureFormat.ARGB32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // calculate the actual distance from the dx/dy
                Point p1 = grid1.Get(x, y);
                Point p2 = grid2.Get(x, y);
                float distSq1 = p1.DistSq();
                float distSq2 = p2.DistSq();

                float dist1 = Mathf.Sqrt(distSq1);
                float dist2 = Mathf.Sqrt(distSq2);
                float dist = dist1 - dist2;

                float c = (dist * 3.0f + 128.0f) / 255.0f;
                //float c = (dist1 * 5 + 128.0f) / 255.0f;

                Color color = Color.black;
                color.r = c;
                m_TextureOut.SetPixel(x, y, color);
            }
        }
        m_TextureOut.Apply();

        byte[] pngData = m_TextureOut.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

    }


    void GenerateSDF(ref Grid g)
    {
        int imageWidth = g.width;
        int imageHeight = g.height;
        // Pass 0
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                Point p = g.Get(x, y);
                Compare(ref g, ref p, x, y, -1, 0);
                Compare(ref g, ref p, x, y, 0, -1);
                Compare(ref g, ref p, x, y, -1, -1);
                Compare(ref g, ref p, x, y, 1, -1);
                g.Set(x, y, p);
            }

            for (int x = imageWidth - 1; x >= 0; x--)
            {
                Point p = g.Get(x, y);
                Compare(ref g, ref p, x, y, 1, 0);
                g.Set(x, y, p);
            }
        }

        ////Pass 1
        //for (int y = imageHeight - 1; y >= 0; y--)
        //{
        //    for (int x = imageWidth - 1; x >= 0; x--)
        //    {
        //        Point p = g.Get(x, y);
        //        Compare(ref g, ref p, x, y, 1, 0);
        //        Compare(ref g, ref p, x, y, 0, 1);
        //        Compare(ref g, ref p, x, y, -1, 1);
        //        Compare(ref g, ref p, x, y, 1, 1);
        //        g.Set(x, y, p);
        //    }

        //    for (int x = 0; x < imageWidth; x++)
        //    {
        //        Point p = g.Get(x, y);
        //        Compare(ref g, ref p, x, y, -1, 0);
        //        g.Set(x, y, p);
        //    }
        //}
    }

    #endregion


    #region GPU GenerateSDF
    void FindSDFCompute(ref ComputeShader cs)
    {
        if (cs == null)
        {
            cs = (ComputeShader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_SDFComputeGUID), typeof(ComputeShader));
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Compute: ", EditorStyles.boldLabel, GUILayout.Width(80));

        cs = (ComputeShader)EditorGUILayout.ObjectField(cs, typeof(ComputeShader), false);
        EditorGUILayout.EndHorizontal();
    }
    private bool GenerateSDFWithComputeShader(ComputeShader cs, RenderTexture rt, Texture2D source)
    {
        if (cs == null)
        {
            UnityEngine.Debug.LogError("compute null"); 
            return false;
        }
            
        if (rt == null)
        {
            UnityEngine.Debug.LogError("compute rendertexture null");
            return false;
        }
             
        if (rt.enableRandomWrite == false)
        {
            UnityEngine.Debug.LogError("compute rendertexture should enableRandomWrite");
            return false;
        }
        Stopwatch  stopwatch = new Stopwatch();
        stopwatch.Start();
        long lastTimeStamp = 0;

        int generateKernel = cs.FindKernel("CSGenerage");
        int downSampleGenerateKernel = cs.FindKernel("CSDownSampleGenerate");

        // DownSample------------------------------
        //int downSampleScale = 2;
        //int downWidth = rt.width / downSampleScale;
        //int downHeight = rt.height / downSampleScale;
        //ComputeBuffer downSampleForeBackBuffer = new ComputeBuffer(downWidth * downHeight, sizeof(float) * 2);

        //cs.SetVector    ("_DownRTSize", new Vector4(downWidth, downHeight, 0, 0));
        //cs.SetBuffer    (downSampleGenerateKernel, "_DownSampleResultBuffer", downSampleForeBackBuffer);
        //cs.SetTexture   (downSampleGenerateKernel, "_SourceTexture", source);
        //cs.Dispatch     (downSampleGenerateKernel, downWidth / 8, downHeight / 8, 1);
        //stopwatch.Stop();
        //UnityEngine.Debug.Log("Dispatch downSample: " + (stopwatch.ElapsedMilliseconds - lastTimeStamp) + " 毫秒");

        //stopwatch.Start();
        //ComputeBuffer foreBackBuffer = new ComputeBuffer(rt.width * rt.height, sizeof(float) * 2);
        //cs.SetVector("_RTSize", new Vector4(rt.width, rt.height, 0, 0));
        //cs.SetBuffer(generateKernel, "_ResultBuffer", foreBackBuffer);
        //cs.SetBuffer(generateKernel, "_DownSampleResultBuffer", downSampleForeBackBuffer);
        //cs.SetTexture(generateKernel, "_SourceTexture", source);
        //cs.Dispatch(generateKernel, rt.width / 8, rt.height / 8, 1);


        //float2[] array = new float2[rt.width * rt.height];
        //float2 maxDist = 0;
        //foreBackBuffer.GetData(array);
        //stopwatch.Stop();
        //UnityEngine.Debug.Log("Dispatch generateKernel: " + (stopwatch.ElapsedMilliseconds - lastTimeStamp) + " 毫秒");


        //foreach (float2 f in array)
        //{
        //    maxDist.x = f.x > maxDist.x ? f.x : maxDist.x;
        //    maxDist.y = f.y > maxDist.y ? f.y : maxDist.y;
        //}

        //lastTimeStamp = stopwatch.ElapsedMilliseconds;
        //stopwatch.Start();
        //int saveKernel = cs.FindKernel("CSSave");
        //cs.SetVector("_MaxDistance", new Vector4(maxDist.x, maxDist.y, 0, 0));
        //cs.SetBuffer(saveKernel, "_ResultBuffer", foreBackBuffer);
        //cs.SetTexture(saveKernel, "_Result", rt);
        //cs.Dispatch(saveKernel, rt.width / 8, rt.height / 8, 1);

        //foreBackBuffer?.Release();

        //downSampleForeBackBuffer?.Release();
        // DownSample------------------------------


        ComputeBuffer foreBackBuffer = new ComputeBuffer(rt.width * rt.height, sizeof(float) * 2);
        cs.SetVector("_RTSize", new Vector4(rt.width, rt.height, 0, 0));
        cs.SetBuffer(generateKernel, "_ResultBuffer", foreBackBuffer);
        cs.SetTexture(generateKernel, "_SourceTexture", source);
        cs.Dispatch(generateKernel, rt.width / 8, rt.height / 8, 1);


        float2[] array = new float2[rt.width * rt.height];
        float2 maxDist = 0;
        foreBackBuffer.GetData(array);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Dispatch generateKernel: " + (stopwatch.ElapsedMilliseconds - lastTimeStamp) + " 毫秒");


        foreach (float2 f in array)
        {
            maxDist.x = f.x > maxDist.x ? f.x : maxDist.x;
            maxDist.y = f.y > maxDist.y ? f.y : maxDist.y;
        }

        lastTimeStamp = stopwatch.ElapsedMilliseconds;
        stopwatch.Start();
        int saveKernel = cs.FindKernel("CSSave");
        cs.SetVector("_MaxDistance", new Vector4(maxDist.x, maxDist.y, 0, 0));
        cs.SetBuffer(saveKernel, "_ResultBuffer", foreBackBuffer);
        cs.SetTexture(saveKernel, "_Result", rt);
        cs.Dispatch(saveKernel, rt.width / 8, rt.height / 8, 1);

        foreBackBuffer?.Release();


        UnityEngine.Debug.Log("Dispatch saveKernel: " + (stopwatch.ElapsedMilliseconds - lastTimeStamp) + " 毫秒");
        return true;
    }

    private void SaveImageFromGPU(string path, string fileName, int width, int height, RenderTexture rt)
    {
        path = path + fileName;
        // TODO : Use AsyncGPUReadback.RequestIntoNativeArray to copy a texture from the GPU to the CPU.
        m_TextureOut = new Texture2D(width, height, TextureFormat.R16, false);
        Rect regionToReadFrom = new Rect(0, 0, width, height);
        int xPosToWriteTo = 0;
        int yPosToWriteTo = 0;
        bool updateMipmaps = false;

        RenderTexture.active = rt;
        m_TextureOut.ReadPixels(regionToReadFrom, xPosToWriteTo,yPosToWriteTo, updateMipmaps);
        m_TextureOut.Apply();

        byte[] pngData = m_TextureOut.EncodeToPNG();
        File.WriteAllBytes(path, pngData);

        //SetTextureImporterSettings(path, TextureImporterCompression.Uncompressed, false, true, false);

        UnityEngine.Debug.Log("Save texture at " + path);
    }

    #endregion



    private bool GenerateLerpTexturesWithComputeShader(ComputeShader cs, RenderTexture rt, Texture2D[] sources)
    {
        if (cs == null)
        {
            UnityEngine.Debug.LogError("compute null");
            return false;
        }

        if (rt == null)
        {
            UnityEngine.Debug.LogError("compute rendertexture null");
            return false;
        }

        if (rt.enableRandomWrite == false)
        {
            UnityEngine.Debug.LogError("compute rendertexture should enableRandomWrite");
            return false;
        }

        if (sources.Length < 2)
        {
            UnityEngine.Debug.LogError("At least two Textures in lerp Array");
            return false;
        }


        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();


        // Compute dispatch
        {
            int sourcesSize = sources.Length;
            int lerpKernel = cs.FindKernel("CSLerp");
            Texture2DArray texture2DArray = new Texture2DArray(sources[0].width, sources[0].height, sourcesSize, sources[0].format, true);
            for (int i = 0; i < sourcesSize; i++)
            {
                Graphics.CopyTexture(sources[i], 0, 0, texture2DArray, i, 0);
            }
            texture2DArray.Apply();
            cs.SetTexture(lerpKernel, "_LerpTexArray", texture2DArray);

            cs.SetInt("_LerpTexArraySize", sourcesSize);
            cs.SetTexture(lerpKernel, "_Result", rt);
            cs.Dispatch(lerpKernel, rt.width / 8, rt.height / 8, 1);
        }


        stopwatch.Stop();
        UnityEngine.Debug.Log("Dispatch lerpKernel: " + stopwatch.ElapsedMilliseconds + " 毫秒");

        return true;
    }



}


#endif