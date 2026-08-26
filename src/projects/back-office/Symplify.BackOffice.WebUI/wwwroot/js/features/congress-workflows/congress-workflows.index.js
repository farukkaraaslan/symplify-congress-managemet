window.Symplify = window.Symplify || {};
window.Symplify.CongressWorkflows = window.Symplify.CongressWorkflows || {};

window.Symplify.CongressWorkflows.Index = (function ($) {
    'use strict';

    const selectors = {
        panel: '#congressWorkflowPanel',
        form: '#applyCongressWorkflowTemplateForm',
        templateSelect: '#congressWorkflowTemplateSelect',
        replaceSwitch: '#replaceExistingTransitionsSwitch',
        refreshButton: '.js-congress-workflow-refresh',
        summary: '.js-current-workflow-summary',
        emptySummary: '.js-current-workflow-empty',
        currentTemplate: '.js-current-workflow-template',
        currentActive: '.js-current-workflow-active',
        currentInitialStatus: '.js-current-workflow-initial-status',
        currentTransitionCount: '.js-current-workflow-transition-count',
        transitionsTableBody: '#congressWorkflowTransitionsTable tbody'
    };

    let templates = [];
    let workflow = null;

    function init() {
        if (!$(selectors.panel).length) return;

        bindEvents();
        loadWorkflow();
    }

    function bindEvents() {
        $(document)
            .off('submit.congressWorkflowApply', selectors.form)
            .on('submit.congressWorkflowApply', selectors.form, applyTemplate);

        $(document)
            .off('click.congressWorkflowRefresh', selectors.refreshButton)
            .on('click.congressWorkflowRefresh', selectors.refreshButton, function () { loadWorkflow(); });

        $(document)
            .off('change.congressWorkflowTemplate', selectors.templateSelect)
            .on('change.congressWorkflowTemplate', selectors.templateSelect, function () {
                clearFieldValidation($(selectors.form), 'WorkflowTemplateId');
            });
    }

    function loadWorkflow() {
        const $panel = $(selectors.panel);
        const sourceUrl = $panel.data('source-url');

        if (!sourceUrl) return;

        setPanelBusy(true);

        $.get(sourceUrl)
            .done(function (response) {
                if (!response || response.success !== true) {
                    showError(response);
                    return;
                }

                templates = response.templates || [];
                workflow = response.workflow || null;

                renderTemplateOptions();
                renderCurrentWorkflow();
                renderTransitions();
            })
            .fail(showError)
            .always(function () { setPanelBusy(false); });
    }

    function renderTemplateOptions() {
        const $select = $(selectors.templateSelect);
        const currentValue = workflow && workflow.sourceWorkflowTemplateId ? workflow.sourceWorkflowTemplateId : '';
        const selectedValue = $select.val() || currentValue;

        $select.empty();
        $select.append($('<option/>').attr('value', '').text(text('templatePlaceholder', 'workflow şablonu seçiniz')));

        templates.forEach(function (template) {
            if (!template || !template.id) return;

            const labelParts = [];
            labelParts.push(template.name || template.code || template.id);

            if (template.isDefault === true) {
                labelParts.push('(' + text('defaultBadge', 'Varsayılan') + ')');
            }

            if (template.initialTransactionStatusName) {
                labelParts.push('- ' + template.initialTransactionStatusName);
            }

            $select.append($('<option/>')
                .attr('value', template.id)
                .text(labelParts.join(' ')));
        });

        if (selectedValue) {
            $select.val(selectedValue);
        }

        if (!templates.length) {
            showFieldValidation($(selectors.form), 'WorkflowTemplateId', $(selectors.panel).data('empty-template-text') || text('noTemplates', 'Aktif workflow şablonu bulunamadı.'));
        }
    }

    function renderCurrentWorkflow() {
        const $panel = $(selectors.panel);
        const $summary = $(selectors.summary);
        const $empty = $(selectors.emptySummary);

        if (!workflow || !workflow.sourceWorkflowTemplateId) {
            $summary.addClass('d-none');
            $empty.removeClass('d-none').text($panel.data('no-workflow-text') || text('noWorkflow', 'Bu kongreye henüz workflow şablonu uygulanmamış.'));
            return;
        }

        const selectedTemplate = templates.find(function (template) {
            return equalsGuid(template.id, workflow.sourceWorkflowTemplateId);
        });

        $empty.addClass('d-none').text('');
        $summary.removeClass('d-none');

        $(selectors.currentTemplate).text(selectedTemplate ? (selectedTemplate.name || selectedTemplate.code) : workflow.sourceWorkflowTemplateId);
        $(selectors.currentActive).text(workflow.isActive ? text('active', 'Aktif') : text('passive', 'Pasif'));
        $(selectors.currentInitialStatus).text(workflow.initialTransactionStatusName || '-');
        $(selectors.currentTransitionCount).text((workflow.transitions || []).length);
    }

    function renderTransitions() {
        const $tbody = $(selectors.transitionsTableBody);
        const transitions = workflow && Array.isArray(workflow.transitions) ? workflow.transitions : [];

        $tbody.empty();

        if (!transitions.length) {
            $tbody.append(
                $('<tr/>').append(
                    $('<td/>')
                        .attr('colspan', 5)
                        .addClass('text-center text-neutral-500 py-24')
                        .text($(selectors.panel).data('empty-transition-text') || text('noTransitions', 'Bu kongre için workflow geçişi bulunamadı.'))
                )
            );
            return;
        }

        transitions.forEach(function (transition) {
            const $row = $('<tr/>');

            $row.append($('<td/>').addClass('text-nowrap').text(transition.order || 0));
            $row.append($('<td/>').text(transition.transitionName || '-'));
            $row.append($('<td/>').text(transition.fromStatusName || '-'));
            $row.append($('<td/>').text(transition.toStatusName || '-'));
            $row.append($('<td/>').addClass('text-nowrap').append(renderStatusBadge(transition.isActive)));

            $tbody.append($row);
        });
    }

    function applyTemplate(event) {
        event.preventDefault();

        const $form = $(this);
        const $panel = $(selectors.panel);
        const templateId = $(selectors.templateSelect).val();

        clearValidation($form);

        if (!templateId) {
            showFieldValidation($form, 'WorkflowTemplateId', $panel.data('template-required-message') || text('templateRequired', 'Workflow şablonu seçimi zorunludur.'));
            focusFirstInvalidField($form);
            return;
        }

        confirmApply().then(function (confirmed) {
            if (!confirmed) return;

            setFormBusy($form, true);

            $.ajax({
                url: $panel.data('apply-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: {
                    CongressId: $panel.data('congress-id'),
                    WorkflowTemplateId: templateId,
                    ReplaceExistingTransitions: $(selectors.replaceSwitch).is(':checked')
                }
            })
                .done(function (response) {
                    if (!response || response.success !== true) {
                        if (renderValidationErrors($form, response)) return;
                        showError(response);
                        return;
                    }

                    showSuccess(response.message || text('templateApplied', 'Workflow şablonu kongreye başarıyla uygulandı.'));
                    loadWorkflow();
                })
                .fail(function (xhr) {
                    if (renderValidationErrors($form, xhr)) return;
                    showError(xhr);
                })
                .always(function () { setFormBusy($form, false); });
        });
    }

    function confirmApply() {
        const $panel = $(selectors.panel);

        if (!window.Swal) {
            return Promise.resolve(true);
        }

        return Swal.fire({
            icon: 'question',
            title: $panel.data('apply-confirm-title') || text('applyConfirmTitle', 'Workflow şablonu uygulansın mı?'),
            text: $panel.data('apply-confirm-text') || text('applyConfirmText', 'Mevcut geçişler seçilen şablona göre güncellenecek.'),
            showCancelButton: true,
            confirmButtonText: $panel.data('apply-confirm-button') || text('apply', 'Şablonu Uygula'),
            cancelButtonText: text('cancel', 'Vazgeç')
        }).then(function (result) {
            return result && result.isConfirmed === true;
        });
    }

    function renderStatusBadge(isActive) {
        return $('<span/>')
            .addClass(isActive ? 'badge bg-success-focus text-success-main' : 'badge bg-danger-focus text-danger-main')
            .text(isActive ? text('active', 'Aktif') : text('passive', 'Pasif'));
    }

    function renderValidationErrors($form, responseOrXhr) {
        const response = normalizeResponse(responseOrXhr);
        if (!response || !response.errors) return false;

        Object.keys(response.errors).forEach(function (fieldName) {
            const messages = response.errors[fieldName];
            if (!messages || !messages.length) return;
            showFieldValidation($form, fieldName, messages[0]);
        });

        focusFirstInvalidField($form);
        return true;
    }

    function showFieldValidation($form, fieldName, message) {
        const $field = $form.find('[name="' + fieldName + '"]');
        const $message = $form.find('[data-valmsg-for="' + fieldName + '"]');

        $field.addClass('input-validation-error is-invalid');
        $message
            .removeClass('field-validation-valid')
            .addClass('field-validation-error')
            .text(message || text('invalidField', 'Geçersiz değer.'));
    }

    function clearFieldValidation($form, fieldName) {
        const $field = $form.find('[name="' + fieldName + '"]');
        const $message = $form.find('[data-valmsg-for="' + fieldName + '"]');

        $field.removeClass('input-validation-error is-invalid');
        $message
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .text('');
    }

    function clearValidation($form) {
        $form.find('.input-validation-error, .is-invalid').removeClass('input-validation-error is-invalid');
        $form.find('[data-valmsg-for]')
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .text('');
    }

    function focusFirstInvalidField($form) {
        const $field = $form.find('.input-validation-error, .is-invalid').first();
        if ($field.length) $field.trigger('focus');
    }

    function setPanelBusy(isBusy) {
        $(selectors.refreshButton).prop('disabled', isBusy);
    }

    function setFormBusy($form, isBusy) {
        $form.find('button[type="submit"]').prop('disabled', isBusy);
        $form.find('select,input,button').not(selectors.refreshButton).prop('disabled', isBusy);
    }

    function buildAjaxHeaders($panel) {
        const token = $panel.find('input[name="__RequestVerificationToken"]').val()
            || $('input[name="__RequestVerificationToken"]').first().val();

        return token ? { RequestVerificationToken: token } : {};
    }

    function showSuccess(message) {
        if (window.Swal) {
            Swal.fire({
                icon: 'success',
                title: $(selectors.panel).data('success-title') || text('success', 'Başarılı'),
                text: message,
                confirmButtonText: text('ok', 'Tamam')
            });
            return;
        }

        alert(message);
    }

    function showError(responseOrXhr) {
        const response = normalizeResponse(responseOrXhr);
        const message = response && response.message
            ? response.message
            : text('genericError', 'İşlem sırasında bir hata oluştu.');

        if (window.Swal) {
            Swal.fire({
                icon: 'error',
                title: $(selectors.panel).data('error-title') || text('error', 'Hata'),
                text: message,
                confirmButtonText: text('ok', 'Tamam')
            });
            return;
        }

        alert(message);
    }

    function normalizeResponse(responseOrXhr) {
        if (!responseOrXhr) return null;

        if (responseOrXhr.responseJSON) return responseOrXhr.responseJSON;

        if (responseOrXhr.responseText) {
            try {
                return JSON.parse(responseOrXhr.responseText);
            } catch (error) {
                return { message: responseOrXhr.responseText };
            }
        }

        return responseOrXhr;
    }

    function text(key, fallback) {
        if (window.Symplify && typeof window.Symplify.t === 'function') {
            const value = window.Symplify.t(key);
            if (value && value !== key) return value;
        }

        const dictionary = {
            templatePlaceholder: 'workflow şablonu seçiniz',
            defaultBadge: 'Varsayılan',
            noTemplates: 'Aktif workflow şablonu bulunamadı.',
            noWorkflow: 'Bu kongreye henüz workflow şablonu uygulanmamış.',
            noTransitions: 'Bu kongre için workflow geçişi bulunamadı.',
            templateRequired: 'Workflow şablonu seçimi zorunludur.',
            templateApplied: 'Workflow şablonu kongreye başarıyla uygulandı.',
            applyConfirmTitle: 'Workflow şablonu uygulansın mı?',
            applyConfirmText: 'Mevcut geçişler seçilen şablona göre güncellenecek.',
            apply: 'Şablonu Uygula',
            cancel: 'Vazgeç',
            ok: 'Tamam',
            success: 'Başarılı',
            error: 'Hata',
            active: 'Aktif',
            passive: 'Pasif',
            invalidField: 'Geçersiz değer.',
            genericError: 'İşlem sırasında bir hata oluştu.'
        };

        return dictionary[key] || fallback || key;
    }

    function equalsGuid(left, right) {
        return String(left || '').toLowerCase() === String(right || '').toLowerCase();
    }

    return { init: init, reload: loadWorkflow };
})(jQuery);

$(function () {
    window.Symplify.CongressWorkflows.Index.init();
});
