window.Symplify = window.Symplify || {};
window.Symplify.DocumentTypes = window.Symplify.DocumentTypes || {};

window.Symplify.DocumentTypes.Translations = (function ($) {
    'use strict';

    const selectors = { statusSwitch: '.js-document-type-status-switch', statusLabel: '.js-document-type-status-label' };

    function init() {
        $(document)
            .off('change.document-typesStatus', selectors.statusSwitch)
            .on('change.document-typesStatus', selectors.statusSwitch, updateStatusLabel);

        $(selectors.statusSwitch).trigger('change');
    }

    function updateStatusLabel() {
        const $switch = $(this);
        const $label = $switch.closest('.form-switch').find(selectors.statusLabel);
        const texts = window.Symplify.DocumentTypes.texts || window.Symplify.Texts || window.Symplify.texts || {};

        if ($switch.is(':checked')) {
            $label.text(texts.active || 'Aktif').removeClass('text-danger-600').addClass('text-success-600');
            return;
        }

        $label.text(texts.passive || 'Pasif').removeClass('text-success-600').addClass('text-danger-600');
    }

    return { init: init };
})(jQuery);
