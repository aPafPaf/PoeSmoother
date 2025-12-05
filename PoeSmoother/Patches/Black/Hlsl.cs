using LibBundle3.Nodes;
using System.Text.RegularExpressions;

namespace PoeSmoother.Patches;

public class Hlsl : IPatch
{
    public string Name => "Hlsl Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".hlsl",
    };

    private readonly string[] targetFilesPath =
    {
         @"shaders/postprocessuber.hlsl",
         @"shaders/bloomcutoff.hlsl",
         @"shaders/bloomgather.hlsl",
         @"shaders/blur.hlsl",
         @"shaders/copytexture.hlsl",
         @"shaders/depthawareblur.hlsl",
         @"shaders/gaussianblur.hlsl",
         @"shaders/postprocessoutput.hlsl",
         @"shaders/postprocessuber.hlsl",
         @"shaders/screenspaceshadows.hlsl",
         @"shaders/include/lighting.hlsl",
         @"shaders/include/projection.hlsl",
    };

    private List<FileNode> fileNodes = [];

    private void CollectFileNodesRecursively(DirectoryNode dir)
    {
        foreach (var node in dir.Children)
        {
            switch (node)
            {
                case DirectoryNode childDir:
                    CollectFileNodesRecursively(childDir);
                    break;

                case FileNode fileNode:
                    if (HasTargetExtension(fileNode.Name) &&
                        targetFilesPath.Contains(fileNode.Record.Path))
                        fileNodes.Add(fileNode);
                    break;
            }
        }
    }

    private void TryPatchFile(FileNode file)
    {
        var record = file.Record;
        var bytes = record.Read();
        var arrayOrigBytes = bytes.ToArray();
        string data = System.Text.Encoding.UTF8.GetString(arrayOrigBytes);

        List<string> functions = [];

        int iteration = 0;
        while (iteration < 100)
        {
            iteration++;

            string pattern = @"(float|float2|float3|float4|float2x2|float3x3|float4x4|void|OutPixel)\s+\w+\s*\([^)]*\)";
            var matchCollection = Regex.Matches(data, pattern);

            if (matchCollection.Count == 0)
                break;

            Match math = matchCollection.FirstOrDefault(
                x => x is not null &&
                x.Success &&
                !functions.Contains(x.Captures.FirstOrDefault().Value));

            if (math is null) break;

            functions.Add(math.Captures[0].Value);

            if (math.Groups.Count < 2) continue;

            string stub = GetStubFromType(math.Groups[1].Value);
            data = ReplaceBlockContent(data, math.Index, stub);
        }

        var newBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var resultBytes = InitByteArrayUtf8(arrayOrigBytes.Length);
        Array.Copy(newBytes, resultBytes, newBytes.Length);

        record.Write(resultBytes);
    }

    private string GetStubFromType(string type)
    {
        return type switch
        {//(0_o) return type $"{\n\treturn ({type})0;\n}" (o_0)
            "float4x4" => "{\n\treturn (float4x4)0;\n}",
            "float3x3" => "{\n\treturn (float3x3)0;\n}",
            "float2x2" => "{\n\treturn (float2x2)0;\n}",
            "float4" => "{\n\treturn (float4)0;\n}",
            "float3" => "{\n\treturn (float3)0;\n}",
            "float2" => "{\n\treturn (float2)0;\n}",
            "float" => "{\n\treturn (float)0;\n}",
            "void" => "{ }",
            "OutPixel" => "{\tOutPixel res;\r\n\tres.shadowmap_data = float4(1.0f, 1.0f, 1.0f, 1.0f);\r\n\treturn res;\n}",
            _ => "{ }"
        };
    }

    private string ReplaceBlockContent(string text, int nameIndex, string stub)
    {
        if (nameIndex < 0)
            return text;

        int openBrace = text.IndexOf('{', nameIndex);
        if (openBrace < 0)
            return text;

        int depth = 0;
        int closeBrace = -1;

        for (int i = openBrace; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') depth--;

            if (depth == 0)
            {
                closeBrace = i;
                break;
            }
        }

        if (closeBrace < 0)
            return text;

        return text.Substring(0, openBrace) +
                stub +
               text.Substring(closeBrace + 1);
    }

    private byte[] InitByteArrayUtf8(int size)
    {
        var bytes = new byte[size];

        // BOM UTF-8
        bytes[0] = 0xEF;
        bytes[1] = 0xBB;
        bytes[2] = 0xBF;

        for (int i = 3; i < size; i++)
        {
            bytes[i] = 0x20;
        }

        return bytes;
    }


    private bool HasTargetExtension(string fileName) =>
        extensions.Any(ext =>
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));


    public void Apply(DirectoryNode root)
    {
        CollectFileNodesRecursively(root);

        foreach (var file in fileNodes)
        {
            TryPatchFile(file);
        }
    }
}