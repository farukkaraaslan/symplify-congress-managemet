window.Symplify = window.Symplify || {};
window.Symplify.SubmissionTypes = window.Symplify.SubmissionTypes || {};

window.Symplify.SubmissionTypes.Create = (function ($) {
    'use strict';

    const selectors = {
        modal: '#createSubmissionTypeModal',
        form: '#createSubmissionTypeForm'
    };

    function init() {
        initializeValidation($(selectors.form));
        bindStatusSwitch($(selectors.form));

        $(document)
            .off('submit.submission-typesCreate', selectors.form)
            .on('submit.submission-typesCreate', selectors.form, submitForm);

        $(document)
            .off('shown.bs.modal.submission-typesCreate', selectors.modal)
            .on('shown.bs.modal.submission-typesCreate', selectors.modal, function () {
                initializeValidation($(selectors.form));
                bindStatusSwitch($(selectors.form));
            });

        $(document)
            .off('hidden.bs.modal.submission-typesCreate', selectors.modal)
            .on('hidden.bs.modal.submission-typesCreate', selectors.modal, resetForm);
    }

    function submitForm(event) {
        event.preventDefault();

        const $form = $(this);

        initializeValidation($form);

        if (hasJQueryValidation() && !$form.valid()) {
            focusFirstInvalidField($form);
            return;
        }

        clearValidationErrors($form);

        postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    if (tryRenderValidationErrors($form, response)) {
                        return;
                    }

                    showError(response);
                    return;
                }

                hideModal(selectors.modal);
                resetForm();

                if (window.Symplify.SubmissionTypes.Table) {
                    window.Symplify.SubmissionTypes.Table.reload(true);
                }

                showSuccess(response.message || getText('created', 'Kayıt oluşturuldu.'));
            })
            .fail(function (xhr) {
                if (tryRenderValidationErrors($form, xhr)) {
                    return;
                }

                showError(xhr);
            });
    }

    function resetForm() {
        const $form = $(selectors.form);

        if (!$form.length) {
            return;
        }

        $form[0].reset();
        $form.find('input[name="Code"]').val('');
        $form.find('.js-lookup-status-switch').prop('checked', true).trigger('change');
        clearValidationErrors($form);
        showFirstTab($form);
    }

    function initializeValidation($form) {
        if (!$form || !$form.length || !hasJQueryValidation()) {
            return;
        }

        $form.removeData('validator');
        $form.removeData('unobtrusiveValidation');

        $.validator.unobtrusive.parse($form);
    }

    function hasJQueryValidation() {
        return typeof $.validator !== 'undefined' &&
            typeof $.validator.unobtrusive !== 'undefined';
    }

    function postForm($form) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.postForm === 'function') {
            return window.Symplify.Ajax.postForm($form);
        }

        return $.ajax({
            url: $form.attr('action'),
            type: $form.attr('method') || 'POST',
            data: $form.serialize()
        });
    }

    function tryRenderValidationErrors($form, response) {
        const payload = response?.responseJSON || response;
        const errors = payload?.errors;

        if (!errors) {
            return false;
        }

        const validator = $form.data('validator');
        const normalizedErrors = {};

        Object.keys(errors).forEach(function (key) {
            const messages = errors[key];
            const message = Array.isArray(messages) ? messages[0] : messages;

            if (!message) {
                return;
            }

            normalizedErrors[key] = message;
        });

        if (validator) {
            validator.showErrors(normalizedErrors);
        }

        renderValidationErrorsManually($form, normalizedErrors);
        focusFirstInvalidField($form);

        return true;
    }

    function renderValidationErrorsManually($form, errors) {
        Object.keys(errors).forEach(function (key) {
            const message = errors[key];
            const $message = $form.find('[data-valmsg-for="' + escapeSelector(key) + '"]');

            if ($message.length) {
                $message
                    .removeClass('field-validation-valid')
                    .addClass('field-validation-error')
                    .text(message);
            }

            const $field = $form.find('[name="' + escapeSelector(key) + '"]');

            if ($field.length) {
                $field.addClass('input-validation-error is-invalid');
            }
        });
    }

    function clearValidationErrors($form) {
        $form.find('.field-validation-error')
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .text('');

        $form.find('.input-validation-error, .is-invalid')
            .removeClass('input-validation-error is-invalid');
    }

    function focusFirstInvalidField($form) {
        const $field = $form.find('.input-validation-error, .is-invalid').first();

        if (!$field.length) {
            return;
        }

        const $tabPane = $field.closest('.tab-pane');

        if ($tabPane.length && !$tabPane.hasClass('active')) {
            const paneId = $tabPane.attr('id');
            const tabButton = document.querySelector('[data-bs-target="#' + paneId + '"]');

            if (tabButton && typeof bootstrap !== 'undefined') {
                bootstrap.Tab.getOrCreateInstance(tabButton).show();
            }
        }

        setTimeout(function () {
            $field.trigger('focus');
        }, 150);
    }

    function bindStatusSwitch($container) {
        $container.find('.js-lookup-status-switch').each(function () {
            updateStatusLabel($(this));
        });

        $container
            .off('change.lookupStatus')
            .on('change.lookupStatus', '.js-lookup-status-switch', function () {
                updateStatusLabel($(this));
            });
    }

    function updateStatusLabel($switch) {
        const $label = $switch.closest('.form-switch').find('.js-lookup-status-label');
        const isActive = $switch.is(':checked');

        $label
            .toggleClass('text-success-600', isActive)
            .toggleClass('text-danger-600', !isActive)
            .text(isActive ? getText('active', 'Aktif') : getText('passive', 'Pasif'));
    }

    function showFirstTab($form) {
        const firstTab = $form.find('[data-bs-toggle="pill"]').get(0);

        if (firstTab && typeof bootstrap !== 'undefined') {
            bootstrap.Tab.getOrCreateInstance(firstTab).show();
        }
    }

    function hideModal(selector) {
        const el = document.querySelector(selector);

        if (!el || typeof bootstrap === 'undefined') {
            return;
        }

        (bootstrap.Modal.getInstance(el) || new bootstrap.Modal(el)).hide();
    }

    function getText(key, fallback) {
        const texts = window.Symplify.SubmissionTypes.texts || window.Symplify.Texts || window.Symplify.texts || {};
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

    function escapeSelector(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') {
            return window.CSS.escape(value);
        }

        return String(value).replace(/([ #;&,.+*~':"!^$[\]()=>|/@])/g, '\$1');
    }

    return {
        init: init
    };
})(jQuery);
