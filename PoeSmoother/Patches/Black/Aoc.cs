using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Aoc : IPatch
{
    public string Name => "Aoc Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".aoc",
    };

    private List<FileNode> fileNodes = [];

    private readonly string[] _functions = {
        "ParticleEffects",
        "TrailsEffects",
        "DecalEvents",
        "ScreenShake",
        "Lights",
        "WindEvents",
        "EffectPack",
        "SoundEvents",
        "SkinMesh",
        "FixedMesh",
    };

    public static string ClearBlockContent(string text, string blockName)
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

        return text.Substring(0, openBrace + 1) +
               "\n}" +
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
        string data = System.Text.Encoding.Unicode.GetString(bytes.ToArray());

        foreach (var func in _functions)
        {
            data = ClearBlockContent(data, func);
        }

        var newBytes = System.Text.Encoding.Unicode.GetBytes(data);

        record.Write(newBytes);
    }

    private bool HasTargetExtension(string fileName) =>
        extensions.Any(ext =>
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    private DirectoryNode? FindDirectory(DirectoryNode start, params string[] path)
    {
        DirectoryNode? current = start;

        foreach (var name in path)
        {
            current = current.Children
                .OfType<DirectoryNode>()
                .FirstOrDefault(c => c.Name == name);

            if (current == null)
                return null;
        }

        return current;
    }

    public void Apply(DirectoryNode root)
    {
        var metadataDir = FindDirectory(root, "metadata");

        if (metadataDir is null)
            return;

        CollectFileNodesRecursively(metadataDir);

        foreach (var file in fileNodes)
        {
            TryPatchFile(file);
        }
    }
}