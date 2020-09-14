using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor.Rendering;
using System.Text.RegularExpressions;

namespace ShaderVariantCollectionFilter
{
    public class ShaderVariantCollectionPreprocessShaders : IPreprocessShaders
    {
        static ShaderVariantCollection shaderVariantCollection;
        static Dictionary<string, ShaderVariantCollectionItem> m_allshaderVariantDict;
        /// <summary>
        /// 测试数据输出目录
        /// </summary>
        public static string OutputDir
        {
            get
            {
                return Application.dataPath + "/../ShaderVariantCollection/";
            }
        }
        /// <summary>
        /// 变体收集器工程路径
        /// </summary>
        public static string ShaderVariantCollectionPath
        {
            get
            {
                return "Assets/ArtEdit/Editor/AllResources/Shaders/Resources/AllShaderVariants.shadervariants";
            }
        }

        public static void Clear()
        {
            shaderVariantCollection = null;
            m_allshaderVariantDict = null;
        }

        [MenuItem("UGameTools/DTool/ShaderVariantCollectionFilter", false, 52)]
        public static void ShaderVariantCollectionFilter()
        {
            string path = "Assets/ArtEdit/Editor/AllResources/Shaders/Resources/AllShaderVariants.shadervariants";

            ShaderVariantCollection shaderVariantCollection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
            if (shaderVariantCollection == null)
            {
                return;
            }
            ShaderVariantCollectionPreprocessShaders variantsGrabber = new ShaderVariantCollectionPreprocessShaders();
            m_allshaderVariantDict = null;
            variantsGrabber.Pack(shaderVariantCollection);
        }

        public void Pack(ShaderVariantCollection svc = null)
        {
            Object[] selectObs = new Object[] { svc };
            if (null != selectObs && selectObs.Length > 0)
            {
                AssetBundleBuild[] abb = new AssetBundleBuild[selectObs.Length];
                for (int i = 0; i < selectObs.Length; i++)
                {
                    Object selectOb = selectObs[i];
                    string path = AssetDatabase.GetAssetPath(selectOb);

                    string[] files = new string[1] { path };

                    string abName = selectOb.name;

                    AssetBundleBuild tmp = new AssetBundleBuild() { assetNames = files, assetBundleName = abName };
                    abb[i] = tmp;

                }

                if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);
                BuildPipeline.BuildAssetBundles(OutputDir, abb, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
            }

        }

        public int callbackOrder { get { return 0; } }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            ProcessFormalSVC(shader, snippet, data);
        }

        static Dictionary<string, ShaderVariantCollectionItem> GetShaderVariantList(ShaderVariantCollection svc, Dictionary<string, ShaderVariantCollectionItem> dict)
        {
            if (svc == null)
            {
                return null;
            }
            if (dict == null)
            {
                return null;
            }

            SerializedObject serializedObject = new SerializedObject(svc);
            SerializedProperty m_shadersProperty = serializedObject.FindProperty("m_Shaders");
            if (m_shadersProperty == null)
            {
                return null;
            }
            Shader shader = null;
            string keywords = string.Empty;
            PassType passType = PassType.Normal;
            ShaderVariantCollectionItem shaderVariantCollectionItem;
            for (int i = 0; i < m_shadersProperty.arraySize; i++)
            {
                var oneShaderProperty = m_shadersProperty.GetArrayElementAtIndex(i);
                var firstSP = oneShaderProperty.FindPropertyRelative("first");
                if (firstSP != null)
                {
                    shader = firstSP.objectReferenceValue as Shader;
                    if (shader)
                    {
                        if (shader.IsDefineKeywords() == false)
                        {
                            //没有定义KeyWords的Shader不能剔除，这里不塞入字典内，不参与过滤
                            continue;
                        }
                        else
                        {
                            //如果定义了，但是没有在变体收集器中指定确定的变体，则默认剔除
                            if (!dict.TryGetValue(shader.name, out shaderVariantCollectionItem))
                            {
                                shaderVariantCollectionItem = new ShaderVariantCollectionItem(shader);
                                dict.Add(shader.name, shaderVariantCollectionItem);
                            }
                        }
                    }
                }

                var variantsSP = oneShaderProperty.FindPropertyRelative("second.variants");
                if (variantsSP != null)
                {

                    for (int j = 0; j < variantsSP.arraySize; j++)
                    {
                        var variantSP = variantsSP.GetArrayElementAtIndex(j);

                        var keywordsSp = variantSP.FindPropertyRelative("keywords");
                        if (keywordsSp != null)
                        {
                            keywords = keywordsSp.stringValue;

                        }

                        var passTypeSp = variantSP.FindPropertyRelative("passType");
                        if (passTypeSp != null)
                        {
                            passType = (PassType)passTypeSp.intValue;
                        }
                        //没有定义KeyWords的Shader不能剔除，这里不塞入字典内，不参与过滤
                        if (shader != null && !string.IsNullOrEmpty(keywords))
                        {
                            try
                            {
                                if (!dict.TryGetValue(shader.name, out shaderVariantCollectionItem))
                                {
                                    shaderVariantCollectionItem = new ShaderVariantCollectionItem(shader);
                                    dict.Add(shader.name, shaderVariantCollectionItem);
                                }
                                ShaderVariantInfo shaderVariant = new ShaderVariantInfo(shader, passType, Regex.Split(keywords, " "));
                                shaderVariantCollectionItem.AddVariant(passType, shaderVariant);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning(e.ToString());
                                Debug.LogWarningFormat("{0}, PassType = {1}, Keywords = '{2}'", shader.name, passType, keywords);
                            }

                        }
                    }


                }
            }
            return dict;
        }


        public static void ProcessFormalSVC(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // 这里用于全局剔除不要的特殊pass编译 
            if (snippet.passType == PassType.LightPrePassBase ||
                snippet.passType == PassType.LightPrePassFinal ||
                snippet.passType == PassType.Deferred)
            {
                data.Clear();
                return;
            }
            if (m_allshaderVariantDict == null)
            {
                m_allshaderVariantDict = new Dictionary<string, ShaderVariantCollectionItem>();
                shaderVariantCollection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(ShaderVariantCollectionPath);
                if (shaderVariantCollection == null)
                {
                    return;
                }
                GetShaderVariantList(shaderVariantCollection, m_allshaderVariantDict);
            }
            ShaderVariantCollectionItem shaderVariantList;
            if (!m_allshaderVariantDict.TryGetValue(shader.name, out shaderVariantList))
            {
                return;
            }
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (!shaderVariantList.IsContain(snippet.passType, data[i]))
                {
                    data.RemoveAt(i);
                }
            }
        }
    }
}
