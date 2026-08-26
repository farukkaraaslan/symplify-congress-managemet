window.Symplify = window.Symplify || {};
window.Symplify.OrganizationApiKeys = window.Symplify.OrganizationApiKeys || {};

window.Symplify.OrganizationApiKeys.Index = (function ($) {
    'use strict';

    function init() {
        if (window.Symplify.OrganizationApiKeys.Table) {
            window.Symplify.OrganizationApiKeys.Table.init();
        }

        if (window.Symplify.OrganizationApiKeys.Create) {
            window.Symplify.OrganizationApiKeys.Create.init();
        }

        bindOneTimeCopyButton();
    }

    function bindOneTimeCopyButton() {
        $(document)
            .off('click.organizationApiKeysOneTimeCopy', '.js-copy-one-time-key')
            .on('click.organizationApiKeysOneTimeCopy', '.js-copy-one-time-key', function () {
                const value = $('#oneTimeApiKey').val();
                const texts = getTexts();

                copyToClipboard(value)
                    .then(function () {
                        showSuccess(texts.copied || 'Kopyalandı.');
                    })
                    .catch(function () {
                        showError(texts.copyFailed || 'Kopyalama işlemi başarısız oldu.');
                    });
            });
    }

    function copyToClipboard(value) {
        const text = String(value || '').trim();

        if (!text) {
            return Promise.reject();
        }

        if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
            return navigator.clipboard.writeText(text);
        }

        const input = document.createElement('textarea');
        input.value = text;
        input.setAttribute('readonly', 'readonly');
        input.style.position = 'absolute';
        input.style.left = '-9999px';

        document.body.appendChild(input);
        input.select();

        const copied = document.execCommand('copy');
        document.body.removeChild(input);

        return copied ? Promise.resolve() : Promise.reject();
    }

    function getTexts() {
        return window.Symplify.OrganizationApiKeys?.texts ||
            window.Symplify.Texts ||
            window.Symplify.texts ||
            {};
    }

    function showSuccess(message) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function') {
            window.Symplify.Ajax.showSuccess(message);
            return;
        }

        console.info(message);
    }

    function showError(message) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(message);
            return;
        }

        window.alert(message);
    }

    return {
        init: init
    };
})(jQuery);

$(function () {
    'use strict';
    window.Symplify.OrganizationApiKeys.Index.init();
});