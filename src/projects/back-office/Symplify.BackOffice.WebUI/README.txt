SYMPLIFY OBS NAVBAR AYARLAR BUTONU DÜZELTMESİ

İçerik:
- Views/Shared/_Navbar.cshtml: settings button + aria-controls
- Views/Shared/_Layout.cshtml: original OBS settings panel, script reference
- wwwroot/js/shared/ui/symplify.obs-appearance.js: click handler, init, theme state

ÖNEMLİ: _Layout.cshtml önceki OBS Orijinal CSS düzenlemesini baz alır.
Kendi layout'unuzda sonradan değişiklik yaptıysanız dosyayı bütünüyle değiştirmeyin; paneli ve JS referansını birleştirin.

OBS'nin mevcut style.css dosyasına dokunulmadı. Ek CSS dosyası yok.
Son değişikliklerin üstüne kopyalamadan önce yedek alın.
Kopyalama sonrası uygulamayı yeniden başlatın, Ctrl+F5 ile önbelleği yenileyin.

Kontrol: F12 Console:
!!document.querySelector('[data-obs-appearance-open]')
!!document.querySelector('#obsAppearancePanel')
!!document.querySelector('script[src*="symplify.obs-appearance.js"]')
Hepsi true olmalı. Ayarlar butonuna basınca panelin active sınıfı eklenmeli.

Not: Tarayıcıda canlı çalıştırma ve build testi yapılmadı.
