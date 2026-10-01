var log = new System.Text.StringBuilder();
System.Action<string, bool, string> Check = (label, ok, detail) =>
    log.AppendLine((ok ? "LULUS  " : "GAGAL  ") + label + (detail != "" ? "  -> " + detail : ""));

var db = UnityEditor.AssetDatabase.LoadAssetAtPath<Geprek.Data.GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
Check("database termuat", db != null, "");
var cfg = db.config;

Check("jumlah resep = 8", db.recipes.Count == 8, db.recipes.Count.ToString());
Check("jumlah item = 11", db.items.Count == 11, db.items.Count.ToString());
Check("jumlah upgrade = 7", db.upgrades.Count == 7, db.upgrades.Count.ToString());
Check("jumlah tipe pelanggan = 9", db.customerTypes.Count == 9, db.customerTypes.Count.ToString());
Check("3 arc x 5 hari", db.arcs.Count == 3 && db.arcs.TrueForAll(a => a.days.Count == 5), "");

int noIcon = 0, noComp = 0;
foreach (var r in db.recipes) { if (r.icon == null) noIcon++; if (r.components.Count == 0) noComp++; }
Check("semua resep punya ikon", noIcon == 0, noIcon + " tanpa ikon");
Check("semua resep punya bahan", noComp == 0, noComp + " kosong");

int badSkin = 0;
foreach (var c in db.customerTypes) if (c.skin == null || !c.skin.IsValid) badSkin++;
Check("semua pelanggan punya skin lengkap", badSkin == 0, badSkin + " rusak");
Check("skin pemain valid", db.playerSkin != null && db.playerSkin.IsValid, "");
Check("skin ibu & ayah valid", db.motherSkin.IsValid && db.fatherSkin.IsValid, "");

var raw = db.GetItem("ayam_mentah");
Check("ayam mentah -> goreng", raw.cookResult != null && raw.cookResult.id == "ayam_goreng", "");
Check("ayam mentah -> gosong", raw.burnResult != null && raw.burnResult.id == "ayam_gosong", "");
Check("ayam goreng -> geprek", raw.cookResult.prepResult != null && raw.cookResult.prepResult.id == "ayam_geprek", "");
Check("ayam geprek bisa dipiring", raw.cookResult.prepResult.isPlatingComponent, "");
Check("ayam gosong hanya buat dibuang", db.GetItem("ayam_gosong").stage == Geprek.Core.ItemStage.Burnt, "");

var original = db.GetRecipe("geprek_original");
var merah = db.GetRecipe("geprek_sambal_merah");
var geprek = db.GetItem("ayam_geprek");
var nasi = db.GetItem("nasi");
var sambal = db.GetItem("sambal_merah");
var L = new System.Func<Geprek.Data.ItemDef[], System.Collections.Generic.List<Geprek.Data.ItemDef>>(
        a => new System.Collections.Generic.List<Geprek.Data.ItemDef>(a));

Check("piring benar cocok", original.Matches(L(new[]{ geprek, nasi })), "");
Check("urutan bahan tidak berpengaruh", original.Matches(L(new[]{ nasi, geprek })), "");
Check("bahan kurang tidak cocok", !original.Matches(L(new[]{ geprek })), "");
Check("bahan berlebih tidak cocok", !original.Matches(L(new[]{ geprek, nasi, sambal })), "");
Check("resep lain tidak salah cocok", !merah.Matches(L(new[]{ geprek, nasi })), "");
Check("resep 3 bahan cocok", merah.Matches(L(new[]{ sambal, geprek, nasi })), "");
Check("hitung bahan benar", original.MatchedCount(L(new[]{ geprek, sambal })) == 1, "");

Geprek.Core.ServeOutcome o1, o2; float s1, s2;
int pPerfect = Geprek.Economy.Payout.Compute(cfg, merah, null, 0.9f, 1f, 0f, out o1, out s1);
int pLate = Geprek.Economy.Payout.Compute(cfg, merah, null, 0.1f, 1f, 0f, out o2, out s2);
Check("cepat = Perfect", o1 == Geprek.Core.ServeOutcome.Perfect, o1.ToString());
Check("lambat = Late", o2 == Geprek.Core.ServeOutcome.Late, o2.ToString());
Check("bayar cepat > lambat", pPerfect > pLate, pPerfect + " vs " + pLate);
Check("kepuasan cepat > lambat", s1 > s2, s1.ToString("F2") + " vs " + s2.ToString("F2"));
Geprek.Core.ServeOutcome o3; float s3;
int pBonus = Geprek.Economy.Payout.Compute(cfg, merah, null, 0.9f, 1f, 0.24f, out o3, out s3);
Check("upgrade harga menaikkan bayaran", pBonus > pPerfect, pBonus + " vs " + pPerfect);

var prog = new Geprek.Progression.PlayerProgress();
Check("mulai level 1", prog.level == 1, "");
prog.AddXp(cfg, cfg.XpForLevel(2));
Check("XP pas -> naik level 2", prog.level == 2, "level " + prog.level);
prog.AddXp(cfg, 100000);
Check("level berhenti di maksimum", prog.level == cfg.maxLevel, "level " + prog.level);
Check("kualitas masak naik seiring level", prog.CookQuality(cfg) > 0.9f, prog.CookQuality(cfg).ToString("F2"));

var up = db.GetUpgrade("sepatu");
Check("harga upgrade naik tiap tingkat", up.CostAt(1) > up.CostAt(0), up.CostAt(0) + " -> " + up.CostAt(1));
prog.SetUpgradeLevel("sepatu", 2);
Check("nilai upgrade terakumulasi",
      Mathf.Approximately(prog.UpgradeValue(db, Geprek.Core.UpgradeKind.MoveSpeed), up.valuePerLevel * 2),
      prog.UpgradeValue(db, Geprek.Core.UpgradeKind.MoveSpeed).ToString("F2"));

var stats = new Geprek.Core.DayStats();
stats.Reset(1, 1, 100000);
stats.revenue = 120000; stats.served = 10; stats.satisfactionSum = 8f;
Check("target tercapai terdeteksi", stats.TargetMet, "");
Check("rata-rata kepuasan benar", Mathf.Approximately(stats.AverageSatisfaction, 0.8f), "");
Check("nilai huruf masuk akal", stats.Grade == "A" || stats.Grade == "B", stats.Grade);

var day1 = db.arcs[0].days[0];
Geprek.Core.ServeOutcome o4; float s4;
int perfectPay = Geprek.Economy.Payout.Compute(cfg, original, null, 0.8f, 0.55f, 0f, out o4, out s4);
int maksimal = perfectPay * day1.totalCustomers;
Check("target hari 1 bisa dicapai", maksimal >= day1.targetRevenue,
      "maks ~" + maksimal + " vs target " + day1.targetRevenue);

// --- target harian & jam sibuk (Phase 4-5) ---
int tanpaTarget = 0, rushSalah = 0;
foreach (var arc in db.arcs)
    foreach (var day in arc.days)
    {
        if (day.objectives == null || day.objectives.Count == 0) tanpaTarget++;
        if (day.hasLunchRush && day.rushEnd <= day.rushStart) rushSalah++;
    }
Check("semua hari punya target tambahan", tanpaTarget == 0, tanpaTarget + " hari kosong");
Check("rentang jam sibuk masuk akal", rushSalah == 0, rushSalah + " salah");

var hariUji = db.arcs[0].days[0];
var statUji = new Geprek.Core.DayStats();
statUji.Reset(1, 1, hariUji.targetRevenue);
Check("target belum tercapai di awal", !hariUji.objectives[0].IsMet(statUji), "");
for (int i = 0; i < 10; i++) { statUji.served++; statUji.satisfactionSum += 0.9f; }
Check("target tercapai setelah dilayani", hariUji.objectives[0].IsMet(statUji), hariUji.objectives[0].Progress(statUji));

statUji.RecordSale("geprek_original"); statUji.RecordSale("geprek_original");
Check("catatan penjualan per menu benar", statUji.SoldOf("geprek_original") == 2, statUji.SoldOf("geprek_original").ToString());

// --- tiga lokasi berbeda (Phase 8) ---
Check("arc 1 di warung", db.arcs[0].businessLocation == Geprek.Core.LocationId.Warung, db.arcs[0].businessLocation.ToString());
Check("arc 2 di ruko", db.arcs[1].businessLocation == Geprek.Core.LocationId.Ruko, db.arcs[1].businessLocation.ToString());
Check("arc 3 di restoran", db.arcs[2].businessLocation == Geprek.Core.LocationId.Restaurant, db.arcs[2].businessLocation.ToString());

// --- tamu istimewa (Phase 6) ---
int istimewa = 0;
foreach (var c in db.customerTypes) if (c.isSpecialGuest) istimewa++;
Check("ada satu tamu istimewa", istimewa == 1, istimewa.ToString());
foreach (var c in db.customerTypes)
    if (c.isSpecialGuest)
        Check("tamu istimewa tidak muncul acak", Mathf.Approximately(c.spawnWeight, 0f), c.spawnWeight.ToString());

// --- karyawan & cabang (Phase 9-10) ---
var uji = new Geprek.Progression.PlayerProgress();
uji.staff.Add(new Geprek.Progression.StaffMember{ role = Geprek.Core.StaffRole.Cook, level = 2, salary = 15000 });
uji.staff.Add(new Geprek.Progression.StaffMember{ role = Geprek.Core.StaffRole.Cashier, level = 1, salary = 15000 });
Check("gaji dihitung per tingkat", uji.TotalSalary() == 45000, uji.TotalSalary().ToString());
Check("kasir memberi bonus harga", uji.CashierPriceBonus() > 0f, uji.CashierPriceBonus().ToString("F2"));
Check("hitung karyawan per peran", uji.StaffCount(Geprek.Core.StaffRole.Cook) == 1, "");

var cab = new Geprek.Progression.BranchInfo{ level = 2, staffAssigned = 2 };
int setoranRendah = cab.DailyIncome(0.1f);
int setoranTinggi = cab.DailyIncome(0.9f);
Check("setoran cabang naik bersama reputasi", setoranTinggi > setoranRendah, setoranRendah + " -> " + setoranTinggi);
Check("cabang punya biaya harian", cab.DailyCost > 0, cab.DailyCost.ToString());

// --- rencana medsos (Phase 6) ---
uji.tomorrowCustomerBonus = 0.25f; uji.tomorrowPriceScale = 0.82f; uji.vloggerTomorrow = true;
uji.ConsumeTomorrowPlan();
Check("rencana besok terpakai sekali", uji.tomorrowCustomerBonus == 0f && uji.tomorrowPriceScale == 1f && !uji.vloggerTomorrow, "");

Geprek.Core.ServeOutcome oD; float sD;
int hargaNormal = Geprek.Economy.Payout.Compute(cfg, merah, null, 0.9f, 1f, 0f, out oD, out sD, 1f);
int hargaDiskon = Geprek.Economy.Payout.Compute(cfg, merah, null, 0.9f, 1f, 0f, out oD, out sD, 0.82f);
Check("diskon menurunkan harga jual", hargaDiskon < hargaNormal, hargaDiskon + " vs " + hargaNormal);

return log.ToString();
