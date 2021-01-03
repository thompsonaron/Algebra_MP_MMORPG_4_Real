using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.IO;
using System.Text;

public class Generator
{
    [MenuItem("Generator/Generate")]
    public static void Generate()
    {
        // serializator class using namespaces
        StringBuilder sbSer = new StringBuilder();
        sbSer.AppendLine("using System.Collections.Generic;");
        sbSer.AppendLine("using System.IO;");
        sbSer.AppendLine("using System;");
        sbSer.AppendLine(Environment.NewLine);
        sbSer.AppendLine("public static class Serializator");
        sbSer.AppendLine("{");

        // deserializator sb
        StringBuilder sbDeser = new StringBuilder();
        // deserializator inner methods sb
        StringBuilder sbDeserRef = new StringBuilder();

        // getting all assemblies
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            // narrowing to desired assemblies
            if (assembly.FullName.Contains("CSharp") && !assembly.FullName.Contains("Editor"))
            {
                // all classes in assembly
                var assemblyClasses = assembly.GetTypes();
                foreach (var classType in assemblyClasses)
                {
                    // getting all attributes from a class
                    Attribute[] attrs = System.Attribute.GetCustomAttributes(classType);
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        //if attribute is [Serializable] => do stuff
                        if (attrs[i] is SerializableAttribute)
                        {
                            // SERIALIZATOR function declarations
                            sbSer.AppendLine("public static byte[] serialize(" + classType.Name + " " + classType.Name.ToLower() + ")");
                            sbSer.AppendLine("{");
                            sbSer.AppendLine("var s = new MemoryStream();");
                            sbSer.AppendLine("var bW = new BinaryWriter(s);");

                            // DESERIALIZATOR function declarations
                            sbDeser.AppendLine("public static " + classType.Name + " Deserialize" + classType.Name + " (byte[] b)");
                            sbDeser.AppendLine("{");
                            sbDeser.AppendLine("var s = new MemoryStream(b);");
                            sbDeser.AppendLine("var bR = new BinaryReader(s);");
                            sbDeser.AppendLine("var obj = new " + classType.Name + "();");

                            // DESERIALIZATOR inner function declarations
                            sbDeserRef.AppendLine("private static " + classType.Name + " Deserialize" + classType.Name + " (ref byte[] b, ref MemoryStream s, ref BinaryReader bR)");
                            sbDeserRef.AppendLine("{");
                            sbDeserRef.AppendLine("var obj = new " + classType.Name + "();");

                            // BindingFlags are NOT USED
                            // TODO bindingFlags will get all types of fields with binding flags - without it, it will ONLY get PUBLIC fields
                            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                            var fields = classType.GetFields(bindingFlags);

                            foreach (var field in classType.GetFields())
                            {
                                if (field.FieldType.IsList())
                                {
                                    // meaning if it is class from this project (editor clases are excluded above)
                                    bool existsInAssembly = ExistsInAssembly(assemblyClasses, field, CollectionType.List);

                                    // SERIALIZATOR
                                    // serializator - writing list count and each item
                                    sbSer.AppendLine("bW.Write(" + classType.Name.ToLower() + "." + field.Name + ".Count);");
                                    sbSer.AppendLine("foreach (var item in " + classType.Name.ToLower() + "." + field.Name + ")");
                                    sbSer.AppendLine("{");
                                    // if item is a  class
                                    if (existsInAssembly) { sbSer.AppendLine("bW.Write(serialize(item));"); }
                                    // if item is a primitive or a string
                                    else { sbSer.AppendLine("bW.Write(item);"); }
                                    sbSer.AppendLine("}");

                                    // DESERIALIZATOR and deserializator inner functions
                                    // extracts class name from list
                                    string[] classNameSplit = field.FieldType.ToString().Split('[', ']');
                                    // separates class name IF it has '.' - meaning it is one of System classes
                                    string[] className = classNameSplit[1].Split('.');

                                    // initalizing a list with primitive or a class
                                    // init primitive
                                    if (className.Length > 1)
                                    {
                                        string primitiveInit = "obj." + field.Name + " = new List<" + className[1] + ">();";
                                        sbDeser.AppendLine(primitiveInit);
                                        sbDeserRef.AppendLine(primitiveInit);
                                    }
                                    // init class
                                    else
                                    {
                                        string classInit = "obj." + field.Name + " = new List<" + className[0] + ">();";
                                        sbDeser.AppendLine(classInit);
                                        sbDeserRef.AppendLine(classInit);
                                    }

                                    // reading list size
                                    string readListSize = "int " + field.Name + "ListSize = bR.ReadInt32();";
                                    sbDeser.AppendLine(readListSize);
                                    sbDeserRef.AppendLine(readListSize);

                                    // setting for loop for both starting and inner functions 
                                    string forLoopDeclaration = "for (int i = 0; i < " + field.Name + "ListSize; i++)";
                                    sbDeser.AppendLine(forLoopDeclaration);
                                    sbDeserRef.AppendLine(forLoopDeclaration);
                                    sbDeser.AppendLine("{");
                                    sbDeserRef.AppendLine("{");
                                    // field is "primitive"
                                    if (className.Length > 1)
                                    {
                                        string primitiveValue = "obj." + field.Name + ".Add(bR.Read" + className[1] + "());";
                                        sbDeser.AppendLine(primitiveValue);
                                        sbDeserRef.AppendLine(primitiveValue);
                                    }
                                    // field is a class
                                    else
                                    {
                                        string classValue = "obj." + field.Name + ".Add(Deserialize" + className[0] + "(ref b, ref s,ref bR));";
                                        sbDeser.AppendLine(classValue);
                                        sbDeserRef.AppendLine(classValue);
                                    }

                                    sbDeser.AppendLine("}");
                                    sbDeserRef.AppendLine("}");
                                }
                                else if (field.FieldType.IsArray)
                                {
                                    bool existsInAssembly = ExistsInAssembly(assemblyClasses, field, CollectionType.Array);

                                    // SERIALIZATOR
                                    // Array Length
                                    sbSer.AppendLine("bW.Write(" + classType.Name.ToLower() + "." + field.Name + ".Length);");
                                    // foreach declaration
                                    sbSer.AppendLine("foreach (var item in " + classType.Name.ToLower() + "." + field.Name + ")");
                                    sbSer.AppendLine("{");
                                    // if item is a class
                                    if (existsInAssembly) { sbSer.AppendLine("bW.Write(serialize(item));"); }
                                    // if item is a primitive
                                    else { sbSer.AppendLine("bW.Write(item);"); }
                                    sbSer.AppendLine("}");

                                    // DESERIALIZATOR and deserializator inner functions
                                    // extracts class name from list
                                    string[] classNameSplit = field.FieldType.ToString().Split('[');
                                    // separates class name IF it has '.' - meaning it is one of System classes
                                    string[] className = classNameSplit[0].Split('.');

                                    // setting array size
                                    string arraySize = "int " + field.Name + "ArraySize = bR.ReadInt32();";
                                    sbDeser.AppendLine(arraySize);
                                    sbDeserRef.AppendLine(arraySize);
                                    // initilization if type is a primitive
                                    if (className.Length > 1)
                                    {
                                        string primitiveInit = "obj." + field.Name + " = new " + className[1] + "[" + field.Name + "ArraySize];";
                                        sbDeser.AppendLine(primitiveInit);
                                        sbDeserRef.AppendLine(primitiveInit);
                                    }
                                    // initilization if type is a class
                                    else
                                    {
                                        string classInit = "obj." + field.Name + " = new " + className[0] + "[" + field.Name + "ArraySize];";
                                        sbDeser.AppendLine(classInit);
                                        sbDeserRef.AppendLine(classInit);
                                    }

                                    string forLoopDeclaration = "for (int i = 0; i <" + field.Name + "ArraySize; i++)";
                                    sbDeser.AppendLine(forLoopDeclaration);
                                    sbDeserRef.AppendLine(forLoopDeclaration);
                                    sbDeser.AppendLine("{");
                                    sbDeserRef.AppendLine("{");
                                    // reading a primitive value
                                    if (className.Length > 1)
                                    {
                                        string primitiveRead = "obj." + field.Name + "[i] = bR.Read" + className[1] + "();";
                                        sbDeser.AppendLine(primitiveRead);
                                        sbDeserRef.AppendLine(primitiveRead);
                                    }
                                    // reading a class value
                                    else
                                    {
                                        string classRead = "obj." + field.Name + "[i] = Deserialize" + className[0] + "(ref b, ref s, ref bR);";
                                        sbDeser.AppendLine(classRead);
                                        sbDeserRef.AppendLine(classRead);
                                    }
                                    sbDeser.AppendLine("}");
                                    sbDeserRef.AppendLine("}");

                                }
                                else
                                {
                                    bool existsInAssembly = ExistsInAssembly(assemblyClasses, field, CollectionType.None);

                                    // serializator
                                    if (existsInAssembly) { sbSer.AppendLine("bW.Write(serialize(" + classType.Name.ToLower() + "." + field.Name + "));"); }
                                    else { sbSer.AppendLine("bW.Write(" + classType.Name.ToLower() + "." + field.Name + ");"); }

                                    // deserializator private function call
                                    if (existsInAssembly)
                                    {
                                        string classRead = "obj." + field.Name + " = Deserialize" + field.FieldType + "(ref b, ref s, ref bR);";
                                        // deserializing a class
                                        sbDeser.AppendLine(classRead);
                                        sbDeserRef.AppendLine(classRead);
                                    }
                                    // deserializator public function call
                                    else
                                    {
                                        // split, after FieldType is read, will do something along splitting System.Int32 and putting System on pos 0 and Int32 on pos 1
                                        string[] fieldTypeSplit = field.FieldType.ToString().Split('.');
                                        // deserializing primitive
                                        string primitiveRead = "obj." + field.Name + " = bR.Read" + fieldTypeSplit[1] + "();";
                                        sbDeser.AppendLine(primitiveRead);
                                        sbDeserRef.AppendLine(primitiveRead);
                                    }
                                }
                            }
                            // serializator
                            sbSer.AppendLine("return s.ToArray();");
                            sbSer.AppendLine("}");

                            // deserializator public and private
                            sbDeser.AppendLine("return obj;");
                            sbDeserRef.AppendLine("return obj;");
                            sbDeser.AppendLine("}");
                            sbDeserRef.AppendLine("}");
                        }
                    }

                }
            }
        }
        // deserializator injection
        sbSer.AppendLine(sbDeser.ToString());

        // deserializator private function injectioin
        sbSer.AppendLine(sbDeserRef.ToString());

        // serializator
        sbSer.AppendLine("}");
        File.WriteAllText(Application.dataPath + "/Serializator.cs", sbSer.ToString());
        AssetDatabase.Refresh();
    }

    private static bool ExistsInAssembly(Type[] assemblyClasses, FieldInfo field, CollectionType collectionType)
    {
        bool existsInAssembly = false;
        string fieldClass = field.FieldType.ToString();
        switch (collectionType)
        {
            case CollectionType.Array:
                fieldClass = fieldClass.Substring(0, fieldClass.Length - 2);
                break;
            case CollectionType.List:
                int start = fieldClass.LastIndexOf("[") + 1;
                int length = fieldClass.Length - start - 1;
                fieldClass = fieldClass.Substring(start, length);
                break;
            case CollectionType.None:

                break;
            default:
                break;
        }
        foreach (var item in assemblyClasses)
        {
            if (item.Name == fieldClass)
            {
                existsInAssembly = true;
            }
        }

        return existsInAssembly;
    }
}

public static class Extensions
{
    public static bool IsList(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        //return type == typeof(List<>);
    }
}

enum CollectionType
{
    Array, List, None
}