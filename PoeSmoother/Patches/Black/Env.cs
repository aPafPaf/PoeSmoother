using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Env : IPatch
{
    public string Name => "Env Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".env",
    };

    private List<FileNode> fileNodes = [];

    private readonly string[] _functions = {
        "\"player_light\":",
        "\"environment_mapping\":",
        "\"fog\":",
        "\"area\":",
        "\"water\":",
        "\"post_transform\":",
        "\"audio\":",
        "\"global_illumination\":",
        "\"effect_spawner\":",
        "\"post_processing\":",
    };

    private string ReplaceBlockContent(string text, string blockName, string blockContent)
    {
        int nameIndex = text.IndexOf(blockName, StringComparison.Ordinal);
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
                blockContent +
               text.Substring(closeBrace + 1);
    }

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
                    if (HasTargetExtension(fileNode.Name))
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
        string data = System.Text.Encoding.Unicode.GetString(arrayOrigBytes);

        data = data.Replace("\"shadows_enabled\": true", "\"shadows_enabled\": false");

        foreach (var func in _functions)
        {
            data = ReplaceBlockContent(data, func, "{}");
        }

        var newBytes = System.Text.Encoding.Unicode.GetBytes(data);
        var resultBytes = InitByteArray(arrayOrigBytes.Length);
        Array.Copy(newBytes, resultBytes, newBytes.Length);

        record.Write(resultBytes);
    }

    private byte[] InitByteArray(int Size)
    {
        var bytes = new byte[Size];

        // BOM UTF-16 LE
        bytes[0] = 0xFF;
        bytes[1] = 0xFE;

        for (int i = 2; i + 1 < Size; i += 2)
        {
            bytes[i] = 0x20;      // ' ' (space)
            bytes[i + 1] = 0x00;  // UTF-16 LE low byte
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