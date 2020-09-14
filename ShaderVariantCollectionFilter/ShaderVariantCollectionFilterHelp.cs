using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ShaderVariantCollectionFilter
{
    public static class Utils
    {
        public static Type FindTypeInAssembly(String typeName, Assembly assembly = null)
        {
            Type type = null;
            if (assembly == null)
            {
                type = Type.GetType(typeName, false);
            }
            if (type == null && assembly != null)
            {
                var types = assembly.GetTypes();
                for (int j = 0; j < types.Length; ++j)
                {
                    var b = types[j];
                    if (b.FullName == typeName)
                    {
                        type = b;
                        break;
                    }
                }
            }
            return type;
        }

        public static Type FindType(String typeName, String assemblyName = null)
        {
            Type type = null;
            try
            {
                if (String.IsNullOrEmpty(assemblyName))
                {
                    type = Type.GetType(typeName, false);
                }
                if (type == null)
                {
                    var asm = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < asm.Length; ++i)
                    {
                        var a = asm[i];
                        if (String.IsNullOrEmpty(assemblyName) || a.GetName().Name == assemblyName)
                        {
                            var types = a.GetTypes();
                            for (int j = 0; j < types.Length; ++j)
                            {
                                var b = types[j];
                                if (b.FullName == typeName)
                                {
                                    type = b;
                                    goto END;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            END:
            if (type == null)
            {
                Debug.LogWarningFormat("FindType( \"{0}\", \"{1}\" ) failed!",
                    typeName, assemblyName ?? String.Empty);
            }
            return type;
        }

        public static object RflxGetValue(String typeName, String memberName, String assemblyName = null)
        {
            object value = null;
            var type = FindType(typeName, assemblyName);
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0, count = smembers.Length; i < count && value == null; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            value = pi.GetValue(null, null);
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null)
                            {
                                value = fi.GetValue(null);
                            }
                        }
                    }
                }
            }
            return value;
        }

        public static bool RflxSetValue(String typeName, String memberName, object value, String assemblyName = null)
        {
            var type = FindType(typeName, assemblyName);
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < smembers.Length; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            pi.SetValue(null, value, null);
                            return true;
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null)
                            {
                                if (fi.IsLiteral == false && fi.IsInitOnly == false)
                                {
                                    fi.SetValue(null, value);
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static object RflxGetValue(Type type, String memberName)
        {
            object value = null;
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0, count = smembers.Length; i < count && value == null; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            value = pi.GetValue(null, null);
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null)
                            {
                                value = fi.GetValue(null);
                            }
                        }
                    }
                }
            }
            return value;
        }

        public static bool RflxSetValue(Type type, String memberName, object value)
        {
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < smembers.Length; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            pi.SetValue(null, value, null);
                            return true;
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null && fi.IsLiteral == false && fi.IsInitOnly == false)
                            {
                                fi.SetValue(null, value);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static object RflxStaticCall(String typeName, String funcName, object[] parameters = null, String assemblyName = null)
        {
            var type = FindType(typeName, assemblyName);
            if (type != null)
            {
                var f = type.GetMethod(funcName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (f != null)
                {
                    var r = f.Invoke(null, parameters);
                    return r;
                }
            }
            return null;
        }

        public static object RflxStaticCall(Type type, String funcName, object[] parameters = null)
        {
            if (type != null)
            {
                var f = type.GetMethod(funcName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (f != null)
                {
                    var r = f.Invoke(null, parameters);
                    return r;
                }
            }
            return null;
        }

        public static String GetProjectUnityTempPath()
        {
            var rootPath = Environment.CurrentDirectory.Replace('\\', '/');
            rootPath += "/Temp";
            if (Directory.Exists(rootPath))
            {
                rootPath = Path.GetFullPath(rootPath);
                return rootPath.Replace('\\', '/');
            }
            else
            {
                return rootPath;
            }
        }

        public static bool IsUnityDefaultResource(String path)
        {
            return String.IsNullOrEmpty(path) == false &&
                (path == "Resources/unity_builtin_extra" ||
                path == "Library/unity default resources");
        }
    }

    public static class ShaderVariantCollectionFilterHelp
    {
        public static string[] GetShaderGlobalKeywords(this Shader shader)
        {
            var result = Utils.RflxStaticCall(typeof(ShaderUtil), "GetShaderGlobalKeywords", new object[] { shader });
            if (result == null)
            {
                Debug.LogError("GetShaderGlobalKeywords failed !!");
                return null;
            }
            var globalKeywords = result as string[];
            return globalKeywords;
        }

        public static string[] GetShaderLocalKeywords(this Shader shader)
        {
            var result = Utils.RflxStaticCall(typeof(ShaderUtil), "GetShaderLocalKeywords", new object[] { shader });
            if (result == null)
            {
                Debug.LogError("GetShaderLocalKeywords failed !!");
                return null;
            }
            var localKeywords = result as string[];
            return localKeywords;
        }

        public static int GetShaderLocalKeywordsCount(this Shader shader)
        {
            var result = Utils.RflxStaticCall(typeof(ShaderUtil), "GetShaderLocalKeywords", new object[] { shader });
            if (result == null)
            {
                Debug.LogError("GetShaderLocalKeywords failed !!");
                return 0;
            }
            var localKeywords = result as string[];
            if (localKeywords == null)
            {
                return 0;
            }
            else
            {
                return localKeywords.Length;
            }
        }

        public static int GetShaderGlobalKeywordsCount(this Shader shader)
        {
            var result = Utils.RflxStaticCall(typeof(ShaderUtil), "GetShaderGlobalKeywords", new object[] { shader });
            if (result == null)
            {
                Debug.LogError("GetShaderGlobalKeywords failed !!");
                return 0;
            }
            var globalKeywords = result as string[];
            if (globalKeywords == null)
            {
                return 0;
            }
            else
            {
                return globalKeywords.Length;
            }
        }

        public static int GetShaderKeywordsTotalCount(this Shader shader)
        {
            return shader.GetShaderGlobalKeywordsCount() + shader.GetShaderLocalKeywordsCount();
        }
        /// <summary>
        /// 是否定义了关键字
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static bool IsDefineKeywords(this Shader shader)
        {
            int total = shader.GetShaderGlobalKeywordsCount() + shader.GetShaderLocalKeywordsCount();
            return total > 0 ;
        }
    }
}
