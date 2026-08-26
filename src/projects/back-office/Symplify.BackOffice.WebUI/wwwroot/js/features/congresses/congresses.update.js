window.Symplify = window.Symplify || {};
window.Symplify.Congresses = window.Symplify.Congresses || {};

window.Symplify.Congresses.Update = (function ($) {
    'use strict';

    const selectors = {
        form: '#congressUpdateForm, form[action*="/Congresses/Edit"], form[action*="/congresses/edit"]',
        submitButton: '.js-congress-submit',
        submitMode: '#congressSubmitMode, input[name="submitMode"]',
        dropzone: '[data-symplify-dropzone]',
        contactEmailList: '[data-contact-email-list]',
        contactEmailRow: '[data-contact-email-row]',
        contactEmailAdd: '[data-contact-email-add]',
        contactEmailRemove: '[data-contact-email-remove]',
        contactEmailPrimary: '[data-contact-email-primary]',
        country: '[data-country-select]',
        state: '[data-state-select]',
        select2: '[data-congress-select2]'
    };

    let isSubmitting = false;

    function init() {
        const $form = $(selectors.form).first();

        if (!$form.length) {
            return;
        }

        initializeSelect2Fields($form);
        initializeForm($form);
        bindSubmitMode($form);
        bindSubmit($form);
    }

    function initializeForm($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.initialize === 'function') {
            window.Symplify.Forms.initialize($form);
        } else {
            initializeValidation($form);
        }

        initializeDropzones($form);
        initializeContactEmails($form);
        initializeLocationSelectors($form);
    }

    function initializeContactEmails($form) {
        const $list = $form.find(selectors.contactEmailList);
        const $add = $form.find(selectors.contactEmailAdd);

        if (!$list.length || !$add.length) {
            return;
        }

        function ensurePrimarySelection() {
            const $rows = $list.find(selectors.contactEmailRow);
            const $selected = $rows.find(selectors.contactEmailPrimary + ':checked');

            if (!$selected.length && $rows.length) {
                $rows.first().find(selectors.contactEmailPrimary).prop('checked', true);
            }
        }

        $add
            .off('click.congressContactEmails')
            .on('click.congressContactEmails', function () {
                const index = $list.find(selectors.contactEmailRow).length;
                $list.append(buildContactEmailRow(index));
                reindexContactEmails($form);

                $list.find(selectors.contactEmailRow).last()
                    .find('input[type="email"]')
                    .trigger('focus');
            });

        $list
            .off('click.congressContactEmails', selectors.contactEmailRemove)
            .on('click.congressContactEmails', selectors.contactEmailRemove, function () {
                const $rows = $list.find(selectors.contactEmailRow);
                const $row = $(this).closest(selectors.contactEmailRow);

                if ($rows.length === 1) {
                    $row.find('input[type="text"], input[type="email"]').val('');
                    $row.find('input[type="checkbox"]').prop('checked', false);
                    $row.find(selectors.contactEmailPrimary).prop('checked', true);
                    $row.find('input[name$=".IsVisibleOnPortal"]').prop('checked', true);
                    $row.find('input[name$=".ReceivesContactMessages"]').prop('checked', true);
                    return;
                }

                $row.remove();
                reindexContactEmails($form);
                ensurePrimarySelection();
            })
            .off('change.congressContactEmails', selectors.contactEmailPrimary)
            .on('change.congressContactEmails', selectors.contactEmailPrimary, function () {
                if (!$(this).prop('checked')) {
                    ensurePrimarySelection();
                    return;
                }

                $list.find(selectors.contactEmailPrimary)
                    .not(this)
                    .prop('checked', false);
            });

        reindexContactEmails($form);
        ensurePrimarySelection();
    }

    function buildContactEmailRow(index) {
        const prefix = 'ContactEmails[' + index + ']';

        return [
            '<div class="border rounded-3 p-16" data-contact-email-row>',
            '  <div class="row g-3 align-items-end">',
            '    <div class="col-lg-3">',
            '      <label class="form-label text-sm fw-semibold mb-1">Etiket</label>',
            '      <input class="form-control radius-8 h-48-px" name="' + prefix + '.Label" placeholder="Örn: Destek" />',
            '      <span class="text-danger field-validation-valid" data-valmsg-for="' + prefix + '.Label" data-valmsg-replace="true"></span>',
            '    </div>',
            '    <div class="col-lg-4">',
            '      <label class="form-label text-sm fw-semibold mb-1">E-posta <span class="text-danger">*</span></label>',
            '      <input class="form-control radius-8 h-48-px" name="' + prefix + '.Email" type="email" placeholder="destek@example.com" />',
            '      <span class="text-danger field-validation-valid" data-valmsg-for="' + prefix + '.Email" data-valmsg-replace="true"></span>',
            '    </div>',
            '    <div class="col-lg-4">',
            '      <div class="d-flex flex-wrap gap-3 pb-2">',
            checkboxMarkup(prefix, 'IsPrimary', 'Ana adres', false, 'data-contact-email-primary'),
            checkboxMarkup(prefix, 'IsVisibleOnPortal', 'Portalda göster', true, ''),
            checkboxMarkup(prefix, 'ReceivesContactMessages', 'Mesajları alsın', true, ''),
            '      </div>',
            '    </div>',
            '    <div class="col-lg-1 text-lg-end">',
            '      <button type="button" class="btn btn-sm btn-outline-danger radius-8 w-40-px h-40-px d-inline-flex align-items-center justify-content-center" data-contact-email-remove title="E-posta adresini kaldır">',
            '        <i class="ri-delete-bin-line"></i>',
            '      </button>',
            '    </div>',
            '  </div>',
            '</div>'
        ].join('');
    }

    function checkboxMarkup(prefix, property, label, checked, extraAttribute) {
        const name = prefix + '.' + property;

        return [
            '<label class="form-check d-inline-flex align-items-center gap-2 mb-0">',
            '<input class="form-check-input" type="checkbox" name="' + name + '" value="true" ' +
                (checked ? 'checked ' : '') + extraAttribute + ' />',
            '<input type="hidden" name="' + name + '" value="false" />',
            '<span class="form-check-label text-sm">' + label + '</span>',
            '</label>'
        ].join('');
    }

    function reindexContactEmails($form) {
        $form.find(selectors.contactEmailRow).each(function (index) {
            const $row = $(this);

            $row.find('[name]').each(function () {
                const $field = $(this);
                const currentName = String($field.attr('name') || '');
                const propertyMatch = currentName.match(/ContactEmails\[\d+\]\.(.+)$/);

                if (!propertyMatch) {
                    return;
                }

                $field.attr('name', 'ContactEmails[' + index + '].' + propertyMatch[1]);
            });

            $row.find('[data-valmsg-for]').each(function () {
                const $message = $(this);
                const currentName = String($message.attr('data-valmsg-for') || '');
                const propertyMatch = currentName.match(/ContactEmails\[\d+\]\.(.+)$/);

                if (!propertyMatch) {
                    return;
                }

                $message.attr('data-valmsg-for', 'ContactEmails[' + index + '].' + propertyMatch[1]);
            });
        });
    }

    function initializeSelect2Fields($form) {
        if (!window.jQuery || typeof $.fn.select2 !== 'function') {
            console.error(
                'Select2 yüklenemedi. /lib/select2/dist/js/select2.min.js dosyasını ve script sırasını kontrol edin.'
            );
            return;
        }

        const pageCulture = getCurrentCulture().toLowerCase();
        const language = pageCulture.startsWith('tr') ? 'tr' : 'en';

        $form.find(selectors.select2).each(function () {
            const $select = $(this);

            if ($select.hasClass('select2-hidden-accessible')) {
                $select.select2('destroy');
            }

            $select.select2({
                dropdownParent: $(document.body),
                placeholder: $select.find('option:first').text() || 'Lütfen seçiniz',
                allowClear: true,
                minimumResultsForSearch: 0,
                language: language
            });
        });
    }

    function initializeLocationSelectors($form) {
        const $country = $form.find(selectors.country);
        const $state = $form.find(selectors.state);
        const statesUrl = String($form.data('states-url') || '');

        if (!$country.length || !$state.length) {
            return;
        }

        // Edit ekranında server Model.CountryId ve Model.StateId ile seçenekleri
        // zaten seçili üretir. İlk açılışta bu seçimi kesinlikle temizlemiyoruz.
        const initialCountryId = String($country.val() || '');
        const initialStateId = String($state.val() || '');

        function resetState() {
            $state.empty()
                .append($('<option />').val('').text('Lütfen seçiniz'))
                .prop('disabled', true)
                .val('');

            syncSelect2($state);
        }

        function fillStates(items, selectedStateId) {
            $state.empty()
                .append($('<option />').val('').text('Lütfen seçiniz'));

            if (!Array.isArray(items) || items.length === 0) {
                $state.prop('disabled', true).val('');
                syncSelect2($state);
                return;
            }

            items.forEach(function (item) {
                const value = String(item.value || item.Value || '');
                const option = $('<option />')
                    .val(value)
                    .text(item.text || item.Text || '');

                if (selectedStateId && value === String(selectedStateId)) {
                    option.prop('selected', true);
                }

                $state.append(option);
            });

            $state.prop('disabled', false);

            if (selectedStateId) {
                $state.val(String(selectedStateId));
            }

            syncSelect2($state);
        }

        function loadStates(countryId, selectedStateId) {
            if (!countryId || !statesUrl) {
                resetState();
                return;
            }

            $state.prop('disabled', true);
            syncSelect2($state);

            $.getJSON(statesUrl, { countryId: countryId })
                .done(function (items) {
                    fillStates(items, selectedStateId);
                })
                .fail(resetState);
        }

        $country
            .off('change.congressLocation')
            .on('change.congressLocation', function () {
                const countryId = String($country.val() || '');

                // Kullanıcı ülkeyi değiştirdiğinde eski il seçimi artık geçersizdir.
                loadStates(countryId, null);
            });

        if (initialCountryId) {
            // StateOptions controller tarafından yalnızca seçili ülkeye göre
            // doldurulduğu için mevcut StateId ilk renderda korunur.
            $state.prop('disabled', false);

            if (initialStateId) {
                $state.val(initialStateId);
            }

            syncSelect2($state);
        } else {
            resetState();
        }
    }

    function syncSelect2($select) {
        if (typeof $.fn.select2 === 'function' &&
            $select.hasClass('select2-hidden-accessible')) {
            $select.trigger('change.select2');
        }
    }

    function initializeDropzones($container) {
        if (window.Symplify.Dropzone && typeof window.Symplify.Dropzone.initAll === 'function') {
            window.Symplify.Dropzone.initAll($container, {
                selector: selectors.dropzone,
                maxSizeMb: 5,
                invalidFileText: text('BackOffice.Congresses.Validation.InvalidLogo', 'Logo PNG, JPG, WEBP veya SVG olmalıdır.'),
                fileTooLargeText: text('Common.FileTooLarge', 'Dosya boyutu çok büyük.'),
                showImagePreview: true
            });
        }
    }

    function bindSubmitMode($form) {
        $(document)
            .off('click.congressesUpdate', selectors.submitButton)
            .on('click.congressesUpdate', selectors.submitButton, function (event) {
                event.preventDefault();

                const $button = $(this);

                if ($button.prop('disabled') || isSubmitting) {
                    return;
                }

                const submitMode = $button.data('submit-mode') || 'save';

                $(selectors.submitMode).first().val(submitMode);
                $form.trigger('submit');
            });
    }

    function bindSubmit($form) {
        $form
            .off('submit.congressesUpdate')
            .on('submit.congressesUpdate', submitForm);
    }

    function submitForm(event) {
        event.preventDefault();

        if (isSubmitting) {
            return;
        }

        const $form = $(this);

        prepareForSubmit($form);

        if (hasJQueryValidation() && !$form.valid()) {
            focusFirstInvalidField($form);
            return;
        }

        isSubmitting = true;
        setBusy($form, true);

        postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    if (renderValidationErrors($form, response)) {
                        return;
                    }

                    showError(response);
                    return;
                }

                showSuccess(response.message || text('BackOffice.Congresses.Messages.Updated', 'Kongre başarıyla güncellendi.'));
                redirectIfNeeded($form, response);
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) {
                    return;
                }

                showError(xhr);
            })
            .always(function () {
                isSubmitting = false;
                setBusy($form, false);
            });
    }

    function prepareForSubmit($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.prepareForSubmit === 'function') {
            window.Symplify.Forms.prepareForSubmit($form);
            return;
        }

        clearValidationErrors($form);
    }

    function postForm($form) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.postForm === 'function') {
            return window.Symplify.Ajax.postForm($form, { multipart: isMultipartForm($form) });
        }

        const ajaxOptions = {
            url: $form.attr('action'),
            type: $form.attr('method') || 'POST',
            headers: buildAjaxHeaders($form)
        };

        if (isMultipartForm($form)) {
            ajaxOptions.data = new FormData($form[0]);
            ajaxOptions.processData = false;
            ajaxOptions.contentType = false;
        } else {
            ajaxOptions.data = $form.serialize();
        }

        return $.ajax(ajaxOptions);
    }

    function renderValidationErrors($form, response) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.renderValidationErrors === 'function') {
            return window.Symplify.Forms.renderValidationErrors($form, response);
        }

        const payload = response && response.responseJSON ? response.responseJSON : response;
        const errors = payload && payload.errors ? payload.errors : null;

        if (!errors) {
            return false;
        }

        Object.keys(errors).forEach(function (fieldName) {
            const value = errors[fieldName];
            const message = Array.isArray(value) ? value[0] : value;
            const escapedFieldName = escapeSelector(fieldName);

            $form.find('[data-valmsg-for="' + escapedFieldName + '"]')
                .removeClass('field-validation-valid')
                .addClass('field-validation-error')
                .text(normalizeMessage(message));

            $form.find('[name="' + escapedFieldName + '"]')
                .addClass('input-validation-error is-invalid');
        });

        focusFirstInvalidField($form);
        return true;
    }

    function clearValidationErrors($form) {
        $form.find('.input-validation-error, .is-invalid')
            .removeClass('input-validation-error is-invalid');

        $form.find('.field-validation-error')
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .empty();

        $form.find('.js-congress-validation-summary')
            .addClass('d-none')
            .empty();
    }

    function focusFirstInvalidField($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.focusFirstInvalidField === 'function') {
            window.Symplify.Forms.focusFirstInvalidField($form);
            return;
        }

        const $field = $form.find('.input-validation-error, .is-invalid, .field-validation-error:visible').first();

        if (!$field.length) {
            return;
        }

        const $input = $field.is(':input')
            ? $field
            : $field.closest('.form-group, .mb-3, .mb-4, .col-12, .col-md-6, .col-lg-6').find(':input').first();

        window.setTimeout(function () {
            if ($input.length) {
                $input.trigger('focus');
            }
        }, 150);
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

    function initializeValidation($form) {
        if (!hasJQueryValidation()) {
            return;
        }

        $form.removeData('validator');
        $form.removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse($form);
    }

    function buildAjaxHeaders($form) {
        const headers = {
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json',
            'X-Culture': getCurrentCulture()
        };

        const token = $form.find('input[name="__RequestVerificationToken"]').first().val();

        if (token) {
            headers.RequestVerificationToken = token;
        }

        return headers;
    }

    function showSuccess(message) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function') {
            window.Symplify.Ajax.showSuccess(message);
            return;
        }

        console.info(message);
    }

    function showError(response) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(response);
            return;
        }

        alert(normalizeMessage(response) || text('Common.GenericError', 'İşlem sırasında bir hata oluştu.'));
    }

    function normalizeMessage(value) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.normalizeMessage === 'function') {
            return window.Symplify.Ajax.normalizeMessage(value);
        }

        if (value === null || value === undefined) {
            return null;
        }

        if (typeof value === 'object') {
            return normalizeMessage(value.responseJSON || value.message || value.title || value.detail || value.error || value.responseText);
        }

        const result = String(value).trim();
        return result.length ? result : null;
    }

    function getCurrentCulture() {
        const htmlCulture = document.documentElement.getAttribute('lang') || $('html').attr('lang');

        if (htmlCulture) {
            return htmlCulture;
        }

        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : 'tr-TR';
    }

    function text(key, fallback) {
        return window.Symplify && typeof window.Symplify.t === 'function'
            ? window.Symplify.t(key, fallback)
            : fallback;
    }

    function escapeHtml(value) {
        return $('<div/>').text(value || '').html();
    }

    function escapeSelector(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') {
            return window.CSS.escape(value);
        }

        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|\/@])/g, '\\$1');
    }

    return {
        init: init
    };
})(jQuery);

$(function () {
    window.Symplify.Congresses.Update.init();
});
