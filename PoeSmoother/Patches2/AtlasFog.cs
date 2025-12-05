using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class AtlasFog : IPatch
{
    public string Name => "Atlas Fog Patch";
    public object Description => "Removes fog from the Atlas.";

    public void Apply(DirectoryNode root)
    {
        // go to metadata/materials/environment/worldmap/worldmap_fogofwar.fxgraph
        foreach (var d in root.Children)
        {
            if (d is DirectoryNode dir && dir.Name == "metadata")
            {
                foreach (var d1 in dir.Children)
                {
                    if (d1 is DirectoryNode subDir && subDir.Name == "materials")
                    {
                        foreach (var d2 in subDir.Children)
                        {
                            if (d2 is DirectoryNode subDir2 && subDir2.Name == "environment")
                            {
                                foreach (var d3 in subDir2.Children)
                                {
                                    if (d3 is DirectoryNode subDir3 && subDir3.Name == "worldmap")
                                    {
                                        foreach (var d4 in subDir3.Children)
                                        {
                                            if (d4 is FileNode file && file.Name == "worldmap_fogofwar.fxgraph")
                                            {
                                                var record = file.Record;
                                                var bytes = record.Read();
                                                string data = System.Text.Encoding.Unicode.GetString(bytes.ToArray());

                                                string nodesPattern = @"""nodes"":\s*\[[\s\S]*?\],";
                                                string nodesReplacement = "\"nodes\": [],";
                                                data = System.Text.RegularExpressions.Regex.Replace(data, nodesPattern, nodesReplacement);

                                                string linksPattern = @"""links"":\s*\[[\s\S]*?\],";
                                                string linksReplacement = "\"links\": [],";
                                                data = System.Text.RegularExpressions.Regex.Replace(data, linksPattern, linksReplacement);

                                                var newBytes = System.Text.Encoding.Unicode.GetBytes(data);
                                                record.Write(newBytes);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
