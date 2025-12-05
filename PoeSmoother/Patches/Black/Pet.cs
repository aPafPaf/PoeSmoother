using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Pet : IPatch
{
    public string Name => "Pet Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".pet",
        ".trl",
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
        int originalLength = file.Record.Size;
        var newBytes = new byte[originalLength];

        // BOM UTF-16 LE
        newBytes[0] = 0xFF;
        newBytes[1] = 0xFE;

        // '0'
        newBytes[2] = 0x30;
        newBytes[3] = 0x00;
        
        for (int i = 4; i + 1 < originalLength; i += 2)
        {
            newBytes[i] = 0x20;      // ' ' (space)
            newBytes[i + 1] = 0x00;  // UTF-16 LE low byte
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