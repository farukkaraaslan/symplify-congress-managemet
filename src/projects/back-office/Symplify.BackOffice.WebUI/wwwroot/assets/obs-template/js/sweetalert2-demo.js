document.addEventListener('DOMContentLoaded', function () {
            if (!window.Swal) {
                console.error('SweetAlert2 yuklenemedi. assets/vendor/sweetalert2 dosyalarini kontrol edin.');
                return;
            }
            const AppToast = {
                show({
                    icon = 'success',
                    title = 'İşlem başarılı',
                    text = '',
                    position = 'top-end',
                    timer = 3000,
                    showCloseButton = true
                } = {}) {
                    return Swal.fire({
                        toast: true,
                        position,
                        icon,
                        title,
                        text,
                        showConfirmButton: false,
                        showCloseButton,
                        timer,
                        timerProgressBar: true,
                        didOpen: (toast) => {
                            toast.addEventListener('mouseenter', Swal.stopTimer);
                            toast.addEventListener('mouseleave', Swal.resumeTimer);
                        }
                    });
                },

                success(title, text = '', position = 'top-end') {
                    return this.show({ icon: 'success', title, text, position });
                },

                error(title, text = '', position = 'top-end') {
                    return this.show({ icon: 'error', title, text, position });
                },

                warning(title, text = '', position = 'top-end') {
                    return this.show({ icon: 'warning', title, text, position });
                },

                info(title, text = '', position = 'top-end') {
                    return this.show({ icon: 'info', title, text, position });
                }
            };

            document.querySelector('.swal-success-btn')?.addEventListener('click', function () {
                Swal.fire({
                    icon: 'success',
                    title: 'Başarılı',
                    text: 'İşlemin başarıyla tamamlandı.'
                });
            });

            document.querySelector('.swal-error-btn')?.addEventListener('click', function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Bir hata oluştu',
                    text: 'Lütfen tekrar deneyin.'
                });
            });

            document.querySelector('.swal-info-btn')?.addEventListener('click', function () {
                Swal.fire({
                    icon: 'info',
                    title: 'Bilgilendirme',
                    text: 'Bu alan sadece görüntüleme amaçlıdır.'
                });
            });

            document.querySelector('.swal-question-btn')?.addEventListener('click', function () {
                Swal.fire({
                    icon: 'question',
                    title: 'Devam etmek istiyor musun?',
                    text: 'Bu işlem bir sonraki adıma geçirecek.',
                    showCancelButton: true,
                    confirmButtonText: 'Evet, devam et',
                    cancelButtonText: 'Vazgeç'
                });
            });

            document.querySelector('.swal-warning-btn')?.addEventListener('click', function () {
                Swal.fire({
                    icon: 'warning',
                    title: 'Emin misin?',
                    text: 'Bu işlem geri alınamayabilir.',
                    showCancelButton: true,
                    confirmButtonText: 'Evet, onayla',
                    cancelButtonText: 'İptal'
                }).then((result) => {
                    if (result.isConfirmed) {
                        AppToast.success('İşlem onaylandı');
                    }
                });
            });

            document.querySelector('.swal-delete-btn')?.addEventListener('click', function () {
                Swal.fire({
                    icon: 'warning',
                    title: 'Kaydı silmek istiyor musun?',
                    text: 'Bu kayıt kalıcı olarak silinecek.',
                    showCancelButton: true,
                    confirmButtonText: 'Evet, sil',
                    cancelButtonText: 'Vazgeç',
                    reverseButtons: true
                }).then((result) => {
                    if (result.isConfirmed) {
                        AppToast.success('Kayıt başarıyla silindi');
                    }
                });
            });

            document.querySelector('.toast-top-end-btn')?.addEventListener('click', function () {
                AppToast.success('Kayıt başarıyla oluşturuldu', '', 'top-end');
            });

            document.querySelector('.toast-top-center-btn')?.addEventListener('click', function () {
                AppToast.info('Yeni bildirim geldi', 'Kontrol etmeyi unutma', 'top');
            });

            document.querySelector('.toast-top-start-btn')?.addEventListener('click', function () {
                AppToast.warning('Dikkat', 'Bazı alanlar eksik görünüyor', 'top-start');
            });

            document.querySelector('.toast-bottom-end-btn')?.addEventListener('click', function () {
                AppToast.error('İşlem başarısız', 'Lütfen tekrar dene', 'bottom-end');
            });

            document.querySelector('.toast-bottom-center-btn')?.addEventListener('click', function () {
                AppToast.info('Alt orta bildirim', 'Demo toast örneği', 'bottom');
            });

            document.querySelector('.toast-bottom-start-btn')?.addEventListener('click', function () {
                AppToast.success('Alt sol bildirim', 'İşlem tamamlandı', 'bottom-start');
            });

            document.querySelector('.swal-input-btn')?.addEventListener('click', async function () {
                const result = await Swal.fire({
                    title: 'Proje adı',
                    input: 'text',
                    inputPlaceholder: 'Bir proje adı yaz...',
                    showCancelButton: true,
                    confirmButtonText: 'Kaydet',
                    cancelButtonText: 'İptal',
                    inputValidator: (value) => {
                        if (!value) {
                            return 'Bu alan boş bırakılamaz';
                        }
                    }
                });

                if (result.isConfirmed) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Kaydedildi',
                        text: `Girilen değer: ${result.value}`
                    });
                }
            });

            document.querySelector('.swal-textarea-btn')?.addEventListener('click', async function () {
                const result = await Swal.fire({
                    title: 'Açıklama ekle',
                    input: 'textarea',
                    inputPlaceholder: 'Bir açıklama yaz...',
                    inputAttributes: {
                        'aria-label': 'Açıklama'
                    },
                    showCancelButton: true,
                    confirmButtonText: 'Gönder',
                    cancelButtonText: 'İptal'
                });

                if (result.isConfirmed) {
                    AppToast.success('Açıklama kaydedildi');
                }
            });

            document.querySelector('.swal-select-btn')?.addEventListener('click', async function () {
                const result = await Swal.fire({
                    title: 'Teknoloji seç',
                    input: 'select',
                    inputOptions: {
                        html: 'HTML',
                        css: 'CSS',
                        javascript: 'JavaScript',
                        php: 'PHP',
                        laravel: 'Laravel',
                        react: 'React'
                    },
                    inputPlaceholder: 'Bir teknoloji seç',
                    showCancelButton: true,
                    confirmButtonText: 'Seç',
                    cancelButtonText: 'İptal'
                });

                if (result.isConfirmed) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Seçim tamamlandı',
                        text: `Seçilen değer: ${result.value}`
                    });
                }
            });

            document.querySelector('.swal-loading-btn')?.addEventListener('click', function () {
                Swal.fire({
                    title: 'Yükleniyor...',
                    text: 'Lütfen bekleyin',
                    allowOutsideClick: false,
                    allowEscapeKey: false,
                    didOpen: () => {
                        Swal.showLoading();
                    }
                });

                setTimeout(() => {
                    Swal.fire({
                        icon: 'success',
                        title: 'Tamamlandı',
                        text: 'Yükleme başarıyla bitti.'
                    });
                }, 2000);
            });

            document.querySelector('.swal-async-btn')?.addEventListener('click', async function () {
                const result = await Swal.fire({
                    title: 'Kullanıcı bilgisini getir',
                    text: 'Demo amaçlı async işlem çalışacak.',
                    showCancelButton: true,
                    confirmButtonText: 'Getir',
                    cancelButtonText: 'İptal',
                    showLoaderOnConfirm: true,
                    preConfirm: () => {
                        return new Promise((resolve) => {
                            setTimeout(() => {
                                resolve({
                                    name: 'Ahmet Yılmaz',
                                    email: 'ahmet@example.com'
                                });
                            }, 1800);
                        });
                    },
                    allowOutsideClick: () => !Swal.isLoading()
                });

                if (result.isConfirmed) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Kullanıcı bulundu',
                        html: `
                        <div class="text-start">
                            <strong>Ad:</strong> ${result.value.name}<br>
                            <strong>Email:</strong> ${result.value.email}
                        </div>
                    `
                    });
                }
            });

            document.querySelector('.swal-wizard-btn')?.addEventListener('click', async function () {
                const steps = ['1', '2', '3'];

                const result = await Swal.mixin({
                    confirmButtonText: 'İleri',
                    cancelButtonText: 'Geri',
                    showCancelButton: true,
                    progressSteps: steps
                }).queue([
                    {
                        title: 'Adım 1',
                        text: 'Proje adını gir',
                        input: 'text',
                        inputPlaceholder: 'Proje adı'
                    },
                    {
                        title: 'Adım 2',
                        text: 'Kategori seç',
                        input: 'select',
                        inputOptions: {
                            saas: 'SaaS',
                            ecommerce: 'E-Commerce',
                            crm: 'CRM'
                        },
                        inputPlaceholder: 'Kategori seç'
                    },
                    {
                        title: 'Adım 3',
                        text: 'Kısa açıklama yaz',
                        input: 'textarea',
                        inputPlaceholder: 'Açıklama'
                    }
                ]);

                if (result.value) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Wizard tamamlandı',
                        html: `
                        <div class="text-start">
                            <strong>Proje:</strong> ${result.value[0] || '-'}<br>
                            <strong>Kategori:</strong> ${result.value[1] || '-'}<br>
                            <strong>Açıklama:</strong> ${result.value[2] || '-'}
                        </div>
                    `
                    });
                }
            });

            document.querySelector('.swal-form-btn')?.addEventListener('click', async function () {
                const result = await Swal.fire({
                    title: 'Yeni kullanıcı ekle',
                    html: `
                    <input id="swal-name" class="swal2-input" placeholder="Ad Soyad">
                    <input id="swal-email" class="swal2-input" placeholder="Email">
                    <input id="swal-role" class="swal2-input" placeholder="Rol">
                `,
                    focusConfirm: false,
                    showCancelButton: true,
                    confirmButtonText: 'Kaydet',
                    cancelButtonText: 'İptal',
                    preConfirm: () => {
                        const name = document.getElementById('swal-name').value;
                        const email = document.getElementById('swal-email').value;
                        const role = document.getElementById('swal-role').value;

                        if (!name || !email || !role) {
                            Swal.showValidationMessage('Lütfen tüm alanları doldurun');
                            return false;
                        }

                        return { name, email, role };
                    }
                });

                if (result.isConfirmed) {
                    AppToast.success('Kullanıcı oluşturuldu', `${result.value.name} eklendi`);
                }
            });
        });
