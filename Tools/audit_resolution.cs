// Set SessionState Geprek.Audit.Width/Height before running; use width=0 to restore.
var assembly = typeof(UnityEditor.Editor).Assembly;
var viewType = assembly.GetType("UnityEditor.GameView");
var view = UnityEditor.EditorWindow.GetWindow(viewType);
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var selected = viewType.GetProperty("selectedSizeIndex", flags);
var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
var sizes = singleton.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);
var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { System.Enum.Parse(groupType, "Standalone") });
int width = UnityEditor.SessionState.GetInt("Geprek.Audit.Width", 0);
int original = UnityEditor.SessionState.GetInt("Geprek.Audit.OriginalSize", -1);
if (original < 0)
{
    original = (int)selected.GetValue(view);
    UnityEditor.SessionState.SetInt("Geprek.Audit.OriginalSize", original);
    UnityEditor.SessionState.SetInt("Geprek.Audit.CustomCount", (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null));
}
if (width == 0)
{
    selected.SetValue(view, original);
    int initial = UnityEditor.SessionState.GetInt("Geprek.Audit.CustomCount", 0);
    int count = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
    int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
    while(count > initial) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtIn + --count });
    UnityEditor.SessionState.EraseInt("Geprek.Audit.OriginalSize");
    return "Original Game View restored";
}
int height = UnityEditor.SessionState.GetInt("Geprek.Audit.Height", 720);
var sizeType = assembly.GetType("UnityEditor.GameViewSize");
var modeType = assembly.GetType("UnityEditor.GameViewSizeType");
var size = System.Activator.CreateInstance(sizeType, new object[] { System.Enum.Parse(modeType, "FixedResolution"), width, height, "Geprek Audit" });
group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
selected.SetValue(view, index);
view.Repaint();
return new { width, height, index };
