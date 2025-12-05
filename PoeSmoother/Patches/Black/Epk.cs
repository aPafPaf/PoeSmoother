using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Epk : IPatch
{
    public string Name => "Epk Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".epk",
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
        long originalLength = file.Record.Size;
        var newBytes = new byte[originalLength];

        // UTF-16 LE BOM = FF FE
        var bom = System.Text.Encoding.Unicode.GetPreamble(); // FF FE
        int offset = 0;

        // BOM
        if (originalLength >= bom.Length)
        {
            Array.Copy(bom, 0, newBytes, 0, bom.Length);
            offset = bom.Length;
        }

        for (int i = offset; i + 1 < originalLength; i += 2)
        {
            newBytes[i] = 0x20;     // ' ' (space)
            newBytes[i + 1] = 0x00; // UTF-16 LE low byte
        }

        file.Record.Write(newBytes);
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