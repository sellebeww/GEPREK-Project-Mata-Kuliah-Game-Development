// Observe the real Opening coroutine. Auto-advance dialogue; never call NewGame/save.
var gm = Geprek.Core.GameManager.Instance;
// Match the Phase 1 capture: apply day-one upgrade visibility before the intro.
gm.Progress.tutorialSeen = true;
gm.BeginDay();
gm.BackToMenu();
var director = UnityEngine.Object.FindAnyObjectByType<Geprek.Core.CutsceneDirector>();
var ui = UnityEngine.Object.FindAnyObjectByType<Geprek.UI.UIRoot>();
var card = (UnityEngine.RectTransform)ui.transform.Find("Cutscene/TitleCard");
var group = card.GetComponent<UnityEngine.CanvasGroup>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var elapsedField = typeof(Geprek.UI.UIRoot).GetField("_titleElapsed", flags);
var output = System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,
    "Screenshots/VisualAudit_Phase2");
System.IO.Directory.CreateDirectory(output);

// Same composited Game-view encoder used by the MCP capture_game_view tool.
System.Type captureType = null;
foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
{
    captureType = assembly.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands");
    if (captureType != null) break;
}
var encode = captureType?.GetMethod("EncodeScreenToPng", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
if (encode == null) throw new System.InvalidOperationException("MCP screen capture encoder unavailable.");
System.Action<string> Capture = file =>
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, file),
        (byte[])encode.Invoke(null, new object[] { 1600, 900 }));

bool titleSeen = false, enterCaptured = false, holdCaptured = false, exitCaptured = false;
float lastElapsed = -1f;
double deadline = UnityEditor.EditorApplication.timeSinceStartup + 30;
var samples = new System.Collections.Generic.List<object>();
System.Action<string, string, float> onTitle = (title, subtitle, duration) =>
{
    if (title == "GEPREK!") titleSeen = true;
};
director.TitleShown += onTitle;
UnityEditor.EditorApplication.CallbackFunction observe = null;
System.Action Cleanup = () =>
{
    UnityEditor.EditorApplication.update -= observe;
    if (director != null) director.TitleShown -= onTitle;
};
observe = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.timeSinceStartup > deadline)
    {
        Cleanup();
        return;
    }
    if (director.IsPlaying) director.Advance();
    if (!titleSeen) return;
    float elapsed = (float)elapsedField.GetValue(ui);
    if (elapsed == lastElapsed) return;
    lastElapsed = elapsed;
    samples.Add(new { elapsed, alpha = group.alpha, y = card.anchoredPosition.y,
        active = card.gameObject.activeInHierarchy });
    if (!enterCaptured && elapsed >= 0.10f && elapsed < 0.25f)
    {
        Capture("warung_title_enter.png");
        enterCaptured = true;
    }
    if (!holdCaptured && elapsed >= 0.40f && group.alpha >= 0.999f)
    {
        Capture("warung_title_after.png");
        var cam = UnityEngine.Camera.main;
        var state = new { position = new[] { cam.transform.position.x, cam.transform.position.y, cam.transform.position.z },
            size = cam.orthographicSize, aspect = cam.aspect, width = cam.pixelWidth, height = cam.pixelHeight,
            bannerAlpha = group.alpha, bannerY = card.anchoredPosition.y };
        System.IO.File.WriteAllText(System.IO.Path.Combine(output, "title_capture_state.json"),
            Newtonsoft.Json.JsonConvert.SerializeObject(state, Newtonsoft.Json.Formatting.Indented));
        holdCaptured = true;
    }
    if (!exitCaptured && elapsed >= 2.62f && elapsed < 2.8f && group.alpha < 0.99f)
    {
        Capture("warung_title_exit.png");
        exitCaptured = true;
    }
};
UnityEditor.EditorApplication.update += observe;
typeof(Geprek.Core.GameManager).GetMethod("SetState", flags).Invoke(gm, new object[] { Geprek.Core.GameState.Cutscene });
director.Play(Geprek.Core.CutsceneId.Opening, () =>
{
    Cleanup();
    samples.Add(new { elapsed = 2.8f, alpha = group.alpha, y = card.anchoredPosition.y,
        active = card.gameObject.activeInHierarchy });
    var result = new { titleSeen, enterCaptured, holdCaptured, exitCaptured,
        completed = !director.IsPlaying, hidden = !card.gameObject.activeInHierarchy, samples };
    System.IO.File.WriteAllText(System.IO.Path.Combine(output, "title_animation_trace.json"),
        Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
});
return "Capturing actual Opening frames and animation trace asynchronously.";
