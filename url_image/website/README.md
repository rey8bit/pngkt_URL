# Penjelasan data.json

File `data.json` berisi konfigurasi untuk semua papan iklan.

## Struktur Billboard:
- `id`: (String) ID unik untuk billboard.
- `nama`: (String) Nama tampilan billboard.
- `posisi`: (String) Lokasi fisik billboard.
- `slides`: (Array of Objects) Opsional. Jika ada, billboard ini memiliki karosel dengan banyak slot iklan.
    - `file`: (String) Nama file gambar (misal: `1.png`). Path akan di-generate (`slides/{billboard_id}/{file}`).
    - `penyewa`: (String) Nama penyewa iklan.
    - `tanggalMulai`: (String) **WAJIB FORMAT YYYY-MM-DD**. Tanggal mulai iklan.
    - `durasiHari`: (Number) **WAJIB INTEGER > 0**. Durasi iklan dalam hari.

## Penting:
- Pastikan semua path gambar di folder `slides/` sudah benar.
- Untuk `tanggalMulai` dan `durasiHari`, gunakan format yang ditentukan agar perhitungan status berfungsi.
- Jika `slides` kosong atau tidak ada, data `penyewa`, `tanggalMulai`, dan `durasi` akan diambil dari level billboard utama (jika ada).

- format { "file": "1.png", "penyewa": "bloodtypea", "tanggalMulai": "2025-07-01", "durasiHari": 30 }
