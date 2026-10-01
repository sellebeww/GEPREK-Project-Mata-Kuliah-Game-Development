// Repeatable visual comparison using real customer prefabs. Play Mode only;
// intentionally staged patience values, not a complete gameplay-flow test.
if (!UnityEngine.Application.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
var gm = Geprek.Core.GameManager.Instance;
gm.Progress.tutorialSeen = true;
foreach (var recipe in gm.Database.recipes)
    if (recipe != null && recipe.unlockArc <= 1 && recipe.unlockCost <= 0 && !recipe.fromParents
        && recipe.unlockLevel <= 1 && !gm.Progress.HasRecipe(recipe.id)) gm.Progress.unlockedRecipes.Add(recipe.id);
gm.BeginDay();
UnityEngine.Time.timeScale = 0f;
var spawner = gm.ActiveBusiness.Spawner;
spawner.StopDay();
spawner.ClearAll();
var previous = UnityEngine.GameObject.Find("VisualAssetPreview");
if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
var preview = new UnityEngine.GameObject("VisualAssetPreview");
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
    "Assets/_Project/Prefabs/Characters/Customer.prefab");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var values = new[] { 0.9f, 0.45f, 0.1f };
var samples = new System.Collections.Generic.List<object>();
for (int i = 0; i < values.Length; i++)
{
    var seat = UnityEngine.GameObject.Find("Locations/Warung/Dining/Meja" + (i + 1) + "/Seat")
        .GetComponent<Geprek.Customers.Seat>();
    var go = UnityEngine.Object.Instantiate(prefab, seat.SitPosition, UnityEngine.Quaternion.identity, preview.transform);
    go.name = "CustomerPreview" + i;
    var customer = go.GetComponent<Geprek.Customers.Customer>();
    customer.Init(spawner, gm.Database.customerTypes[i], gm);
    customer.GoToSeat(seat, System.Array.Empty<UnityEngine.Vector3>());
    typeof(Geprek.Customers.Customer).GetMethod("PlaceOrder", flags).Invoke(customer, null);
    typeof(Geprek.Customers.Customer).GetField("_patienceMax", flags).SetValue(customer, 100f);
    typeof(Geprek.Customers.Customer).GetField("_patience", flags).SetValue(customer, 100f * values[i]);
    var bubble = go.GetComponentInChildren<Geprek.Customers.OrderBubble>(true);
    bubble.transform.localScale = UnityEngine.Vector3.one;
    bubble.ShowOrder(gm.Database.GetRecipe("geprek_original").icon);
    bubble.SetPatience(values[i]);
    go.GetComponent<Geprek.World.SortingByY>().Apply();
    samples.Add(new { name = go.name, patience = values[i], x = go.transform.position.x, y = go.transform.position.y });
}
var cam = UnityEngine.Camera.main;
cam.transform.position = new UnityEngine.Vector3(-0.07777786f, -0.149999857f, -10f);
UnityEngine.Canvas.ForceUpdateCanvases();
return new { samples, camera = cam.transform.position.ToString(), size = cam.orthographicSize };
