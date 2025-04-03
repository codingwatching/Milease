﻿#if UNITY_EDITOR && MILEASE_ENABLE_CODEGEN
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Milease.CodeGen;
using Milease.Core;
using UnityEditor;
using UnityEngine;

namespace Milease.Editor
{
    public class Il2CppCalFuncGenerator
    {
        private class MemberMetaData
        {
            public string TName, EName, MemberName;
        }
        
        [MenuItem("Milease/Generate source code")]
        public static void Generate()
        {
            var path = AssetDatabase.GUIDToAssetPath("80c71963530044e459353fc6947dafe5");
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Milease",
                    "Please import il2cpp support package first, check it here:\n" +
                    "Project Window -> Packages\\com.morizero.milease\\", "OK");
                return;
            }
            
            var folder = Path.GetDirectoryName(path);
            
            EditorUtility.DisplayProgressBar("Milease", "Preparing...", 0f);

            var types = GenerationBridge.GetAnimatableTypes();
            var members =
                AccessorGenerationList.GetGenerateMembers().Select(x =>
                {
                    if (x.Body is MemberExpression memberExpr)
                    {
                        return new MemberMetaData()
                        {
                            TName = x.Parameters[0].Type.FullName,
                            EName = x.ReturnType.FullName,
                            MemberName = memberExpr.Member.Name
                        };
                    }
                    else if (x.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression propExpr)
                    {
                        return new MemberMetaData()
                        {
                            TName = x.Parameters[0].Type.FullName,
                            EName = x.ReturnType.FullName,
                            MemberName = propExpr.Member.Name
                        };
                    }
                    else
                    {
                        return null;
                    }
                }).Where(x => x != null);
            
            if (!EditorUtility.DisplayDialog("Milease",
                    $"Found {types.Count()} animatable type(s) and {members.Count()} member(s), " +
                            $"do you want to generate now?",
                    "Yes", "No"))
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            
            EditorUtility.DisplayProgressBar("Milease", "Generating source code...", 0.5f);

            var code = GenerateFunctions(types);
            path = Path.Combine(folder!, "GeneratedCalculation.cs");
            File.WriteAllText(path, code);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
            
            code = GenerateAccessors(members);
            path = Path.Combine(folder!, "GeneratedAccessors.cs");
            File.WriteAllText(path, code);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
            
            EditorUtility.ClearProgressBar();
        }
        
        [MenuItem("Milease/Reset source code")]
        public static void Reset()
        {
            var path = AssetDatabase.GUIDToAssetPath("80c71963530044e459353fc6947dafe5");
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Milease",
                    "Please import il2cpp support package first, check it here:\n" +
                    "Project Window -> Packages\\com.morizero.milease\\", "OK");
                return;
            }
            
            var folder = Path.GetDirectoryName(path);
            
            EditorUtility.DisplayProgressBar("Milease", "Resetting source code...", 0.5f);

            var code = GenerateFunctions(new Type[]{ });
            path = Path.Combine(folder!, "GeneratedCalculation.cs");
            File.WriteAllText(path, code);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
            
            code = GenerateAccessors(new MemberMetaData[]{ });
            path = Path.Combine(folder!, "GeneratedAccessors.cs");
            File.WriteAllText(path, code);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
            
            EditorUtility.ClearProgressBar();
        }

        private static string GenerateAccessors(IEnumerable<MemberMetaData> members)
        {
            string template;
            var sb = new StringBuilder();
            sb.AppendLine(
@"// This file is generated by Milease automatically

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Milease.CodeGen
{
    public static partial class GeneratedAccessors
    {
        internal static readonly Dictionary<(Type, string), object> getters = new Dictionary<(Type, string), object>()
        {");

            template = "            [(typeof(<<t>>), \"<<m>>\")] = new Func<<<t>>, <<e>>>(_mil_generated_get_<<n>>),";
            foreach (var member in members)
            {
                sb.AppendLine(
                    template.Replace("<<t>>", member.TName)
                            .Replace("<<e>>", member.EName)
                            .Replace("<<m>>", member.MemberName)
                            .Replace("<<n>>", 
                                (member.TName + "_" + member.MemberName)
                                        .Replace(".", "_")
                            )
                );
            }
            
            sb.AppendLine(
@"        };
        
        internal static readonly Dictionary<(Type, string), object> setters = new Dictionary<(Type, string), object>()
        {");
            
            template = "            [(typeof(<<t>>), \"<<m>>\")] = new Action<<<t>>, <<e>>>(_mil_generated_set_<<n>>),";
            foreach (var member in members)
            {
                sb.AppendLine(
                    template.Replace("<<t>>", member.TName)
                        .Replace("<<e>>", member.EName)
                        .Replace("<<m>>", member.MemberName)
                        .Replace("<<n>>", 
                            (member.TName + "_" + member.MemberName)
                            .Replace(".", "_")
                        )
                );
            }
            
            sb.AppendLine("        };");
            sb.AppendLine();
            
            template = 
@"        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static <<e>> _mil_generated_get_<<n>>(<<t>> o)
        {
            return o.<<m>>;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void _mil_generated_set_<<n>>(<<t>> o, <<e>> v)
        {
            o.<<m>> = v;
        }
";
            foreach (var member in members)
            {
                sb.AppendLine(
                    template.Replace("<<t>>", member.TName)
                        .Replace("<<e>>", member.EName)
                        .Replace("<<m>>", member.MemberName)
                        .Replace("<<n>>", 
                            (member.TName + "_" + member.MemberName)
                            .Replace(".", "_")
                        )
                );
            }
            
            sb.AppendLine(
@"    }
}");
            
            return sb.ToString();
        }
        
        private static string GenerateFunctions(IEnumerable<Type> types)
        {
            string template;
            var sb = new StringBuilder();
            sb.AppendLine(
@"// This file is generated by Milease automatically

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Milease.CodeGen
{
    public delegate E CalculateFunction<E>(E start, E end, float progress);
    public delegate E OffsetCalculateFunction<E>(E start, E end, float progress, E offset);
    public static partial class GeneratedCalculation
    {
        internal static readonly Dictionary<Type, object> calculateFunctions = new Dictionary<Type, object>()
        {");

            template = "            [typeof(<<t>>)] = new CalculateFunction<<<t>>>(_mil_generated_calc),";
            foreach (var type in types)
            {
                sb.AppendLine(template.Replace("<<t>>", type.FullName));
            }
            
            sb.AppendLine(
@"            [typeof(ushort)] = new CalculateFunction<ushort>(_mil_generated_calc),
            [typeof(short)] = new CalculateFunction<short>(_mil_generated_calc),
            [typeof(uint)] = new CalculateFunction<uint>(_mil_generated_calc),
            [typeof(int)] = new CalculateFunction<int>(_mil_generated_calc),
            [typeof(ulong)] = new CalculateFunction<ulong>(_mil_generated_calc),
            [typeof(long)] = new CalculateFunction<long>(_mil_generated_calc),
            [typeof(float)] = new CalculateFunction<float>(_mil_generated_calc),
            [typeof(double)] = new CalculateFunction<double>(_mil_generated_calc),
            [typeof(string)] = new CalculateFunction<string>(_mil_generated_calc),
            [typeof(object)] = new CalculateFunction<object>(_mil_generated_calc)
        };

        internal static readonly Dictionary<Type, object> offsetCalculateFunctions = new Dictionary<Type, object>()
        {");
            
            template = "            [typeof(<<t>>)] = new OffsetCalculateFunction<<<t>>>(_mil_generated_calc_offset),";
            foreach (var type in types)
            {
                sb.AppendLine(template.Replace("<<t>>", type.FullName));
            }
            
            sb.AppendLine(
@"            [typeof(ushort)] = new OffsetCalculateFunction<ushort>(_mil_generated_calc_offset),
            [typeof(short)] = new OffsetCalculateFunction<short>(_mil_generated_calc_offset),
            [typeof(uint)] = new OffsetCalculateFunction<uint>(_mil_generated_calc_offset),
            [typeof(int)] = new OffsetCalculateFunction<int>(_mil_generated_calc_offset),
            [typeof(ulong)] = new OffsetCalculateFunction<ulong>(_mil_generated_calc_offset),
            [typeof(long)] = new OffsetCalculateFunction<long>(_mil_generated_calc_offset),
            [typeof(float)] = new OffsetCalculateFunction<float>(_mil_generated_calc_offset),
            [typeof(double)] = new OffsetCalculateFunction<double>(_mil_generated_calc_offset),
            [typeof(string)] = new OffsetCalculateFunction<string>(_mil_generated_calc_offset),
            [typeof(object)] = new OffsetCalculateFunction<object>(_mil_generated_calc_offset)
        };
        ");
            
            template = 
@"        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static <<t>> _mil_generated_calc(<<t>> a, <<t>> b, float p)
        {
            return a + (b - a) * p;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static <<t>> _mil_generated_calc_offset(<<t>> a, <<t>> b, float p, <<t>> o)
        {
            return a + (b - a) * p + o;
        }
        ";
            foreach (var type in types)
            {
                sb.AppendLine(template.Replace("<<t>>", type.FullName));
            }
            
            sb.AppendLine(
@"        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort _mil_generated_calc(ushort a, ushort b, float p)
        {
            return (ushort)(a + (b - a) * p);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort _mil_generated_calc_offset(ushort a, ushort b, float p, ushort o)
        {
            return (ushort)(a + (b - a) * p + o);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short _mil_generated_calc(short a, short b, float p)
        {
            return (short)(a + (b - a) * p);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short _mil_generated_calc_offset(short a, short b, float p, short o)
        {
            return (short)(a + (b - a) * p + o);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint _mil_generated_calc(uint a, uint b, float p)
        {
            return (uint)(a + (b - a) * p);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint _mil_generated_calc_offset(uint a, uint b, float p, uint o)
        {
            return (uint)(a + (b - a) * p + o);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int _mil_generated_calc(int a, int b, float p)
        {
            return (int)(a + (b - a) * p);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int _mil_generated_calc_offset(int a, int b, float p, int o)
        {
            return (int)(a + (b - a) * p + o);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _mil_generated_calc(ulong a, ulong b, float p)
        {
            return (ulong)(a + (b - a) * p);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _mil_generated_calc_offset(ulong a, ulong b, float p, ulong o)
        {
            return (ulong)(a + (b - a) * p + o);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long _mil_generated_calc(long a, long b, float p)
        {
            return (long)(a + (b - a) * p);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long _mil_generated_calc_offset(long a, long b, float p, long o)
        {
            return (long)(a + (b - a) * p + o);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float _mil_generated_calc(float a, float b, float p)
        {
            return a + (b - a) * p;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float _mil_generated_calc_offset(float a, float b, float p, float o)
        {
            return a + (b - a) * p + o;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double _mil_generated_calc(double a, double b, float p)
        {
            return a + (b - a) * p;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double _mil_generated_calc_offset(double a, double b, float p, double o)
        {
            return a + (b - a) * p + o;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string _mil_generated_calc(string a, string b, float p)
        {
            return b?.Substring(0, (int)(b.Length * p));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string _mil_generated_calc_offset(string a, string b, float p, string o)
        {
            return o + b?.Substring(0, (int)(b.Length * p));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object _mil_generated_calc(object a, object b, float p)
        {
            return (p >= 1f ? b : a);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object _mil_generated_calc_offset(object a, object b, float p, object o)
        {
            return (p >= 1f ? b : a);
        }
    }
}");

            return sb.ToString();
        }
    }
}
#endif
