//Source: https://gist.github.com/koshelevpavel/8e2d62053fc79b2bf9e2105d18b056bc
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf;
using ProtoBuf.Meta;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = UnityEditor.Compilation.Assembly;

public class ProtobufSerializerCheck : MonoBehaviour
{
    [MenuItem("Protobuf/Build model")]
    private static void BuildMyProtoModel()
    {
        RuntimeTypeModel typeModel = GetModel();
        //typeModel.Compile("MyProtoModel", "MyProtoModel.dll");
        RuntimeTypeModel.CompilerOptions co = new RuntimeTypeModel.CompilerOptions();
        co.TypeName = "MyProtoModel";
        #pragma warning disable CS0618 // Type or member is obsolete
        co.OutputPath = "MyProtoModel.dll";
        #pragma warning restore CS0618 // Type or member is obsolete
        typeModel.Compile(co);

        if (!Directory.Exists("Assets/Plugins"))
        {
            Directory.CreateDirectory("Assets/Plugins");
        }

        File.Copy("MyProtoModel.dll", "Assets/Plugins/MyProtoModel.dll");

        AssetDatabase.Refresh();
    }

    [MenuItem("Protobuf/Create proto file")]
    private static void CreateProtoFile()
    {
        RuntimeTypeModel typeModel = GetModel();
        using (FileStream stream = File.Open("model.proto", FileMode.Create))
        {
            byte[] protoBytes = Encoding.UTF8.GetBytes(typeModel.GetSchema(null));
            stream.Write(protoBytes, 0, protoBytes.Length);
        }
    }

    private static RuntimeTypeModel GetModel()
    {
        RuntimeTypeModel typeModel = TypeModel.Create();

        foreach (var t in GetTypes(CompilationPipeline.GetAssemblies()))
        {
            var contract = t.GetCustomAttributes(typeof(ProtoContractAttribute), false);
            if (contract.Length > 0)
            {
                typeModel.Add(t, true);

                //add unity types
                //typeModel.Add(typeof(Vector2), false).Add("x", "y");
                //typeModel.Add(typeof(Vector3), false).Add("x", "y", "z");
            }
        }

        return typeModel;
    }

    private static IEnumerable<Type> GetTypes(IEnumerable<Assembly> assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in AppDomain.CurrentDomain.Load(assembly.name).GetTypes())
            {
                yield return type;
            }
        }
    }
}
#endif