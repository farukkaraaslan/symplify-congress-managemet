window.Symplify = window.Symplify || {};
window.Symplify.Congresses = window.Symplify.Congresses || {};

window.Symplify.Congresses.Create = (function ($) {
    'use strict';

    const selectors = {
        form: '#congressCreateForm, form[action*="/Congresses/Create"], form[action*="/congresses/create"]',
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

        function resetState() {
            $state.empty()
                .append($('<option />').val('').text('Lütfen seçiniz'))
                .prop('disabled', true)
                .val('');

            syncSelect2($state);
        }

        function fillStates(items) {
            resetState();

            if (!Array.isArray(items) || items.length === 0) {
                return;
            }

            items.forEach(function (item) {
                $state.append(
                    $('<option />')
                        .val(item.value || item.Value)
                        .text(item.text || item.Text)
                );
            });

            $state.prop('disabled', false);
            syncSelect2($state);
        }

        $country
            .off('change.congressLocation')
            .on('change.congressLocation', function () {
                const countryId = String($country.val() || '');

                resetState();

                if (!countryId || !statesUrl) {
                    return;
                }

                $.getJSON(statesUrl, { countryId: countryId })
                    .done(fillStates)
                    .fail(resetState);
            });

        $state.prop('disabled', !$country.val());
        syncSelect2($state);
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
            .off('click.congressesCreate', selectors.submitButton)
            .on('click.congressesCreate', selectors.submitButton, function (event) {
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
            .off('submit.congressesCreate')
            .on('submit.congressesCreate', submitForm);
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

                showSuccess(response.message || text('BackOffice.Congresses.Messages.Created', 'Kongre başarıyla oluşturuldu.'));
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
        reindexContactEmails($form);

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
    window.Symplify.Congresses.Create.init();
});

(function ($) {
    'use strict';

    const selectors = {
        form: '#congressCreateForm',
        organization: '#OrganizationId',
        enabled: '[data-clone-enabled]',
        panel: '[data-clone-panel]',
        source: '[data-clone-source]',
        module: '[data-clone-module]',
        selectAll: '[data-clone-select-all]',
        dateShift: '[data-clone-date-shift]',
        status: '[data-clone-status]',
        modal: '[data-clone-modal]',
        defaultWelcomeContent: '.js-default-congress-welcome-content'
    };

    function initCongressCloneOptions() {
        const $form = $(selectors.form);

        if (!$form.length) {
            return;
        }

        const $enabled = $form.find(selectors.enabled);
        const $panel = $form.find(selectors.panel);
        const $source = $form.find(selectors.source);
        const $modules = $form.find(selectors.module);
        const $selectAll = $form.find(selectors.selectAll);
        const $dateShift = $form.find(selectors.dateShift);
        const $status = $form.find(selectors.status);
        const $cloneModal = $form.find(selectors.modal);
        const $organization = $form.find(selectors.organization);

        function filterSourceCongresses() {
            const organizationId = String($organization.val() || '').toLowerCase();
            const selectedValue = String($source.val() || '');
            let selectedStillAvailable = false;
            let availableCount = 0;

            $source.find('option').each(function () {
                const $option = $(this);
                const optionValue = String($option.val() || '');

                if (!optionValue) {
                    $option.prop('hidden', false).prop('disabled', false);
                    return;
                }

                const optionOrganizationId = String(
                    $option.data('organization-id') || ''
                ).toLowerCase();

                const isAvailable = Boolean(organizationId) &&
                    optionOrganizationId === organizationId;

                $option
                    .prop('hidden', !isAvailable)
                    .prop('disabled', !isAvailable);

                if (isAvailable) {
                    availableCount += 1;
                }

                if (isAvailable && optionValue === selectedValue) {
                    selectedStillAvailable = true;
                }
            });

            if (!selectedStillAvailable) {
                $source.val('');
            }

            $source.prop(
                'disabled',
                !$enabled.prop('checked') || availableCount === 0
            );
        }

        function copiesGeneralInformation() {
            const generalInformationValue = '1';

            return $enabled.prop('checked') &&
                $modules.filter('[value="' + generalInformationValue + '"]:checked').length > 0;
        }

        function updateConditionalRequiredRules() {
            const isCopied = copiesGeneralInformation();
            const $conditionalFields = $form.find(
                selectors.defaultWelcomeContent
            );

            $conditionalFields.each(function () {
                const $field = $(this);
                const requiredMessage =
                    $field.attr('data-original-required-message') ||
                    $field.attr('data-val-required') ||
                    'Bu alan zorunludur.';

                if (!$field.attr('data-original-required-message')) {
                    $field.attr(
                        'data-original-required-message',
                        requiredMessage
                    );
                }

                if (isCopied) {
                    $field.removeAttr('data-val-required');

                    if ($.validator && typeof $field.rules === 'function') {
                        try {
                            $field.rules('remove', 'required');
                        } catch (error) {
                            // TinyMCE/validation adapter fieldi hazır değilse backend kontrolü devreye girer.
                        }
                    }

                    $field.removeClass('input-validation-error is-invalid');
                    $form.find(
                        '[data-valmsg-for="' +
                        String($field.attr('name') || '').replace(/"/g, '\\"') +
                        '"]'
                    ).empty();

                    return;
                }

                $field.attr('data-val-required', requiredMessage);

                if ($.validator && typeof $field.rules === 'function') {
                    try {
                        $field.rules('add', {
                            required: true,
                            messages: {
                                required: requiredMessage
                            }
                        });
                    } catch (error) {
                        // TinyMCE/validation adapter fieldi hazır değilse backend kontrolü devreye girer.
                    }
                }
            });
        }

        function updateSelectAllText() {
            const enabledModules = $modules.filter(':enabled');
            const allChecked = enabledModules.length > 0 &&
                enabledModules.filter(':checked').length === enabledModules.length;

            $selectAll.text(allChecked ? 'Tümünü Kaldır' : 'Tümünü Seç');
        }

        function updateCloneStatus() {
            const isEnabled = $enabled.prop('checked');

            if (!$status.length) {
                return;
            }

            if (!isEnabled) {
                $status
                    .removeClass('bg-success-focus text-success-600')
                    .addClass('bg-neutral-200 text-neutral-700')
                    .text('Kullanılmıyor');
                return;
            }

            const hasSource = Boolean($source.val());

            $status
                .removeClass('bg-neutral-200 text-neutral-700')
                .addClass('bg-success-focus text-success-600')
                .text(hasSource ? 'Aktif' : 'Kaynak bekleniyor');
        }

        function syncClonePanel() {
            const isEnabled = $enabled.prop('checked');

            $panel.toggleClass('d-none', !isEnabled);
            $modules.prop('disabled', !isEnabled);
            $dateShift.prop('disabled', !isEnabled);
            $selectAll.prop('disabled', !isEnabled);

            filterSourceCongresses();
            updateSelectAllText();
            updateConditionalRequiredRules();
            updateCloneStatus();
        }

        $enabled
            .off('change.congressClone')
            .on('change.congressClone', syncClonePanel);

        $organization
            .off('change.congressClone')
            .on('change.congressClone', function () {
                filterSourceCongresses();
                updateCloneStatus();
            });

        $source
            .off('change.congressClone')
            .on('change.congressClone', updateCloneStatus);

        $modules
            .off('change.congressClone')
            .on('change.congressClone', function () {
                updateSelectAllText();
                updateConditionalRequiredRules();
            });

        $selectAll
            .off('click.congressClone')
            .on('click.congressClone', function () {
                const enabledModules = $modules.filter(':enabled');
                const allChecked = enabledModules.length > 0 &&
                    enabledModules.filter(':checked').length === enabledModules.length;

                enabledModules.prop('checked', !allChecked);
                updateSelectAllText();
                updateConditionalRequiredRules();
            });

        if ($cloneModal.length) {
            $cloneModal
                .off('shown.bs.modal.congressClone')
                .on('shown.bs.modal.congressClone', function () {
                    filterSourceCongresses();
                    updateSelectAllText();
                    updateCloneStatus();
                });
        }

        syncClonePanel();
    }

    $(initCongressCloneOptions);
})(jQuery);
