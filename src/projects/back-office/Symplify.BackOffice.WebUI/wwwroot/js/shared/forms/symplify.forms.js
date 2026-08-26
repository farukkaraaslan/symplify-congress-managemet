window.Symplify = window.Symplify || {};

window.Symplify.Forms = (function ($) {
    'use strict';

    const selectors = {
        validationSummary: '.js-validation-summary, .js-congress-validation-summary, [data-symplify-validation-summary]',
        translationTabs: '[data-symplify-translation-tabs], .js-translation-tabs, .nav[role="tablist"]',
        editor: '[data-symplify-editor]'
    };

    function initialize($form) {
        if (!$form || !$form.length) return;

        initializeValidation($form);
        initializeEditors($form);
        initializeTranslationTabs($form);
        bindConfirmForms($form);
    }

    function prepareForSubmit($form) {
        syncEditors($form);
        clearValidationErrors($form);
    }

    function initializeValidation($form) {
        if (!hasJQueryValidation()) return;

        $form.removeData('validator');
        $form.removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse($form);

        const validator = $form.data('validator');
        if (validator) {
            validator.settings.ignore = ':hidden:not([data-symplify-editor])';
        }
    }

    function initializeEditors($container) {
        if (window.Symplify.TinyMce && typeof window.Symplify.TinyMce.initAll === 'function') {
            window.Symplify.TinyMce.initAll($container);
        }
    }

    function syncEditors($container) {
        if (window.Symplify.TinyMce && typeof window.Symplify.TinyMce.syncAll === 'function') {
            window.Symplify.TinyMce.syncAll($container);
        }
    }

    function initializeTranslationTabs($container) {
        const $scope = normalizeContainer($container);

        $scope.find(selectors.translationTabs).each(function () {
            const $tabs = $(this);
            const $buttons = $tabs.find('[data-bs-toggle="pill"], [data-bs-toggle="tab"]');

            $buttons.each(function () {
                const $button = $(this);
                if ($button.attr('data-symplify-tab-initialized') === 'true') return;

                $button.attr('data-symplify-tab-initialized', 'true');

                if ($button.find('.js-translation-tab-error-indicator').length) return;

                $button.append('<span class="badge bg-danger text-white rounded-pill ms-2 d-none js-translation-tab-error-indicator">!</span>');
            });
        });
    }

    function renderValidationErrors($form, response) {
        const payload = response?.responseJSON || response;
        const errors = payload?.errors;

        if (!errors) return false;

        const normalizedErrors = {};
        const summaryMessages = [];

        Object.keys(errors).forEach(function (key) {
            const messages = errors[key];
            const message = Array.isArray(messages) ? messages[0] : messages;
            const normalizedMessage = window.Symplify.Ajax && typeof window.Symplify.Ajax.normalizeMessage === 'function'
                ? window.Symplify.Ajax.normalizeMessage(message)
                : normalizeMessage(message);

            if (!normalizedMessage) return;

            normalizedErrors[key] = normalizedMessage;
            summaryMessages.push(normalizedMessage);
        });

        const fieldErrors = filterErrorsForExistingFields($form, normalizedErrors);
        const validator = $form.data('validator');
        if (validator && Object.keys(fieldErrors).length) {
            validator.showErrors(fieldErrors);
        }

        renderValidationErrorsManually($form, normalizedErrors);
        renderSummary($form, summaryMessages);
        markTabsWithErrors($form);
        focusFirstInvalidField($form);
        return true;
    }


    function filterErrorsForExistingFields($form, errors) {
        const fieldErrors = {};

        Object.keys(errors || {}).forEach(function (key) {
            const escapedKey = escapeSelector(key);
            const hasField = $form.find('[name="' + escapedKey + '"]').length > 0;

            if (hasField) {
                fieldErrors[key] = errors[key];
            }
        });

        return fieldErrors;
    }

    function renderValidationErrorsManually($form, errors) {
        Object.keys(errors).forEach(function (key) {
            const message = errors[key];
            const escapedKey = escapeSelector(key);
            const $message = $form.find('[data-valmsg-for="' + escapedKey + '"]');

            if ($message.length) {
                $message
                    .removeClass('field-validation-valid')
                    .addClass('field-validation-error')
                    .text(message);
            }

            const $field = $form.find('[name="' + escapedKey + '"]');
            if ($field.length) {
                $field.addClass('input-validation-error is-invalid');
            }
        });
    }

    function renderSummary($form, messages) {
        const $summary = $form.find(selectors.validationSummary).first();
        if (!$summary.length) return;

        if (!messages || !messages.length) {
            $summary.addClass('d-none').empty();
            return;
        }

        const html = '<ul class="mb-0">' + messages.map(function (message) {
            return '<li>' + escapeHtml(message) + '</li>';
        }).join('') + '</ul>';

        $summary.removeClass('d-none').html(html);
    }

    function clearValidationErrors($form) {
        $form.find('.input-validation-error, .is-invalid').removeClass('input-validation-error is-invalid');
        $form.find('.field-validation-error')
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .empty();
        renderSummary($form, []);
        clearTabErrorIndicators($form);
    }

    function focusFirstInvalidField($form) {
        const $field = $form.find('.input-validation-error, .is-invalid, .field-validation-error:visible').first();
        if (!$field.length) return;

        const $input = $field.is(':input')
            ? $field
            : $field.closest('.form-group, .mb-3, .mb-4, .col-12, .col-md-6, .col-lg-6').find(':input').first();

        activateTabForElement($input.length ? $input : $field);

        window.setTimeout(function () {
            if ($input.length && $input.is(selectors.editor) && window.Symplify.TinyMce && typeof window.Symplify.TinyMce.focusByName === 'function') {
                window.Symplify.TinyMce.focusByName($input.attr('name'));
            } else if ($input.length && typeof $input.trigger === 'function') {
                $input.trigger('focus');
            }
        }, 150);

        $('html, body').animate({ scrollTop: Math.max($field.offset().top - 120, 0) }, 200);
    }

    function reset($form) {
        if ($form && $form[0]) {
            $form[0].reset();
        }

        clearValidationErrors($form);
        resetTranslationTabs($form);

        if (window.Symplify.Dropzone && typeof window.Symplify.Dropzone.reset === 'function') {
            window.Symplify.Dropzone.reset($form);
        }
    }

    function resetTranslationTabs($container) {
        const $scope = normalizeContainer($container);

        $scope.find(selectors.translationTabs).each(function () {
            const $tabs = $(this);
            const $buttons = $tabs.find('[data-bs-toggle="pill"], [data-bs-toggle="tab"]');
            const $first = $buttons.first();

            if (!$first.length) return;

            if (typeof bootstrap !== 'undefined' && bootstrap.Tab) {
                bootstrap.Tab.getOrCreateInstance($first[0]).show();
            } else {
                $buttons.removeClass('active').first().addClass('active');
                const target = $first.attr('data-bs-target') || $first.attr('href');
                if (target) {
                    const $content = $(target).closest('.tab-content');
                    $content.find('.tab-pane').removeClass('show active');
                    $(target).addClass('show active');
                }
            }
        });
    }

    function markTabsWithErrors($form) {
        clearTabErrorIndicators($form);

        $form.find('.input-validation-error, .is-invalid, .field-validation-error').each(function () {
            const $element = $(this);
            const $pane = $element.closest('.tab-pane');
            if (!$pane.length || !$pane.attr('id')) return;

            const $button = $form.find('[data-bs-target="#' + escapeSelector($pane.attr('id')) + '"], [href="#' + escapeSelector($pane.attr('id')) + '"]');
            $button.find('.js-translation-tab-error-indicator').removeClass('d-none');
            $button.addClass('text-danger');
        });
    }

    function clearTabErrorIndicators($form) {
        $form.find('.js-translation-tab-error-indicator').addClass('d-none');
        $form.find('[data-bs-toggle="pill"], [data-bs-toggle="tab"]').removeClass('text-danger');
    }

    function activateTabForElement($element) {
        if (!$element || !$element.length) return;

        const $pane = $element.closest('.tab-pane');
        if (!$pane.length || !$pane.attr('id') || $pane.hasClass('active')) return;

        const selector = '[data-bs-target="#' + escapeSelector($pane.attr('id')) + '"], [href="#' + escapeSelector($pane.attr('id')) + '"]';
        const button = document.querySelector(selector);

        if (button && typeof bootstrap !== 'undefined' && bootstrap.Tab) {
            bootstrap.Tab.getOrCreateInstance(button).show();
            return;
        }

        const $button = $(selector);
        $button.closest('[role="tablist"]').find('.active').removeClass('active');
        $button.addClass('active');
        $pane.closest('.tab-content').find('.tab-pane').removeClass('show active');
        $pane.addClass('show active');
    }


    function bindConfirmForms(container) {
        const $scope = normalizeContainer(container);

        $scope.find('form[data-symplify-confirm-form], form[data-confirm-form], form.js-confirm-form')
            .addBack('form[data-symplify-confirm-form], form[data-confirm-form], form.js-confirm-form')
            .each(function () {
                const $form = $(this);
                if ($form.attr('data-symplify-confirm-bound') === 'true') return;
                $form.attr('data-symplify-confirm-bound', 'true');

                $form.on('submit.symplifyConfirmForm', function (event) {
                    if ($form.attr('data-symplify-confirmed') === 'true') {
                        $form.removeAttr('data-symplify-confirmed');
                        return true;
                    }

                    event.preventDefault();

                    const options = {
                        title: $form.data('confirmTitle') || $form.attr('data-confirm-title'),
                        text: $form.data('confirmText') || $form.attr('data-confirm-text'),
                        confirmButtonText: $form.data('confirmButtonText') || $form.attr('data-confirm-button-text'),
                        cancelButtonText: $form.data('confirmCancelText') || $form.attr('data-confirm-cancel-text'),
                        icon: $form.data('confirmIcon') || $form.attr('data-confirm-icon') || 'warning'
                    };

                    if (!window.Symplify.Ajax || typeof window.Symplify.Ajax.confirm !== 'function') {
                        HTMLFormElement.prototype.submit.call($form[0]);
                        return false;
                    }

                    window.Symplify.Ajax.confirm(options).then(function (result) {
                        if (!result || result.isConfirmed !== true) return;
                        $form.attr('data-symplify-confirmed', 'true');
                        HTMLFormElement.prototype.submit.call($form[0]);
                    });

                    return false;
                });
            });
    }

    function hasJQueryValidation() {
        return typeof $.validator !== 'undefined' && typeof $.validator.unobtrusive !== 'undefined';
    }

    function normalizeContainer(container) {
        if (!container) return $(document);
        return container.jquery ? container : $(container);
    }

    function normalizeMessage(value) {
        if (value === null || value === undefined) return null;
        if (Array.isArray(value)) return value.map(normalizeMessage).filter(Boolean).join('\n');
        if (typeof value === 'object') return normalizeMessage(value.message || value.title || value.detail || value.error);
        const text = String(value).trim();
        return text.length ? text : null;
    }

    function escapeHtml(value) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.escapeHtml === 'function') {
            return window.Symplify.Ajax.escapeHtml(value);
        }

        return $('<div/>').text(value || '').html();
    }

    function escapeSelector(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|/@])/g, '\\$1');
    }

    return {
        initialize: initialize,
        initializeValidation: initializeValidation,
        initializeEditors: initializeEditors,
        initializeTranslationTabs: initializeTranslationTabs,
        prepareForSubmit: prepareForSubmit,
        syncEditors: syncEditors,
        renderValidationErrors: renderValidationErrors,
        clearValidationErrors: clearValidationErrors,
        focusFirstInvalidField: focusFirstInvalidField,
        reset: reset,
        resetTranslationTabs: resetTranslationTabs,
        bindConfirmForms: bindConfirmForms,
        markTabsWithErrors: markTabsWithErrors,
        activateTabForElement: activateTabForElement
    };
})(jQuery);

$(function () {
    if (window.Symplify.Forms) {
        window.Symplify.Forms.initializeTranslationTabs(document);
        window.Symplify.Forms.bindConfirmForms(document);
    }
});
