namespace Geprek.Core
{
    /// <summary>Layar / fase besar yang sedang aktif. Dipegang GameManager.</summary>
    public enum GameState
    {
        Boot,
        MainMenu,
        Cutscene,      // adegan cerita, kendali pemain dimatikan
        DayBriefing,   // ringkasan target sebelum warung buka
        DayOperating,  // jam operasional, pemain melayani pelanggan
        DayClosing,    // jam habis, menunggu pelanggan sisa selesai
        DayReport,     // laporan harian
        NightHome,     // aktivitas malam di rumah
        ArcReport,     // laporan ke dosen di akhir arc
        Paused,
        GameOver
    }

    /// <summary>Lokasi yang bisa ditempati pemain.</summary>
    public enum LocationId { Warung, Home, Ruko, Restaurant }

    /// <summary>Arah hadap karakter, dipakai CharacterAnimator.</summary>
    public enum Facing { Down = 0, Left = 1, Right = 2, Up = 3 }

    /// <summary>Tahap pengolahan sebuah bahan. Menentukan sprite dan stasiun berikutnya.</summary>
    public enum ItemStage
    {
        Raw,      // ayam mentah
        Cooked,   // sudah digoreng
        Prepped,  // sudah digeprek / diulek
        Plated,   // sudah jadi porsi siap antar
        Burnt,    // gosong, hanya bisa dibuang
        Dirty     // piring kotor
    }

    /// <summary>
    /// Status hidup seorang pelanggan, mengikuti alur:
    /// masuk -> antre -> duduk -> pesan -> tunggu -> (senang/marah) -> makan -> bayar -> pulang.
    /// </summary>
    public enum CustomerState
    {
        Entering,   // jalan dari pintu ke kursi / antrean
        Queueing,   // menunggu kursi kosong
        Seating,    // jalan menuju kursi
        Ordering,   // jeda singkat sebelum memunculkan pesanan
        Waiting,    // menunggu pesanan datang (patience berjalan)
        Happy,      // reaksi singkat setelah dilayani dengan baik
        Angry,      // reaksi singkat setelah kehabisan kesabaran
        Eating,     // makan setelah dilayani
        Paying,     // membayar di meja sebelum pergi
        Leaving,    // jalan keluar
        Done
    }

    /// <summary>Hasil akhir seorang pelanggan, dipakai untuk laporan harian.</summary>
    public enum ServeOutcome { Perfect, Good, Late, WrongOrder, Abandoned, NoSeat }

    /// <summary>Kategori upgrade di toko.</summary>
    public enum UpgradeKind
    {
        MoveSpeed,     // kecepatan jalan pemain
        CookSpeed,     // penggorengan lebih cepat
        PrepSpeed,     // ulek lebih cepat
        ExtraFryer,    // slot penggorengan tambahan
        ExtraSeat,     // kursi pelanggan tambahan
        PatienceBonus, // pelanggan lebih sabar (kursi nyaman)
        PriceBonus,    // harga jual naik
        HireStaff      // rekrut karyawan (Arc 2)
    }

    /// <summary>Peran karyawan yang bisa direkrut pada Arc 2.</summary>
    public enum StaffRole { Cook, Server, Cashier }

    /// <summary>Jenis target harian. Tiap hari punya satu target selain omzet.</summary>
    public enum ObjectiveKind
    {
        ServeCustomers,   // layani sekian pelanggan
        Satisfaction,     // jaga kepuasan rata-rata di atas ambang
        SellRecipe,       // jual sekian porsi menu tertentu
        MaxAbandon,       // jangan sampai lebih dari sekian pelanggan kabur
        PerfectOrders     // sekian pelayanan sempurna
    }

    public enum SfxId
    {
        Click, Pickup, Drop, Sizzle, Geprek, Plate, Serve, Coin,
        Error, Levelup, Upgrade, Notify, CustomerAngry, DayEnd, Sleep
    }
}
