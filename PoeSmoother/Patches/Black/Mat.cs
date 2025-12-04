using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Mat : IPatch
{
    public string Name => "Pet Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".mat",
    };

    private List<FileNode> fileNodes = [];

    private string matContent = @"{
""version"":4,
""defaultgraph"": {
 ""version"":3
 }
}  ";

    private byte[]? bytesContent = null;

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
        file.Record.Write(bytesContent);
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
        if (bytesContent is null)
        {
            bytesContent = System.Text.Encoding.Unicode.GetBytes(matContent);
        }

        CollectFileNodesRecursively(root);

        foreach (var file in fileNodes)
        {
            TryPatchFile(file);
        }
    }
}