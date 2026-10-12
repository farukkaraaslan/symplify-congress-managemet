(function ($) {
    'use strict';

    if (!$ || !$.fn || !$.fn.select2) {
        console.error('Select2 yuklenemedi. assets/vendor/select2 dosyalarini kontrol edin.');
        return;
    }

    $(function () {
        $('.js-select2').each(function () {
            $(this).select2({
                width: '100%',
                placeholder: $(this).data('placeholder') || 'Seçiniz'
            });
        });

        $('.js-select2-clear').each(function () {
            $(this).select2({
                width: '100%',
                placeholder: $(this).data('placeholder') || 'Seçiniz',
                allowClear: true
            });
        });

        $('.js-select2-multiple').each(function () {
            $(this).select2({
                width: '100%',
                placeholder: $(this).data('placeholder') || 'Bir veya daha fazla seçim yap'
            });
        });

        $('.js-select2-tags').each(function () {
            $(this).select2({
                width: '100%',
                tags: true,
                tokenSeparators: [','],
                placeholder: $(this).data('placeholder') || 'Etiket yaz ve Enter bas'
            });
        });

        $('.js-select2-ajax').each(function () {
            $(this).select2({
                width: '100%',
                placeholder: $(this).data('placeholder') || 'Kullanıcı ara...',
                minimumInputLength: 2,
                ajax: {
                    delay: 250,
                    transport: function (params, success) {
                        var users = [
                            { id: 1, text: 'Ahmet Yılmaz' },
                            { id: 2, text: 'Ayşe Demir' },
                            { id: 3, text: 'Mehmet Kaya' },
                            { id: 4, text: 'Elif Şahin' },
                            { id: 5, text: 'Faruk Karaaslan' },
                            { id: 6, text: 'Zeynep Arslan' }
                        ];
                        var term = String((params.data && params.data.q) || '').toLocaleLowerCase('tr-TR');
                        window.setTimeout(function () {
                            success({ items: users.filter(function (x) {
                                return !term || x.text.toLocaleLowerCase('tr-TR').includes(term);
                            }), hasMore: false });
                        }, 250);
                        return { abort: function () {} };
                    },
                    data: function (params) {
                        return { q: params.term || '', page: params.page || 1 };
                    },
                    processResults: function (data) {
                        return { results: data.items || [], pagination: { more: !!data.hasMore } };
                    },
                    cache: true
                }
            });
        });

        $('#select2Modal').on('shown.bs.modal', function () {
            $('.js-select2-modal').each(function () {
                if (!$(this).hasClass('select2-hidden-accessible')) {
                    $(this).select2({
                        width: '100%',
                        dropdownParent: $('#select2Modal'),
                        placeholder: $(this).data('placeholder') || 'Kategori seç',
                        allowClear: true
                    });
                }
            });

            $('.js-select2-modal-tags').each(function () {
                if (!$(this).hasClass('select2-hidden-accessible')) {
                    $(this).select2({
                        width: '100%',
                        dropdownParent: $('#select2Modal'),
                        tags: true,
                        tokenSeparators: [','],
                        placeholder: $(this).data('placeholder') || 'Etiket seç / yaz'
                    });
                }
            });
        });

        $('#select2Modal').on('hidden.bs.modal', function () {
            $('.js-select2-modal').each(function () {
                if ($(this).hasClass('select2-hidden-accessible')) {
                    $(this).select2('destroy');
                }
            });

            $('.js-select2-modal-tags').each(function () {
                if ($(this).hasClass('select2-hidden-accessible')) {
                    $(this).select2('destroy');
                }
            });
        });
        });
})(window.jQuery);
