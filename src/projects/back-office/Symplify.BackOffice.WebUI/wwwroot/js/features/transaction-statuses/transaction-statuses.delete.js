window.Symplify = window.Symplify || {};
window.Symplify.TransactionStatuses = window.Symplify.TransactionStatuses || {};

window.Symplify.TransactionStatuses.Delete = (function ($) {
    'use strict';

    const selectors = { form: '#deleteTransactionStatusForm', deleteButton: '.js-transaction-status-delete-button' };

    function init() {
        $(document).off('click.transactionstatusDelete', selectors.deleteButton).on('click.transactionstatusDelete', selectors.deleteButton, confirmDelete);
    }

    function confirmDelete() {
        const id = $(this).data('id'); if (!id) return;
        const texts = getTexts();
        const confirmOptions = {
            title: texts.deleteConfirmTitle || 'Silmek istediğinize emin misiniz?',
            text: texts.deleteConfirmText || 'Bu kayıt silinecek. Silme sonrası aktif kayıtların sıra numarası otomatik düzenlenecek.',
            confirmButtonText: texts.delete || texts.deleteConfirmButton || 'Sil',
            cancelButtonText: texts.cancel || 'Vazgeç'
        };
        const confirmPromise = window.Symplify.Ajax?.confirm ? window.Symplify.Ajax.confirm(confirmOptions) : $.Deferred().resolve({ isConfirmed: window.confirm(confirmOptions.title) }).promise();
        confirmPromise.then(function (result) { if (result.isConfirmed) deleteItem(id); });
    }

    function deleteItem(id) {
        const $form = $(selectors.form); if (!$form.length) return;
        $.ajax({ url: $form.attr('action'), type: $form.attr('method') || 'POST', data: { id: id, culture: getCurrentCulture() }, headers: getAjaxHeaders($form) })
            .done(function (response) { if (!response || response.success !== true) { showError(response); return; } if (window.Symplify.TransactionStatuses.Table) window.Symplify.TransactionStatuses.Table.reload(false); showSuccess(response.message || getTexts().deleted || 'Kayıt silindi.'); })
            .fail(showError);
    }

    function getAjaxHeaders($container) { const headers = { 'X-Culture': getCurrentCulture() }; const token = window.Symplify.Ajax?.getAntiForgeryToken ? window.Symplify.Ajax.getAntiForgeryToken($container || $(document)) : $('input[name="__RequestVerificationToken"]').first().val(); if (token) headers.RequestVerificationToken = token; return headers; }
    function getCurrentCulture() { const segments = window.location.pathname.split('/').filter(Boolean); return segments.length > 0 ? segments[0] : ''; }
    function getTexts() { return window.Symplify.TransactionStatuses.texts || window.Symplify.Lookup?.texts || window.Symplify.Texts || window.Symplify.texts || {}; }
    function showSuccess(message) { if (window.Symplify.Ajax?.showSuccess && message) { window.Symplify.Ajax.showSuccess(message); return; } if (message) alert(message); }
    function showError(xhr) { if (window.Symplify.Ajax?.showError) { window.Symplify.Ajax.showError(xhr); return; } alert(xhr?.responseJSON?.message || xhr?.message || getTexts().genericError || 'İşlem sırasında hata oluştu.'); }

    return { init: init };
})(jQuery);
