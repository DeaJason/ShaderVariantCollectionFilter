using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderVariantCollectionFilter
{
    public struct ShaderVariantInfo
    {
        public Shader m_shader;
        public PassType m_passType;
        public string[] m_keywords;
        public int m_keyWordHash;
        static HashSet<string> hashSet = new HashSet<string>();

        public ShaderVariantInfo(Shader shader, PassType passType, string[] keyWords)
        {
            m_shader = shader;
            m_passType = passType;
            m_keywords = keyWords;
            m_keyWordHash = 0;
        }

        public bool IsEqual(Shader shader, PassType passType, string[] keyWords)
        {
            if (m_shader != shader)
            {
                return false;
            }
            return IsEqual(passType, keyWords);
        }

        public bool IsEqual(PassType passType, string[] keyWords)
        {
            if (m_passType != passType)
            {
                return false;
            }
            if (keyWords == null)
            {
                return false;
            }
            if (keyWords.Length != m_keywords.Length)
            {
                return false;
            }
            hashSet.Clear();
            foreach (var item in keyWords)
            {
                hashSet.Add(item);
            }
            foreach (var key in m_keywords)
            {
                if (!hashSet.Contains(key))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
