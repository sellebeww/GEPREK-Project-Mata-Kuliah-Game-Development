// Run through Unity MCP eval_file while Main is in Play Mode at DayOperating.
// Exercises visibility, reachability and banner transitions without advancing the day.
var gm = Geprek.Core.GameManager.Instance;
if (!UnityEngine.Application.isPlaying || gm == null || !gm.IsOperating)
    throw new System.InvalidOperationException("Run in Main, DayOperating, with the clock frozen.");

var checks = new System.Collections.Generic.List<object>();
int passed = 0, failed = 0;
System.Action<string, bool, string> Check = (name, ok, detail) =>
{
    checks.Add(new { name, passed = ok, detail });
    if (ok) passed++; else failed++;
};
var root = UnityEngine.GameObject.Find("Locations/Warung").transform;
System.Func<string, UnityEngine.Transform> At = path => root.Find(path);
string[] counter = { "SumberAyam", "SumberNasi", "Penggorengan1", "Penggorengan2", "Cobek1",
    "SumberSambalMerah", "SumberSambalIjo", "SumberTelur", "SumberTahu", "SumberLalapan", "SumberKeju" };
foreach (string name in counter)
{
    var t = At("Kitchen/" + name);
    var body = t.Find("Body").GetComponent<UnityEngine.SpriteRenderer>();
    Check(name + " grid/baseline/scale", UnityEngine.Mathf.Abs(t.localPosition.y - 2.0625f) < 0.0001f
        && UnityEngine.Mathf.Abs(t.localPosition.x * 16f - UnityEngine.Mathf.Round(t.localPosition.x * 16f)) < 0.0001f
        && body.transform.localScale == new UnityEngine.Vector3(0.85f, 0.85f, 1f)
        && body.sprite.pivot.y == 0f, t.localPosition.ToString("F4"));
}
for (int i = 1; i < counter.Length; i++)
    Check("Counter spacing " + i, UnityEngine.Mathf.Abs(At("Kitchen/" + counter[i]).localPosition.x
        - At("Kitchen/" + counter[i - 1]).localPosition.x - 1.375f) < 0.0001f, "1.375 world units");

foreach (UnityEngine.Transform table in At("Dining"))
{
    var left = table.Find("StoolL").GetComponent<UnityEngine.SpriteRenderer>();
    var right = table.Find("StoolR").GetComponent<UnityEngine.SpriteRenderer>();
    var plate = table.Find("NomorMeja").GetComponent<UnityEngine.SpriteRenderer>();
    var label = plate.GetComponentInChildren<Geprek.World.WorldLabel>(true);
    label.RefreshSorting();
    var text = label.GetComponent<UnityEngine.MeshRenderer>();
    Check(table.name + " matching chairs", left.sprite == right.sprite && left.color == right.color, left.sprite.name);
    Check(table.name + " number above plaque", text.sortingOrder > plate.sortingOrder
        && text.sortingLayerID == plate.sortingLayerID,
        "text=" + text.sortingOrder + ", plaque=" + plate.sortingOrder);
    Check(table.name + " glyph proportions", UnityEngine.Mathf.Abs(label.transform.lossyScale.x
        - label.transform.lossyScale.y) < 0.0001f, label.transform.lossyScale.ToString());
}

foreach (string path in new[] { "CustomerSpawner/PembatasAwal", "CustomerSpawner/PembatasAkhir", "WallDecor/JamDinding" })
    Check(path + " hidden", !At(path).gameObject.activeInHierarchy, "disabled decoration");
var queue = new UnityEditor.SerializedObject(At("CustomerSpawner").GetComponent<Geprek.Customers.CustomerSpawner>())
    .FindProperty("queueSpots");
Check("Queue references retained", queue.arraySize == 3, "3 waypoints");
for (int i = 0; i < 3; i++)
{
    var point = At("CustomerSpawner/Queue" + i);
    var mark = At("CustomerSpawner/QueueMark" + i).GetComponent<UnityEngine.SpriteRenderer>();
    Check("Queue " + i + " logic and subtle marker", queue.GetArrayElementAtIndex(i).objectReferenceValue == point
        && point.GetComponent<UnityEngine.Renderer>() == null && mark.sprite.name == "fx_ring" && mark.color.a <= 0.2f,
        "waypoint preserved; ring alpha=" + mark.color.a);
}

var fryer2 = At("Kitchen/Penggorengan2").GetComponent<Geprek.Stations.UpgradeToggle>();
System.Func<bool> LockedVisualsHidden = () =>
{
    foreach (var r in fryer2.GetComponentsInChildren<UnityEngine.Renderer>(true))
        if (r.enabled && r.gameObject.activeInHierarchy) return false;
    return true;
};
Check("Locked fryer: no orphan label", LockedVisualsHidden(), "all child renderers effectively hidden");
fryer2.SetActiveState(true);
Check("Unlock restores body and label", fryer2.transform.Find("Body").gameObject.activeInHierarchy
    && fryer2.transform.Find("NamePlate/Label").gameObject.activeInHierarchy
    && fryer2.GetComponent<UnityEngine.Collider2D>().enabled, "visibility + collision");
Check("Unlock keeps idle effects hidden", !fryer2.transform.Find("Sizzle").gameObject.activeInHierarchy
    && !fryer2.transform.Find("Bar/Back").GetComponent<UnityEngine.SpriteRenderer>().enabled
    && !fryer2.transform.Find("Highlight").GetComponent<UnityEngine.SpriteRenderer>().enabled,
    "no idle sizzle/bar/highlight");

var player = UnityEngine.Object.FindAnyObjectByType<Geprek.Player.PlayerController>();
var oldPosition = player.transform.position;
var held = player.Carry.Release();
var rb = player.GetComponent<UnityEngine.Rigidbody2D>();
var oldInterpolation = rb.interpolation;
rb.interpolation = UnityEngine.RigidbodyInterpolation2D.None;
try
{
    var reach = new[] {
        ("SumberAyam", ""), ("SumberNasi", ""), ("Penggorengan1", "ayam_mentah"),
        ("Penggorengan2", "ayam_mentah"), ("Cobek1", "ayam_goreng"),
        ("SumberSambalMerah", ""), ("MejaPenyajian", "ayam_geprek"), ("Kasir", "") };
    foreach (var entry in reach)
    {
        var station = At("Kitchen/" + entry.Item1).GetComponent<Geprek.Stations.StationBase>();
        player.Carry.Clear();
        if (entry.Item2 != "") player.Carry.TryTake(Geprek.Data.CarriedItem.FromItem(gm.Database.GetItem(entry.Item2)));
        player.transform.position = station.transform.position + UnityEngine.Vector3.down * 0.65f;
        rb.position = player.transform.position;
        UnityEngine.Physics2D.SyncTransforms();
        player.Interactor.SetFacing(UnityEngine.Vector2.up);
        player.Interactor.Scan();
        Check("Reach " + entry.Item1, object.ReferenceEquals(player.Interactor.Current, station),
            player.Interactor.Current != null ? player.Interactor.Current.Transform.name : "no target");
        bool clear = true;
        var feet = player.GetComponent<UnityEngine.CapsuleCollider2D>();
        foreach (var obstacle in root.GetComponentsInChildren<UnityEngine.Collider2D>())
            if (obstacle.enabled && !obstacle.isTrigger && feet.Distance(obstacle).isOverlapped) clear = false;
        Check("Approach clear " + entry.Item1, clear, "player capsule vs active warung colliders");
    }
}
finally
{
    player.Carry.Clear();
    if (held != null) player.Carry.TryTake(held);
    rb.position = oldPosition;
    player.transform.position = oldPosition;
    rb.interpolation = oldInterpolation;
    UnityEngine.Physics2D.SyncTransforms();
    player.Interactor.Scan();
    fryer2.SetActiveState(false);
}
Check("Relock hides text again", LockedVisualsHidden(), "upgrade toggle round trip");

var ui = UnityEngine.Object.FindAnyObjectByType<Geprek.UI.UIRoot>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
System.Action<string, object[]> Invoke = (name, args) => typeof(Geprek.UI.UIRoot).GetMethod(name, flags).Invoke(ui, args);
var card = (UnityEngine.RectTransform)ui.transform.Find("Cutscene/TitleCard");
var group = card.GetComponent<UnityEngine.CanvasGroup>();
try
{
    Invoke("OnCutsceneActiveChanged", new object[] { true });
    Invoke("OnCutsceneTitle", new object[] { "GEPREK!", "Hari 1 — Awal Mula", 2.8f });
    Check("Title is overlay UI", ui.GetComponent<UnityEngine.Canvas>().renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay
        && !group.blocksRaycasts, "Canvas, no raycast blocking");
    Check("Banner starts hidden above screen", group.alpha == 0f && card.anchoredPosition.y > 0f, card.anchoredPosition.ToString());
    Invoke("UpdateTitleBanner", new object[] { 0.14f });
    Check("Banner slides/fades in", group.alpha > 0f && group.alpha < 1f && card.anchoredPosition.y < 80f,
        "alpha=" + group.alpha + ", y=" + card.anchoredPosition.y);
    Invoke("UpdateTitleBanner", new object[] { 0.20f });
    UnityEngine.Canvas.ForceUpdateCanvases();
    var corners = new UnityEngine.Vector3[4];
    var band = new UnityEngine.Vector3[4];
    card.GetWorldCorners(corners);
    ((UnityEngine.RectTransform)ui.transform.Find("Cutscene/BarTop")).GetWorldCorners(band);
    Check("Banner stays within upper cinematic band", group.alpha == 1f && corners[0].y >= band[0].y
        && corners[1].y <= band[1].y, "clear of kitchen and characters");
    Invoke("UpdateTitleBanner", new object[] { 2.32f });
    Check("Banner slides/fades out", group.alpha > 0f && group.alpha < 1f && card.anchoredPosition.y > -7f,
        "alpha=" + group.alpha + ", y=" + card.anchoredPosition.y);
    Invoke("UpdateTitleBanner", new object[] { 0.20f });
    Check("Banner completes and hides", !card.gameObject.activeSelf && group.alpha == 0f, "no lingering title");
}
finally { Invoke("OnCutsceneActiveChanged", new object[] { false }); }

var report = new { passed, failed, checks };
var file = System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,
    "Screenshots/VisualAudit_Phase1/verification.json");
System.IO.File.WriteAllText(file, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return report;
