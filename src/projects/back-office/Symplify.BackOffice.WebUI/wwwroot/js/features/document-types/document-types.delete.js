window.Symplify = window.Symplify || {};
window.Symplify.DocumentTypes = window.Symplify.DocumentTypes || {};

window.Symplify.DocumentTypes.Delete = (function ($) {
    'use strict';

    const selectors = {
        form: '#deleteDocumentTypeForm',
        deleteButton: '.js-document-type-delete-button'
    };

    function init() {
        $(document)
            .off('click.document-typesDelete', selectors.deleteButton)
            .on('click.document-typesDelete', selectors.deleteButton, confirmDelete);
    }

    function confirmDelete() {
        const id = $(this).data('id');

        if (!id) {
            return;
        }

        const texts = window.Symplify.DocumentTypes.texts || window.Symplify.Texts || window.Symplify.texts || {};
        const confirmOptions = {
            title: texts.deleteConfirmTitle || 'Silmek istediğinize emin misiniz?',
            text: texts.deleteConfirmText || 'Bu kayıt silinecek. Silme sonrası aktif kayıtların sıra numarası otomatik düzenlenecek.',
            confirmButtonText: texts.delete || texts.deleteConfirmButton || 'Sil',
            cancelButtonText: texts.cancel || 'Vazgeç'
        };

        const confirmPromise = window.Symplify.Ajax && typeof window.Symplify.Ajax.confirm === 'function'
            ? window.Symplify.Ajax.confirm(confirmOptions)
            : $.Deferred().resolve({ isConfirmed: window.confirm(confirmOptions.title) }).promise();

        confirmPromise.then(function (result) {
            if (!result.isConfirmed) {
                return;
            }

            deleteItem(id);
        });
    }

    function deleteItem(id) {
        const $form = $(selectors.form);

        if (!$form.length) {
            return;
        }

        $.ajax({
            url: $form.attr('action'),
            type: $form.attr('method') || 'POST',
            data: {
                id: id,
                culture: getCurrentCulture()
            },
            headers: getAjaxHeaders($form)
        })
            .done(function (response) {
                if (!response || response.success !== true) {
                    showError(response);
                    return;
                }

                if (window.Symplify.DocumentTypes.Table) {
                    window.Symplify.DocumentTypes.Table.reload(false);
                }

                showSuccess(response.message || getText('deleted', 'Kayıt silindi.'));
            })
            .fail(showError);
    }

    function getAjaxHeaders($form) {
        const headers = {
            'X-Culture': getCurrentCulture()
        };

        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.getAntiForgeryToken === 'function') {
            const token = window.Symplify.Ajax.getAntiForgeryToken($form);

            if (token) {
                headers.RequestVerificationToken = token;
            }
        }

        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }


    function getText(key, fallback) {
        const texts = window.Symplify.DocumentTypes.texts || window.Symplify.Texts || window.Symplify.texts || {};
        return texts[key] || fallback;
    }

    function showSuccess(message) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function') {
            window.Symplify.Ajax.showSuccess(message);
            return;
        }

        alert(message || getText('successTitle', 'Başarılı'));
    }

    function showError(response) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(response);
            return;
        }

        alert(response?.responseJSON?.message || response?.message || getText('genericError', 'İşlem sırasında hata oluştu.'));
    }

    return {
        init: init
    };
})(jQuery);
