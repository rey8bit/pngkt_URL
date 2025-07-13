// script.js

// Objek untuk melacak indeks slide setiap karosel
let slideIndex = {};

/**
 * Fungsi untuk mengontrol efek expand/collapse pada bagian accordion.
 * @param {HTMLElement} button - Tombol accordion yang diklik.
 */
function toggleBillboard(button) {
    const content = button.nextElementSibling;
    const icon = button.querySelector('.icon');

    // Tutup accordion lain yang mungkin terbuka, kecuali yang sedang diklik
    document.querySelectorAll('.accordion-button.active').forEach(activeButton => {
        if (activeButton !== button) {
            activeButton.classList.remove('active');
            activeButton.querySelector('.icon').innerHTML = '&#9654;'; // Panah kanan
            activeButton.nextElementSibling.style.maxHeight = null;
            activeButton.nextElementSibling.style.paddingTop = '0';
        }
    });

    if (content.style.maxHeight) {
        content.style.maxHeight = null;
        content.style.paddingTop = '0';
        icon.innerHTML = '&#9654;'; // Panah kanan
        button.classList.remove('active');
    } else {
        // Set max-height ke nilai yang cukup besar untuk menampung konten
        content.style.maxHeight = content.scrollHeight + 50 + "px"; // Tambah buffer sedikit lebih besar
        content.style.paddingTop = '18px';
        icon.innerHTML = '&#9660;'; // Panah bawah
        button.classList.add('active');
    }
}

/**
 * Mendapatkan tanggal saat ini di zona waktu Jakarta (WIB), dinormalisasi ke awal hari.
 * Ini penting untuk perhitungan durasi yang konsisten tanpa terpengaruh jam.
 * @returns {Date} Objek Date yang mewakili tanggal hari ini di Jakarta pada 00:00:00.
 */
function getJakartaCurrentDate() {
    const now = new Date();
    // Offset WIB adalah UTC+7
    const jakartaOffset = 7 * 60; // dalam menit
    const utc = now.getTime() + (now.getTimezoneOffset() * 60000); // Konversi ke UTC milliseconds
    const jakartaTime = new Date(utc + (jakartaOffset * 60000));
    
    // Kembalikan objek Date yang dinormalisasi ke awal hari di zona waktu Jakarta
    return new Date(jakartaTime.getFullYear(), jakartaTime.getMonth(), jakartaTime.getDate());
}

/**
 * Menghitung status dan sisa durasi iklan berdasarkan tanggal mulai dan durasi.
 * @param {string} startDateString - Tanggal mulai dalam format YYYY-MM-DD.
 * @param {number} durationDays - Durasi dalam hari.
 * @returns {Object} Objek berisi status dan sisa durasi/informasi.
 */
function calculateAdStatus(startDateString, durationDays) {
    if (!startDateString || !durationDays || durationDays <= 0) {
        return { status: "Tidak Tersedia", remaining: "" };
    }

    // Pastikan tanggal mulai diinterpretasikan di awal hari untuk konsistensi perhitungan
    const startDate = new Date(startDateString + 'T00:00:00'); 
    
    const endDate = new Date(startDate);
    endDate.setDate(endDate.getDate() + durationDays - 1); // Durasi N hari berarti hari terakhir adalah Hari ke-N
    // Set ke akhir hari terakhir untuk inklusivitas
    endDate.setHours(23, 59, 59, 999); 

    const todayJakarta = getJakartaCurrentDate();
    
    // Normalisasi tanggal untuk perbandingan yang akurat (mengabaikan waktu)
    const normalizedStartDate = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate());
    const normalizedEndDate = new Date(endDate.getFullYear(), endDate.getMonth(), endDate.getDate());


    const oneDay = 24 * 60 * 60 * 1000; // milliseconds dalam satu hari

    // Kasus 1: Iklan sudah selesai
    if (todayJakarta > normalizedEndDate) {
        const daysOver = Math.floor((todayJakarta.getTime() - normalizedEndDate.getTime()) / oneDay);
        return { status: "Selesai", remaining: `${daysOver} hari lalu` };
    } 
    // Kasus 2: Iklan belum dimulai
    else if (todayJakarta < normalizedStartDate) {
        const remainingDaysToStart = Math.ceil((normalizedStartDate.getTime() - todayJakarta.getTime()) / oneDay);
        
        if (remainingDaysToStart <= 7) {
            return { status: "Akan Berakhir", remaining: `${remainingDaysToStart} hari lagi dimulai` };
        } else {
            // Jika lebih dari 7 hari, kembalikan status "Tidak Tersedia"
            return { status: "Tidak Tersedia", remaining: "" }; 
        }
    } 
    // Kasus 3: Iklan sedang berlangsung atau hari terakhir
    else { 
        const daysPassed = Math.floor((todayJakarta.getTime() - normalizedStartDate.getTime()) / oneDay);
        const remainingDays = durationDays - daysPassed; 
        
        if (remainingDays <= 0) { 
             return { status: "Hari Terakhir", remaining: "hari ini" };
        } else {
            return { status: "Sedang Berlangsung", remaining: `${remainingDays} hari tersisa` };
        }
    }
}


/**
 * Fungsi untuk menampilkan slide tertentu dalam karosel dan memperbarui info terkait (penyewa, tanggal, durasi, status).
 * @param {string} carouselId - ID unik dari elemen karosel (e.g., 'carousel-billboard1').
 * @param {number} n - Jumlah pergeseran slide (1 untuk maju, -1 untuk mundur).
 */
function showSlides(carouselId, n) {
    const carousel = document.getElementById(carouselId);
    if (!carousel) {
        console.warn(`[showSlides] Carousel ID "${carouselId}" tidak ditemukan. Aborted.`);
        return;
    }

    const slides = carousel.querySelectorAll(".carousel-slide");
    const dots = carousel.querySelectorAll(".dot");
    const wrapper = carousel.querySelector(".carousel-wrapper");
    const totalSlides = slides.length;

    // Temukan elemen di luar karosel untuk menampilkan info penyewa/durasi/status
    // Sekarang kita mencari di parent .accordion-content
    const parentContainer = carousel.closest('.accordion-content'); 
    const clientInfoElement = parentContainer ? parentContainer.querySelector('.current-penyewa-info') : null;
    const tanggalMulaiInfoElement = parentContainer ? parentContainer.querySelector('.current-tanggal-mulai-info') : null;
    const durasiInfoElement = parentContainer ? parentContainer.querySelector('.current-durasi-info') : null;
    const statusInfoElement = parentContainer ? parentContainer.querySelector('.current-status-info') : null;

    if (totalSlides === 0) {
        // Jika tidak ada slide, set info ke N/A
        if (clientInfoElement) clientInfoElement.textContent = 'Penyewa: N/A';
        if (tanggalMulaiInfoElement) tanggalMulaiInfoElement.textContent = 'Tanggal Mulai: N/A';
        if (durasiInfoElement) durasiInfoElement.textContent = 'Durasi: N/A';
        if (statusInfoElement) statusInfoElement.innerHTML = 'Status: <span class="status-tidak-tersedia">Tidak Tersedia</span>';
        return;
    }

    let newIndex = (slideIndex[carouselId] || 0) + n;

    if (newIndex >= totalSlides) {
        newIndex = 0;
    }
    if (newIndex < 0) {
        newIndex = totalSlides - 1;
    }
    slideIndex[carouselId] = newIndex;

    wrapper.style.transform = `translateX(${-newIndex * 100}%)`;

    dots.forEach((dot, index) => {
        dot.classList.toggle('active', index === newIndex);
    });

    // --- Pembaruan: Update info penyewa, tanggal mulai, durasi, dan status berdasarkan slide aktif ---
    const currentActiveSlide = slides[newIndex];
    const clientNameFromSlide = currentActiveSlide.dataset.penyewa || 'N/A';
    const startDateFromSlide = currentActiveSlide.dataset.tanggalmulai || 'N/A';
    const durationDaysFromSlide = parseInt(currentActiveSlide.dataset.durasihari) || 0;

    // Hitung status dan durasi dinamis
    const adStatus = calculateAdStatus(startDateFromSlide, durationDaysFromSlide);

    if (clientInfoElement) clientInfoElement.textContent = `Penyewa: ${clientNameFromSlide}`;
    if (tanggalMulaiInfoElement) tanggalMulaiInfoElement.textContent = `Tanggal Mulai: ${startDateFromSlide}`;
    // Tampilkan durasi statis dari data JSON, sedangkan statusnya dinamis
    if (durasiInfoElement) durasiInfoElement.textContent = `Durasi: ${durationDaysFromSlide} hari`; 
    if (statusInfoElement) statusInfoElement.innerHTML = `Status: <span class="status-${adStatus.status.toLowerCase().replace(/ /g, '-')}">${adStatus.status}</span> ${adStatus.remaining}`;
}

/**
 * Mengubah slide di karosel dengan pergeseran (maju/mundur).
 * Ini adalah wrapper untuk showSlides.
 * @param {string} carouselId - ID karosel.
 * @param {number} n - Jumlah pergeseran (1 atau -1).
 */
function changeSlide(carouselId, n) {
    showSlides(carouselId, n);
}

/**
 * Menampilkan slide spesifik berdasarkan nomor urut (1-based index).
 * Ini adalah wrapper untuk showSlides.
 * @param {string} carouselId - ID karosel.
 * @param {number} n - Nomor slide yang ingin ditampilkan (1, 2, 3, dst.).
 */
function currentSlide(carouselId, n) {
    slideIndex[carouselId] = n - 1;
    showSlides(carouselId, 0); // Panggil showSlides dengan 0 untuk menampilkan indeks baru tanpa pergeseran
}

// Fungsi utama yang dijalankan saat seluruh DOM halaman telah dimuat
document.addEventListener("DOMContentLoaded", async () => {
    console.log("DOMContentLoaded: Memulai proses loading data dari data.json...");

    try {
        const response = await fetch('data.json');
        console.log(`Fetch request untuk data.json selesai. Status: ${response.status} ${response.statusText}`);

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP error! status: ${response.status}. Respons: ${errorText.substring(0, 200)}...`);
        }
        
        const data = await response.json();
        console.log("Data.json berhasil dimuat dan di-parse:", data);

        if (data && Array.isArray(data.billboards)) {
            initializePage(data.billboards);
        } else {
            console.error("[DOMContentLoaded Error]: Struktur data JSON tidak valid. 'billboards' array tidak ditemukan atau bukan array.");
            const errorContainer = document.querySelector('.container');
            if (errorContainer) {
                 errorContainer.innerHTML = `<p style="color: red;">Error: Struktur data JSON tidak valid. Mohon hubungi administrator.</p>`;
            }
        }

    } catch (error) {
        console.error("[DOMContentLoaded Catch Block]: Gagal memuat data billboard:", error);
        const errorContainer = document.querySelector('.container');
        if (errorContainer) {
             errorContainer.innerHTML = `<p style="color: red;">Gagal memuat data. Silakan coba lagi nanti. Detail: ${error.message}</p>`;
        }
    }
});

/**
 * Fungsi untuk membangun seluruh konten halaman berdasarkan data billboards.
 * Ini akan mengisi semua billboard sebagai item accordion.
 * @param {Array<Object>} billboards - Array objek billboard dari data.json.
 */
function initializePage(billboards) {
    console.log("[initializePage]: Memulai pembangunan halaman dengan data billboards.");

    const allBillboardsContainer = document.getElementById('all-billboards-accordion-container');

    if (!allBillboardsContainer) {
        console.error("[initializePage Error]: Elemen container HTML untuk semua billboard (ID 'all-billboards-accordion-container') tidak ditemukan. Pastikan ID ada di index.html.");
        return;
    }

    allBillboardsContainer.innerHTML = ''; // Kosongkan kontainer

    billboards.forEach(billboard => {
        let carouselHTML = '';
        
        let displayPenyewa = '(Kosong)';
        let displayDurasi = '-';
        let displayTanggalMulai = '-';
        let billboardStatus = { status: "Tidak Tersedia", remaining: "" };

        if (billboard.slides && billboard.slides.length > 0) {
            const carouselId = `carousel-${billboard.id}`;
            let slidesHTML = '';
            let dotsHTML = '';

            billboard.slides.forEach((slide, index) => {
                const imagePath = `slides/${billboard.id}/${slide.file}`;
                
                slidesHTML += `
                    <div class="carousel-slide" 
                         data-penyewa="${slide.penyewa || ''}" 
                         data-tanggalmulai="${slide.tanggalMulai || ''}" 
                         data-durasihari="${slide.durasiHari || ''}">
                        <img src="${imagePath}" 
                             alt="${billboard.nama} - Penyewa: ${slide.penyewa}" 
                             onerror="this.parentElement.classList.add('error'); this.alt='Gambar tidak ditemukan'; console.error('Gagal memuat gambar: ${imagePath}');">
                        <span class="error-message">Gambar tidak ditemukan</span>
                        <span class="client-name">Penyewa: ${slide.penyewa || 'N/A'}</span>
                    </div>
                `;
                dotsHTML += `<span class="dot" onclick="currentSlide('${carouselId}', ${index + 1})"></span>`;
            });

            carouselHTML = `
                <h4>Galeri Slot Iklan</h4>
                <div class="carousel-container" id="${carouselId}">
                    <div class="carousel-ratio-box">
                        <div class="carousel-wrapper">${slidesHTML}</div>
                    </div>
                    <button class="carousel-nav-btn prev-slide" onclick="changeSlide('${carouselId}', -1)">&#10094;</button>
                    <button class="carousel-nav-btn next-slide" onclick="changeSlide('${carouselId}', 1)">&#10095;</button>
                    <div class="slide-dots">${dotsHTML}</div>
                </div>
            `;
            
            if (billboard.slides[0]) {
                 const firstSlide = billboard.slides[0];
                 displayPenyewa = firstSlide.penyewa || 'N/A';
                 displayDurasi = `${firstSlide.durasiHari} hari` || 'N/A';
                 displayTanggalMulai = firstSlide.tanggalMulai || 'N/A';
                 billboardStatus = calculateAdStatus(firstSlide.tanggalMulai, firstSlide.durasiHari);
            }

        } else { // Jika tidak ada slide, ambil data dari properti billboard utama
            displayPenyewa = billboard.penyewa || '(Kosong)';
            displayDurasi = billboard.durasi || '-';
            displayTanggalMulai = billboard.tanggalMulai || '-';
            const durationInt = parseInt(billboard.durasi) || 0; 
            billboardStatus = calculateAdStatus(billboard.tanggalMulai, durationInt);
        }

        // --- Pembangunan HTML untuk Setiap Billboard sebagai Accordion ---
        const billboardAccordionHTML = `
            <button class="accordion-button" onclick="toggleBillboard(this)">
                <h3>${billboard.nama}</h3>
                <span class="icon">&#9654;</span>
            </button>
            <div class="accordion-content">
                <p><strong>Posisi:</strong> ${billboard.posisi}</p>
                <p class="current-tanggal-mulai-info">Tanggal Mulai: ${displayTanggalMulai}</p>
                <p class="current-durasi-info">Durasi: ${displayDurasi}</p>
                <p class="current-penyewa-info">Penyewa: ${displayPenyewa}</p>
                <p class="current-status-info">Status: <span class="status-${billboardStatus.status.toLowerCase().replace(/ /g, '-')}">${billboardStatus.status}</span> ${billboardStatus.remaining}</p>
                ${carouselHTML}
            </div>
        `;
        
        allBillboardsContainer.insertAdjacentHTML('beforeend', billboardAccordionHTML);

        if (billboard.slides && billboard.slides.length > 0) {
            const carouselId = `carousel-${billboard.id}`;
            slideIndex[carouselId] = 0;
            setTimeout(() => showSlides(carouselId, 0), 100); 
        }
    });
    console.log("[initializePage]: Pembangunan halaman selesai. Semua billboard diatur sebagai accordion.");
}