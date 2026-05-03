window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplates = window.Symplify.WorkflowTemplates || {};

window.Symplify.WorkflowTemplates.update = (function ($) {
    'use strict';

    const selectors = {
        button: '.js-workflow-template-update-button',
        form: '#updateWorkflowTemplateForm',
        modal: '#updateWorkflowTemplateModal'
    };

    function init() {
        $(document).off('click.workflowTemplatesUpdate', selectors.button)
            .on('click.workflowTemplatesUpdate', selectors.button, handleOpen);

        $(document).off('submit.workflowTemplatesUpdate', selectors.form)
            .on('submit.workflowTemplatesUpdate', selectors.form, handleSubmit);
    }

    function handleOpen() {
        const id = $(this).data('id');
        const getForUpdateUrl = $('#workflowTemplatesTable').data('get-for-update-url') || window.Symplify.WorkflowTemplates?.urls?.getForUpdate;

        if (!id || !getForUpdateUrl) {
            return;
        }

        $.ajax({
            url: getForUpdateUrl,
            type: 'GET',
            data: { id: id },
            headers: getAjaxHeaders($(document))
        }).done(function (response) {
            if (!response || response.success !== true) {
                window.Symplify.Ajax.showError(response);
                return;
            }

            fillForm(response);
            $(selectors.modal).modal('show');
        }).fail(function (xhr) {
            window.Symplify.Ajax.showError(xhr);
        });
    }

    function handleSubmit(event) {
        event.preventDefault();

        const $form = $(this);

        if ($form.valid && !$form.valid()) {
            return;
        }

        window.Symplify.Ajax.postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    window.Symplify.Ajax.showError(response);
                    return;
                }

                window.Symplify.Ajax.showSuccess(response.message);
                $(selectors.modal).modal('hide');
                window.Symplify.WorkflowTemplates.table.reload(false);
            })
            .fail(function (xhr) {
                window.Symplify.Ajax.showError(xhr);
            });
    }

    function fillForm(response) {
        const $form = $(selectors.form);

        $form.find('[name="Id"]').val(response.id || '');
        $form.find('[name="Code"]').val(response.code || '');
        $form.find('[name="InitialTransactionStatusId"]').val(response.initialTransactionStatusId || '');
        $form.find('[name="IsDefault"]').prop('checked', response.isDefault === true);
        $form.find('[name="IsActive"]').prop('checked', response.isActive === true);

        (response.translations || []).forEach(function (translation, index) {
            $form.find('[name="Translations[' + index + '].LanguageId"]').val(translation.languageId || '');
            $form.find('[name="Translations[' + index + '].Culture"]').val(translation.culture || '');
            $form.find('[name="Translations[' + index + '].LanguageName"]').val(translation.languageName || '');
            $form.find('[name="Translations[' + index + '].IsDefault"]').val(String(translation.isDefault === true));
            $form.find('[name="Translations[' + index + '].Exists"]').val(String(translation.exists === true));
            $form.find('[name="Translations[' + index + '].Name"]').val(translation.name || '');
            $form.find('[name="Translations[' + index + '].Description"]').val(translation.description || '');
        });
    }

    function getAjaxHeaders($container) {
        const headers = { 'X-Culture': getCurrentCulture() };
        const token = window.Symplify.Ajax?.getAntiForgeryToken
            ? window.Symplify.Ajax.getAntiForgeryToken($container || $(document))
            : $('input[name="__RequestVerificationToken"]').first().val();

        if (token) {
            headers.RequestVerificationToken = token;
        }

        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }

    return { init: init };
})(jQuery);
