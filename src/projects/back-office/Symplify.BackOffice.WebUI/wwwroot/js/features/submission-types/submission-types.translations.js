window.Symplify = window.Symplify || {};
window.Symplify.SubmissionTypes = window.Symplify.SubmissionTypes || {};

window.Symplify.SubmissionTypes.Translations = (function ($) {
    'use strict';

    const selectors = { statusSwitch: '.js-submission-type-status-switch', statusLabel: '.js-submission-type-status-label' };

    function init() {
        $(document)
            .off('change.submission-typesStatus', selectors.statusSwitch)
            .on('change.submission-typesStatus', selectors.statusSwitch, updateStatusLabel);

        $(selectors.statusSwitch).trigger('change');
    }

    function updateStatusLabel() {
        const $switch = $(this);
        const $label = $switch.closest('.form-switch').find(selectors.statusLabel);
        const texts = window.Symplify.SubmissionTypes.texts || window.Symplify.Texts || window.Symplify.texts || {};

        if ($switch.is(':checked')) {
            $label.text(texts.active || 'Aktif').removeClass('text-danger-600').addClass('text-success-600');
            return;
        }

        $label.text(texts.passive || 'Pasif').removeClass('text-success-600').addClass('text-danger-600');
    }

    return { init: init };
})(jQuery);
