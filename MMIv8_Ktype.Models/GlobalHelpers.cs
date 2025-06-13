using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using Serilog;
using System;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MMIv8_Ktype.Models
{
    public static class GlobalHelpers
    {
        public static ExpandoObject AddProperty(this ExpandoObject expando, string propertyName, object? propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
            {
                expandoDict[ propertyName ] = propertyValue ?? string.Empty;
            }
            else
            {
                expandoDict.Add(propertyName, propertyValue ?? string.Empty);
            }

            return expando;
        }
        //public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        //{
        //    var expandoDict = expando as IDictionary<string, object>;
        //    if (expandoDict.ContainsKey(propertyName))
        //    {
        //        expandoDict[ propertyName ] = propertyValue;
        //    }
        //    else
        //    {
        //        expandoDict.Add(propertyName, propertyValue);
        //    }
        //}

        public static string GenerateKey(object sourceObject)
        {
            string hashString;

            if (sourceObject == null)
            {
                throw new ArgumentNullException("Null as parameter is not allowed");
            }
            else
            {
                try
                {
                    string objectSerialize = System.Text.Json.JsonSerializer.Serialize(sourceObject);
                    var objectAsBytes = Encoding.UTF8.GetBytes(objectSerialize);

                    hashString = ComputeHash(objectAsBytes);
                    return hashString;
                }
                catch (AmbiguousMatchException ame)
                {
                    throw new ApplicationException("Could not definitely decide if object is serializable.Message:" + ame.Message);
                }
            }
        }

        //TODO Stolen needs proof
        public static string LongestCommonSubsequence(string str1, string str2, out string aligned1, out string aligned2)
        {
            int[,] dp = ComputeLCSMatrix(str1, str2);
            int m = str1.Length, n = str2.Length;
            int x = m, y = n;
            string lcs = "";
            string alignedStr1 = "", alignedStr2 = "";

            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0 && str1[ x - 1 ] == str2[ y - 1 ])
                {
                    lcs = str1[ x - 1 ] + lcs;
                    alignedStr1 = str1[ x - 1 ] + alignedStr1;
                    alignedStr2 = str2[ y - 1 ] + alignedStr2;
                    x--;
                    y--;
                }
                else if (y > 0 && (x == 0 || dp[ x, y - 1 ] >= dp[ x - 1, y ]))
                {
                    alignedStr1 = "-" + alignedStr1;
                    alignedStr2 = str2[ y - 1 ] + alignedStr2;
                    y--;
                }
                else
                {
                    alignedStr1 = str1[ x - 1 ] + alignedStr1;
                    alignedStr2 = "-" + alignedStr2;
                    x--;
                }
            }

            aligned1 = alignedStr1;
            aligned2 = alignedStr2;
            return lcs;
        }


        public static Dictionary<string, object> ObjToDictionary(object obj)
        {
            if (obj is null)
                throw new NullReferenceException("Object is empty or missing");

            try
            {
                Dictionary<string, object> dict = obj.GetType()
                              .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                              .ToDictionary(prop => prop.Name, prop => prop.GetValue(obj, null) ?? string.Empty);

                return dict ?? new();
            }
            catch (NullReferenceException nrex)
            {
                //Serilog.Log.Error(nrex, "Exception using ObjToDictionary");
                //return new();
                throw new Exception("Exception using ObjToDictionary", nrex);
            }
            catch (Exception ex)
            {
                //Serilog.Log.Error(ex, "Exception using ObjToDictionary: {obj}", obj);
                //return new();
                throw new Exception($"Exception using ObjToDictionary: {@obj}", ex);
            }
        }

        /// <summary>
        /// Instantiate class from constructorArgs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="constructorArgs"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NullReferenceException"></exception>
        public static T StringToObject<T>(this Type type, string[] constructorArgs)
        {
            if (type != typeof(T) && !type.IsAssignableTo(typeof(T)))
                throw new Exception("Type and T are not related");

            if (constructorArgs is null)
                throw new NullReferenceException("constructorArgs is empty or missing");

            // Find a constructor that matches the number of parameters
            var constructor = type.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == constructorArgs.Length);
            if (constructor == null)
                throw new Exception($"No matching constructor found for class '{typeof(T)}' with {constructorArgs.Length} parameters.");

            // Convert parameters to the constructor parameter types
            var parameters = constructor.GetParameters();
            object[] parsedArgs = new object[ constructorArgs.Length ];
            for (int i = 0; i < constructorArgs.Length; i++)
            {
                //parsedArgs[ i ] = Convert.ChangeType(constructorArgs[ i ], parameters[ i ].ParameterType);

                Type paramType = parameters[ i ].ParameterType;
                var defaultValue = parameters[ i ].DefaultValue;
                string arg = constructorArgs[ i ];

                if (paramType.IsEnum)
                    parsedArgs[ i ] = Enum.Parse(paramType, arg);
                else if (arg == "null")
                    parsedArgs[ i ] = defaultValue;
                else if (Nullable.GetUnderlyingType(paramType) == typeof(bool))
                    parsedArgs[ i ] = bool.Parse(arg);
                else
                    parsedArgs[ i ] = Convert.ChangeType(arg, paramType);
            }

            // Instantiate the class
            return (T)constructor.Invoke(parsedArgs);
        }

        public static T StringToObject<T>(string[] constructorArgs)
        {
            return StringToObject<T>(typeof(T), constructorArgs);
        }

        public static string RemoveDuplicatedStrings(string str, string[] stringsToRemove)
        {
            foreach (string stringToRemove in stringsToRemove)
            {
                str = $" {str} ".Replace($" {stringToRemove} ", " ").Trim();
            }

            return str.Replace(Convert.ToChar((byte)160), ' ').Trim() ?? string.Empty;
        }

        public static string RemoveDuplicatedStringsTokenised(string str, string[] stringsToRemove)
        {
            stringsToRemove = string.Join(' ', stringsToRemove)
                                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Distinct()
                                    .ToArray();

            string[] strArray = str.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Except(stringsToRemove)
                                   .ToArray();

            return strArray.Length > 0 ? string.Join(' ', strArray) : string.Empty;
        }


        //public static string RemoveSpecialCharacters(string str)
        //{
        //    return Regex.Replace(str, "[^a-zA-Z0-9_|()/]+", "", RegexOptions.Compiled).ToLower();
        //}

        public static string RemoveSpecialCharacters(this string str, char[]? exclusions = null)
        {
            return Regex.Replace(str, $"[^a-zA-Z0-9_{string.Join(string.Empty, exclusions ?? [])}]+", "", RegexOptions.Compiled).ToLower();
        }

        private static string ComputeHash(byte[] objectAsBytes)
        {
            try
            {
                byte[] result = SHA256.HashData(objectAsBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < result.Length; i++)
                {
                    sb.Append(result[ i ].ToString("X2"));
                }

                return sb.ToString();
            }
            catch (ArgumentNullException ane)
            {
                Log.Error(ane, "Hash has not been generated");
                return string.Empty;
            }
        }

        //TODO Stolen needs proof
        static int[,] ComputeLCSMatrix(string str1, string str2)
        {
            int m = str1.Length, n = str2.Length;
            int[,] dp = new int[ m + 1, n + 1 ];

            // Fill DP table
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (str1[ i - 1 ] == str2[ j - 1 ])
                        dp[ i, j ] = dp[ i - 1, j - 1 ] + 1;
                    else
                        dp[ i, j ] = Math.Max(dp[ i - 1, j ], dp[ i, j - 1 ]);
                }
            }

            return dp;
        }
    }
}