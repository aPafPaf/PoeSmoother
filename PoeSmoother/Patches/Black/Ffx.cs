using LibBundle3.Nodes;
using System.Text.RegularExpressions;

namespace PoeSmoother.Patches;

public class Ffx : IPatch
{
    public string Name => "Ffx Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".ffx",
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

        string pattern = @"(FRAGMENT\s+\w+.*?\{\{)(.*?)(\}\})";

        string result = Regex.Replace(
            data,
            pattern,
            m => $"{m.Groups[1].Value} {m.Groups[3].Value}",
            RegexOptions.Singleline);

        var newBytes = System.Text.Encoding.Unicode.GetBytes(result);
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