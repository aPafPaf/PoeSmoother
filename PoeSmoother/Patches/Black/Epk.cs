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
        var newBytes = System.Text.Encoding.Unicode.GetBytes(string.Empty);
        file.Record.Write(newBytes);
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