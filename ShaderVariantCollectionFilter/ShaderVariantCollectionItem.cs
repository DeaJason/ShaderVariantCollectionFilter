using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderVariantCollectionFilter
{
    public struct ShaderVariantCollectionItem
    {
        public Shader m_shader;
        public Dictionary<PassType, List<ShaderVariantInfo>> m_variantByPassTypeDict;

        public ShaderVariantCollectionItem(Shader shader)
        {
            m_shader = shader;
            m_variantByPassTypeDict = new Dictionary<PassType, List<ShaderVariantInfo>>();
        }

        public void AddVariant(PassType passType, ShaderVariantInfo variant)
        {
            List<ShaderVariantInfo> m_tempList;
            if (!m_variantByPassTypeDict.TryGetValue(passType, out m_tempList))
            {
                m_tempList = new List<ShaderVariantInfo>();
                m_variantByPassTypeDict.Add(passType, m_tempList);
            }
            m_tempList.Add(variant);
        }

        public bool IsContain(Shader shader, PassType passType, string[] keyWords)
        {
            if (m_shader != shader)
            {
                return false;
            }
            return IsContain(passType, keyWords);
        }

        public bool IsContain(PassType passType, string[] keyWords)
        {
            List<ShaderVariantInfo> m_tempList;
            if (!m_variantByPassTypeDict.TryGetValue(passType, out m_tempList))
            {
                return false;
            }
            bool isHas = false;
            for (int i = 0; i < m_tempList.Count; i++)
            {
                isHas = m_tempList[i].IsEqual(passType, keyWords);
                if (isHas == true)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsContain(PassType passType, ShaderCompilerData compilerData)
        {
            List<ShaderVariantInfo> m_tempList;
            if (!m_variantByPassTypeDict.TryGetValue(passType, out m_tempList))
            {
                return false;
            }
            var list = compilerData.shaderKeywordSet.GetShaderKeywords();
            if (list.Length == 0)
            {
                //无关键字的变体，LocalKeyWord目前获取不到，shader_feature_local、multi_compile_local
                return true;
            }
            string[] keyWords = new string[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                keyWords[i] = list[i].GetKeywordName();
            }
            bool isHas = false;
            for (int i = 0; i < m_tempList.Count; i++)
            {
                isHas = m_tempList[i].IsEqual(passType, keyWords);
                if (isHas == true)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
