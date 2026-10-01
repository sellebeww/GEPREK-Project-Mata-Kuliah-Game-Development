# Unity CLI dan MCP

Project ini memakai Unity CLI resmi beserta server MCP bawaannya, yang terhubung
lewat paket `com.unity.pipeline` di `Packages/manifest.json`.

Konfigurasi yang dipakai:

- Editor: `6000.5.9f1`
- Unity CLI: `1.0.0-beta.11`
- Unity Pipeline: `0.8.0-exp.1`
- Server MCP `unity` dijalankan lewat `unity mcp --project-path <folder project>`.

## Cara pakai

Buka project ini di Unity. Dari terminal di folder project:

```sh
unity --version
unity status --project-path . --format json
unity command --project-path . --limit 10 --format json
node Tools/check-unity-mcp.mjs
```

Biarkan Unity tetap terbuka selama memakai tool Editor. Skrip
`Tools/check-unity-mcp.mjs` membuka klien MCP sementara sendiri, lalu menutupnya
lagi; skrip ini tidak mengubah scene.

Hasil verifikasi 2026-10-01: MCP berhasil diinisialisasi dengan protokol
`2025-06-18`, menampilkan 160 tool, dan berhasil memanggil `editor_status` pada
Editor yang sedang terbuka.

Untuk menyiapkan komputer lain, pasang Unity CLI resmi, buka project ini, lalu
jalankan `unity mcp configure <klien>` dari folder project. Daftar klien yang
didukung ada di `unity mcp configure --list`. Tidak perlu paket MCP pihak ketiga.

## Pemecahan masalah

Jika Unity sudah terbuka tetapi CLI melaporkan tidak ada instans Pipeline, pilih
**Window → Pipeline → Stop Server**, lalu **Window → Pipeline → Start Server**.
Jika servernya sudah berhenti, cukup **Start Server**. Langkah ini membuat ulang
`Library/Pipeline/.unity-pipeline-port`. File itu berisi token autentikasi lokal,
jadi biarkan tetap berada di folder `Library` yang diabaikan git.

## Dokumentasi resmi

- [Referensi Unity CLI](https://docs.unity.com/en-us/unity-cli/unity-cli-reference)
- [Unity CLI menggantikan server MCP di dalam Editor](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli)
