using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Camera : IPatch
{
    public string Name => "Camera Patch";
    public object Description => "Allows adjusting the default camera zoom level.";

    public double ZoomLevel { get; set; } = 2.4;

    private readonly string[] _extensions = {
        ".ot",
        ".otc",
    };

    private readonly string[] _functions = {
        "CreateCameraZoomNode",
        "ClearCameraZoomNodes",
        "CreateCameraLookAtNode",
        "CreateCameraPanNode",
        "ClearCameraPanNode",
        "SetCustomCameraSpeed",
        "RemoveCustomCameraSpeed",
    };

    private void RecursivePatcher(DirectoryNode dir)
    {
        foreach (var d in dir.Children)
        {
            if (d is DirectoryNode childDir)
            {
                RecursivePatcher(childDir);
            }
            else if (d is FileNode file)
            {

                if (Array.Exists(_extensions, ext => file.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    if (file.Name == "character.ot")
                    {
                        continue;
                    }

                    var record = file.Record;
                    var bytes = record.Read();
                    string data = System.Text.Encoding.Unicode.GetString(bytes.ToArray());

                    if (!_functions.Any(func => data.Contains(func)))
                    {
                        continue;
                    }

                    string pattern = @"(?:[\w_]+\.)?(?:" + string.Join("|", _functions.Select(System.Text.RegularExpressions.Regex.Escape)) + @")\s*\([^)]*\)\s*;";
                    data = System.Text.RegularExpressions.Regex.Replace(data, pattern, "");

                    var newBytes = System.Text.Encoding.Unicode.GetBytes(data);
                    record.Write(newBytes);
                }
            }
        }
    }

    public void Apply(DirectoryNode root)
    {
        // go to metadata/
        foreach (var d in root.Children)
        {
            if (d is DirectoryNode dir && dir.Name == "metadata")
            {
                RecursivePatcher(dir);

                // go to metadata/characters/character.ot
                foreach (var dir1 in dir.Children)
                {
                    if (dir1 is DirectoryNode subDir && subDir.Name == "characters")
                    {
                        foreach (var dir2 in subDir.Children)
                        {
                            if (dir2 is FileNode file && file.Name == "character.ot")
                            {
                                var record = file.Record;
                                var bytes = record.Read();
                                string data = System.Text.Encoding.Unicode.GetString(bytes.ToArray());
                                List<string> lines = data.Split("\r\n").ToList();
                                string zoomLevelString = ZoomLevel.ToString().Replace(',', '.');

                                if (data.Contains("CreateCameraZoomNode"))
                                {
                                    int x = lines.FindIndex(line => line.Contains("CreateCameraZoomNode"));
                                    lines[x] = $"\ton_initial_position_set = \"CreateCameraZoomNode(5000.0, 5000.0, {zoomLevelString});\" ";
                                }
                                else
                                {
                                    int index = lines.FindIndex(x => x.Contains("team = 1"));
                                    if (index == -1) continue;
                                    lines.Insert(index + 1, $"\ton_initial_position_set = \"CreateCameraZoomNode(5000.0, 5000.0, {zoomLevelString});\" ");
                                }
                                string newData = string.Join("\r\n", lines);
                                var newBytes = System.Text.Encoding.Unicode.GetBytes(newData);
                                record.Write(newBytes);
                            }
                        }
                    }
                }
            }
        }
    }
}