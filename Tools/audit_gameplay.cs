// Run with Unity eval_file in an isolated Main Play Mode session.
Geprek.EditorTools.GeprekPreview.Begin(1);
var game = Geprek.Core.GameManager.Instance;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var results = new System.Collections.Generic.List<string>();
System.Action<string, bool> check = (name, ok) => { results.Add((ok ? "PASS " : "FAIL ") + name); };
var ui = UnityEngine.Object.FindFirstObjectByType<Geprek.UI.UIRoot>();
game.Pause();
ui.GetType().GetMethod("OnEscape", flags).Invoke(ui, null);
check("Escape resumes game and time", game.IsOperating && UnityEngine.Time.timeScale == 1);
int money = game.Progress.money;
check("Negative spend rejected", !game.TrySpend(-500) && game.Progress.money == money);
game.AddMoney(int.MaxValue);
check("Money overflow saturates safely", game.Progress.money == int.MaxValue);
game.Progress.money = money;
game.AddMoney(10000);
check("Opening checkpoint saved", game.SaveNow() && Geprek.Save.SaveSystem.Load().money == money);
game.Progress.money = money;
var spawner = game.ActiveBusiness.Spawner;
spawner.GetType().GetMethod("SpawnOne", flags).Invoke(spawner, null);
spawner.StopDay();
var customer = ((System.Collections.Generic.List<Geprek.Customers.Customer>)spawner.GetType().GetField("_active", flags).GetValue(spawner))[0];
customer.transform.position = customer.AssignedSeat.SitPosition;
customer.GetType().GetField("_hasTarget", flags).SetValue(customer, false);
customer.GetType().GetMethod("PlaceOrder", flags).Invoke(customer, null);
var player = UnityEngine.Object.FindFirstObjectByType<Geprek.Player.PlayerController>();
var carry = player.Carry;
carry.Clear();
var fryer = game.ActiveBusiness.GetComponentsInChildren<Geprek.Stations.CookStation>().First();
var prep = game.ActiveBusiness.GetComponentsInChildren<Geprek.Stations.PrepStation>().First();
var plate = game.ActiveBusiness.Plate;
plate.DumpPlate();
carry.TryTake(Geprek.Data.CarriedItem.FromItem(game.Database.GetItem("ayam_mentah")));
fryer.Interact(carry);
check("Raw chicken enters fryer", carry.IsEmpty && fryer.IsBusy);
fryer.Interact(carry);
check("Early pickup blocked", carry.IsEmpty);
// Advance the existing timer to completion; do not replace the cooking result or recipe.
fryer.GetType().GetField("_timer", flags).SetValue(fryer, 100f);
fryer.GetType().GetMethod("TickCooking", flags).Invoke(fryer, null);
fryer.Interact(carry);
check("Fryer gives cooked chicken", carry.Held?.def?.id == "ayam_goreng");
prep.Interact(carry);
prep.HoldTick(carry, 100f);
prep.Interact(carry);
check("Prep gives geprek", carry.Held?.def?.id == "ayam_geprek");
plate.Interact(carry);
foreach (var ingredient in customer.Order.components)
{
    if (ingredient.id == "ayam_geprek") continue;
    carry.TryTake(Geprek.Data.CarriedItem.FromItem(ingredient));
    plate.Interact(carry);
}
check("Requested recipe matches assembled plate", plate.Match == customer.Order);
plate.Interact(carry);
int before = game.Progress.money;
int expected = Geprek.Economy.Payout.Compute(game.Config, customer.Order, customer.Type,
    customer.PatienceLeft, carry.Held.quality, game.PriceBonus, out var outcome, out var satisfaction, game.TodayPriceScale);
customer.Interact(carry);
check("Serving consumes dish and pays once", carry.IsEmpty && game.Today.served == 1 && game.Progress.money == before + expected);
customer.Interact(carry);
check("Repeated serving cannot pay twice", game.Today.served == 1 && game.Progress.money == before + expected);
// Re-stage the same real recipe for visual inspection.
customer.GetType().GetMethod("PlaceOrder", flags).Invoke(customer, null);
player.transform.position = customer.AssignedSeat.ApproachPosition;
UnityEngine.Physics2D.SyncTransforms();
player.Interactor.SetFacing(UnityEngine.Vector2.down);
player.Interactor.Scan();
UnityEngine.Time.timeScale = 0;
check("Selected customer supplies the order card", player.Interactor.Current == customer);
System.IO.Directory.CreateDirectory("Logs");
System.IO.File.WriteAllLines("Logs/audit-gameplay-20261009.txt", results);
return string.Join("\n", results);
