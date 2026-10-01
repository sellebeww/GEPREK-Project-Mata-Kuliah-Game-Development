// Focused integration checks for the new art. Run after stage_visual_phase2.cs.
if (!UnityEngine.Application.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
var checks = new System.Collections.Generic.List<object>();
int failures = 0;
void Check(string name, bool ok) { checks.Add(new { name, passed = ok }); if (!ok) failures++; }
var folder = "Assets/Sprites/Warung/";
var specs = new[] { ("mood_happy",128,128), ("mood_waiting",128,128), ("mood_angry",128,128),
    ("order_bubble",128,96), ("patience_frame",128,32), ("patience_fill",128,16),
    ("spice_shelf",128,128), ("menu_board",128,160), ("day_banner",512,128) };
foreach (var spec in specs)
{
    string path = folder + spec.Item1 + ".png";
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
    var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
    Check(spec.Item1 + ": exact size", sprite != null && sprite.rect.width == spec.Item2 && sprite.rect.height == spec.Item3);
    Check(spec.Item1 + ": PPU128/Point/uncompressed/no mipmaps",
        importer != null && importer.spritePixelsPerUnit == 128f && importer.filterMode == UnityEngine.FilterMode.Point
        && importer.textureCompression == UnityEditor.TextureImporterCompression.Uncompressed && !importer.mipmapEnabled);
    var png = new UnityEngine.Texture2D(2,2);
    UnityEngine.ImageConversion.LoadImage(png, System.IO.File.ReadAllBytes(path));
    var pixels = png.GetPixels32();
    Check(spec.Item1 + ": real alpha", pixels.Any(c => c.a == 0) && pixels.Any(c => c.a == 255));
    UnityEngine.Object.DestroyImmediate(png);
}
var preview = UnityEngine.GameObject.Find("VisualAssetPreview");
var samples = preview.GetComponentsInChildren<Geprek.Customers.OrderBubble>(true);
Check("Three staged customers", samples.Length == 3);
string[] moods = { "mood_happy", "mood_waiting", "mood_angry" };
float[] patience = { .9f, .45f, .1f };
for (int i = 0; i < samples.Length; i++)
{
    var root = samples[i].transform;
    var mood = root.Find("Mood").GetComponent<UnityEngine.SpriteRenderer>();
    Check("Patience " + patience[i] + ": " + moods[i], mood.sprite.name == moods[i]);
    foreach (var sr in root.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))
        Check("Customer " + i + ": overlay " + sr.name, sr.sortingOrder >= 1000);
}
var bubble = samples[0];
var bar = bubble.GetComponentInChildren<Geprek.World.WorldProgressBar>(true);
var fill = bar.transform.Find("Fill").GetComponent<UnityEngine.SpriteRenderer>();
var frame = bar.transform.Find("Back").GetComponent<UnityEngine.SpriteRenderer>();
var positions = new System.Collections.Generic.List<object>();
foreach (float value in new[] { -1f, 0f, .1f, .45f, .9f, 1f, 2f })
{
    bar.SetValue(value);
    float clamped = UnityEngine.Mathf.Clamp01(value);
    Check("Bar " + value + ": clamped width", UnityEngine.Mathf.Abs(fill.transform.localScale.x - .55f * clamped) < .0001f);
    float left = fill.transform.localPosition.x - fill.transform.localScale.x * .5f;
    Check("Bar " + value + ": fixed left edge", UnityEngine.Mathf.Abs(left + .275f) < .0001f);
    Check("Bar " + value + ": inside frame", fill.bounds.min.x >= frame.bounds.min.x - .0001f
        && fill.bounds.max.x <= frame.bounds.max.x + .0001f
        && fill.bounds.min.y >= frame.bounds.min.y - .0001f && fill.bounds.max.y <= frame.bounds.max.y + .0001f);
    positions.Add(new { value, left, width = fill.transform.localScale.x, color = fill.color.ToString() });
}
var recipe = Geprek.Core.GameManager.Instance.Database.GetRecipe("geprek_original");
bubble.ShowReaction(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(folder + "mood_happy.png"));
Check("Reaction hides bar and second mood", !fill.enabled && !frame.enabled
    && !bubble.transform.Find("Mood").GetComponent<UnityEngine.SpriteRenderer>().enabled);
bubble.Hide();
Check("Hide deactivates bubble", !bubble.gameObject.activeSelf);
bubble.ShowOrder(recipe.icon); bubble.SetPatience(.9f);
Check("ShowOrder restores bar", fill.enabled && frame.enabled && bubble.gameObject.activeSelf);

var ui = UnityEngine.Object.FindAnyObjectByType<Geprek.UI.UIRoot>();
var card = ui.transform.Find("Cutscene/TitleCard").GetComponent<UnityEngine.UI.Image>();
Check("Title is Canvas UI with sliced artwork", card.sprite.name == "day_banner"
    && card.type == UnityEngine.UI.Image.Type.Sliced && card.GetComponentInParent<UnityEngine.Canvas>(true) != null);
var warung = UnityEngine.GameObject.Find("Locations/Warung").transform;
Check("Spice shelf installed", warung.Find("WallDecor/RakPelengkap").GetComponent<UnityEngine.SpriteRenderer>().sprite.name == "spice_shelf");
Check("Menu board installed", warung.Find("WallDecor/MenuBoard").GetComponent<UnityEngine.SpriteRenderer>().sprite.name == "menu_board");
var result = new { total = checks.Count, failures, checks, positions, staged = true,
    scope = "Phase 2 art integration; complete cooking/service/payment flow belongs to Phase 3." };
System.IO.File.WriteAllText("Screenshots/VisualAudit_Phase2/verification.json",
    Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return new { total = checks.Count, failures };
