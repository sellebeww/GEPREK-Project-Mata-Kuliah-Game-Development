// Run through Unity MCP eval_file. Only trims transparent margins and resamples
// generated PNGs with nearest-neighbour; no artwork is drawn by this exporter.
if (UnityEngine.Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var project = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var source = System.IO.Path.Combine(project, "ArtSource/Generated/WarungPhase2");
var output = System.IO.Path.Combine(project, "Assets/Sprites/Warung");
System.IO.Directory.CreateDirectory(output);
var specs = new[] {
    ("mood_happy", 128, 128, 6, true), ("mood_waiting", 128, 128, 6, true),
    ("mood_angry", 128, 128, 6, true), ("order_bubble", 128, 96, 2, false),
    ("patience_frame", 128, 32, 1, false), ("patience_fill", 128, 16, 0, false),
    ("spice_shelf", 128, 128, 2, true), ("menu_board", 128, 160, 2, true),
    ("day_banner", 512, 128, 1, false)
};
var manifest = new System.Collections.Generic.List<object>();
foreach (var spec in specs)
{
    var input = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, false);
    var bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(source, spec.Item1 + ".png"));
    if (!UnityEngine.ImageConversion.LoadImage(input, bytes)) throw new System.Exception("Invalid PNG: " + spec.Item1);
    var pixels = input.GetPixels32();
    int x0 = input.width, y0 = input.height, x1 = -1, y1 = -1;
    for (int y = 0; y < input.height; y++)
        for (int x = 0; x < input.width; x++)
            // Generated sources can contain isolated near-invisible alpha specks.
            // Use the visible silhouette for the crop, retaining edge alpha inside it.
            if (pixels[y * input.width + x].a >= 16)
            { x0 = System.Math.Min(x0, x); y0 = System.Math.Min(y0, y); x1 = System.Math.Max(x1, x); y1 = System.Math.Max(y1, y); }
    if (x1 < x0) throw new System.Exception("Empty PNG: " + spec.Item1);
    x0 = System.Math.Max(0, x0 - 2); y0 = System.Math.Max(0, y0 - 2);
    x1 = System.Math.Min(input.width - 1, x1 + 2); y1 = System.Math.Min(input.height - 1, y1 + 2);
    int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
    int w = spec.Item2, h = spec.Item3, pad = spec.Item4, dw = w - pad * 2, dh = h - pad * 2;
    if (spec.Item5)
    {
        float fit = System.Math.Min((float)dw / cw, (float)dh / ch);
        dw = UnityEngine.Mathf.RoundToInt(cw * fit); dh = UnityEngine.Mathf.RoundToInt(ch * fit);
    }
    int dx = (w - dw) / 2, dy = (h - dh) / 2;
    var result = new UnityEngine.Color32[w * h];
    for (int y = 0; y < dh; y++)
        for (int x = 0; x < dw; x++)
        {
            int sx = x0 + System.Math.Min(cw - 1, (int)((x + 0.5f) * cw / dw));
            int sy = y0 + System.Math.Min(ch - 1, (int)((y + 0.5f) * ch / dh));
            result[(dy + y) * w + dx + x] = pixels[sy * input.width + sx];
        }
    var final = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBA32, false);
    final.SetPixels32(result); final.Apply();
    var target = System.IO.Path.Combine(output, spec.Item1 + ".png");
    System.IO.File.WriteAllBytes(target, UnityEngine.ImageConversion.EncodeToPNG(final));
    manifest.Add(new { name = spec.Item1, sourceWidth = input.width, sourceHeight = input.height,
        crop = new[] { x0, y0, cw, ch }, width = w, height = h, padding = pad,
        preserveAspect = spec.Item5, cropAlphaThreshold = 16, alpha = result.Any(c => c.a == 0), method = "nearest-neighbour" });
    UnityEngine.Object.DestroyImmediate(input); UnityEngine.Object.DestroyImmediate(final);
}
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
System.IO.File.WriteAllText(System.IO.Path.Combine(source, "export_manifest.json"),
    Newtonsoft.Json.JsonConvert.SerializeObject(manifest, Newtonsoft.Json.Formatting.Indented));
return manifest;
