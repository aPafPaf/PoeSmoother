using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Mat : IPatch
{
    public string Name => "Mat Patch (Experimental)";
    public object Description => "Black.";

    private readonly string[] extensions = {
        ".mat",
    };

    private List<FileNode> fileNodes = [];

    private string matContent = "{\r\n\"version\":4,\r\n\"defaultgraph\": {\r\n \"version\":3\r\n }\r\n}";

    private byte[]? bytesContent = null;

    public Mat()
    {
        bytesContent = System.Text.Encoding.UTF8.GetBytes(matContent);
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
        var size = file.Record.Size - bytesContent.Length;
        if (size < 1) return;

        var newBytes = Enumerable.Repeat((byte)0x20, file.Record.Size).ToArray();
        Array.Copy(bytesContent, newBytes, bytesContent.Length);
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