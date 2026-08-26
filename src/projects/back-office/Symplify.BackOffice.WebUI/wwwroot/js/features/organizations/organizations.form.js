window.Symplify = window.Symplify || {};
window.Symplify.Organizations = window.Symplify.Organizations || {};

window.Symplify.Organizations.Form = (function ($) {
    'use strict';

    const selectors = {
        form: '#createOrganizationForm, #updateOrganizationForm, form[action*="/Organizations/Create"], form[action*="/organizations/create"], form[action*="/Organizations/Edit"], form[action*="/organizations/edit"]',
        submitButton: 'button[type="submit"], .js-organization-submit',
        nameInput: '#organizationName, input[name="Name"]',
        codeInput: '#organizationCode, input[name="Code"]',
        fileInput: 'input[type="file"]',
        fileNameTarget: '#logoFileName, .js-organization-logo-file-name'
    };

    function init() {
        const $form = $(selectors.form).first();

        if (!$form.length) {
            return;
        }

        initializeForm($form);
        bindSlugify($form);
        bindFileNames($form);
        bindSubmit($form);
    }

    function initializeForm($form) {
        window.Symplify.Forms.initialize($form);
    }

    function bindSubmit($form) {
        $form.off('submit.organizationsForm').on('submit.organizationsForm', function (event) {
            event.preventDefault();

            window.Symplify.Forms.prepareForSubmit($form);

            if (hasJQueryValidation() && !$form.valid()) {
                window.Symplify.Forms.focusFirstInvalidField($form);
                return;
            }

            setBusy($form, true);

            window.Symplify.Ajax.postForm($form, { multipart: isMultipartForm($form) })
                .done(function (response) {
                    if (!response || response.success !== true) {
                        if (window.Symplify.Forms.renderValidationErrors($form, response)) {
                            return;
                        }

                        window.Symplify.Ajax.showError(response);
                        return;
                    }

                    window.Symplify.Ajax.showSuccess(response.message || text('Common.Saved', 'Kayıt kaydedildi.'));
                    redirectIfNeeded($form, response);
                })
                .fail(function (xhr) {
                    if (window.Symplify.Forms.renderValidationErrors($form, xhr)) {
                        return;
                    }

                    window.Symplify.Ajax.showError(xhr);
                })
                .always(function () {
                    setBusy($form, false);
                });
        });
    }

    function bindSlugify($form) {
        const $nameInput = $form.find(selectors.nameInput).first();
        const $codeInput = $form.find(selectors.codeInput).first();

        if (!$nameInput.length || !$codeInput.length) {
            return;
        }

        $nameInput.off('input.organizationsSlugify').on('input.organizationsSlugify', function () {
            if ($codeInput.attr('data-touched') !== 'true') {
                $codeInput.val(slugify($nameInput.val()));
            }
        });

        $codeInput.off('input.organizationsSlugify').on('input.organizationsSlugify', function () {
            $codeInput.attr('data-touched', 'true');
            $codeInput.val(slugify($codeInput.val()));
        });
    }

    function bindFileNames($form) {
        $form.find(selectors.fileInput).each(function () {
            const input = this;
            const $input = $(input);
            const $target = resolveFileNameTarget($form, $input);

            $input.off('change.organizationsFileName').on('change.organizationsFileName', function () {
                if (!$target.length) {
                    return;
                }

                const fileName = input.files && input.files.length
                    ? input.files[0].name
                    : text('BackOffice.Organizations.Helper.Logo', 'PNG, JPG, WEBP veya SVG önerilir.');

                $target.text(fileName);
            });
        });
    }

    function resolveFileNameTarget($form, $input) {
        const targetSelector = $input.data('file-name-target') || $input.data('dropzone-file-name-target');

        if (targetSelector) {
            const $explicit = $form.find(targetSelector).first();
            if ($explicit.length) return $explicit;
        }

        const $nearest = $input.closest('.col-12, .col-md-6, .mb-3, .mb-4').find(selectors.fileNameTarget).first();
        if ($nearest.length) return $nearest;

        return $form.find(selectors.fileNameTarget).first();
    }

    function redirectIfNeeded($form, response) {
        const redirectUrl = response.redirectUrl || $form.data('redirect-url');

        if (!redirectUrl) {
            return;
        }

        window.setTimeout(function () {
            window.location.href = redirectUrl;
        }, 450);
    }

    function setBusy($form, isBusy) {
        const $buttons = $form.find(selectors.submitButton);
        $buttons.prop('disabled', isBusy);

        if (isBusy) {
            $buttons.each(function () {
                const $button = $(this);
                if (!$button.attr('data-original-text')) {
                    $button.attr('data-original-text', $button.html());
                }

                $button.html('<span class="spinner-border spinner-border-sm me-2"></span>' + escapeHtml(text('Common.Saving', 'Kaydediliyor...')));
            });
            return;
        }

        $buttons.each(function () {
            const $button = $(this);
            const originalText = $button.attr('data-original-text');

            if (originalText) {
                $button.html(originalText).removeAttr('data-original-text');
            }
        });
    }

    function isMultipartForm($form) {
        return $form.find('input[type="file"]').length > 0 ||
            String($form.attr('enctype') || '').toLowerCase().indexOf('multipart/form-data') >= 0;
    }

    function hasJQueryValidation() {
        return typeof $.validator !== 'undefined' && typeof $.validator.unobtrusive !== 'undefined';
    }

    function slugify(value) {
        return String(value || '')
            .trim()
            .toLocaleLowerCase('tr-TR')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/ğ/g, 'g')
            .replace(/ü/g, 'u')
            .replace(/ş/g, 's')
            .replace(/ı/g, 'i')
            .replace(/ö/g, 'o')
            .replace(/ç/g, 'c')
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '')
            .replace(/-{2,}/g, '-');
    }

    function text(key, fallback) {
        return window.Symplify && typeof window.Symplify.t === 'function'
            ? window.Symplify.t(key, fallback)
            : fallback;
    }

    function escapeHtml(value) {
        return $('<div/>').text(value || '').html();
    }

    return {
        init: init
    };
})(jQuery);

$(function () {
    window.Symplify.Organizations.Form.init();
});
