window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplates = window.Symplify.WorkflowTemplates || {};

window.Symplify.WorkflowTemplates.table = (function ($) {
    'use strict';

    const selectors = {
        table: '#workflowTemplatesTable'
    };

    let table;

    function init() {
        const $table = $(selectors.table);

        if (!$table.length || $table.data('symplify-feature-table-initialized')) {
            return;
        }

        table = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            ordering: true,
            responsive: true,
            language: getDataTableLanguage(),
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders($(document)),
                beforeSend: function (xhr) {
                    const token = getAntiForgeryToken($(document));

                    if (token) {
                        xhr.setRequestHeader('RequestVerificationToken', token);
                    }

                    xhr.setRequestHeader('X-Culture', getCurrentCulture());
                },
                data: function (data) {
                    data.culture = getCurrentCulture();

                    const token = getAntiForgeryToken($(document));

                    if (token) {
                        data.__RequestVerificationToken = token;
                    }

                    return data;
                },
                error: function (xhr) {
                    showError(xhr);
                }
            },
            order: [[5, 'desc'], [1, 'asc']],
            columns: [
                {
                    data: 'rowNumber',
                    name: 'rowNumber',
                    orderable: false,
                    searchable: false
                },
                {
                    data: 'code',
                    name: 'code',
                    orderable: true,
                    searchable: true,
                    render: renderCode
                },
                {
                    data: 'name',
                    name: 'name',
                    orderable: true,
                    searchable: true,
                    render: renderText
                },
                {
                    data: 'description',
                    name: 'description',
                    orderable: false,
                    searchable: true,
                    render: renderDescription
                },
                {
                    data: 'initialTransactionStatusName',
                    name: 'initialStatus',
                    orderable: true,
                    searchable: false,
                    render: renderOptionalText
                },
                {
                    data: 'isDefault',
                    name: 'isDefault',
                    orderable: true,
                    searchable: false,
                    render: renderBoolean
                },
                {
                    data: 'isActive',
                    name: 'isActive',
                    orderable: true,
                    searchable: false,
                    render: renderStatus
                },
                {
                    data: 'id',
                    name: 'actions',
                    orderable: false,
                    searchable: false,
                    className: 'text-end text-nowrap',
                    render: renderActions
                }
            ]
        });

        $table.data('symplify-feature-table-initialized', true);
    }

    function reload(resetPaging) {
        if (table) {
            table.ajax.reload(null, resetPaging === true);
        }
    }

    function renderCode(value) {
        return '<span class="badge bg-neutral-100 text-neutral-700">' + escapeHtml(value || '') + '</span>';
    }

    function renderText(value) {
        return escapeHtml(value || '');
    }

    function renderOptionalText(value) {
        return value ? escapeHtml(value) : '<span class="text-neutral-400">-</span>';
    }

    function renderDescription(value) {
        if (!value) {
            return '<span class="text-neutral-400">-</span>';
        }

        return '<span title="' + escapeHtml(value) + '">' + escapeHtml(value) + '</span>';
    }

    function renderBoolean(value) {
        const label = value ? (getTexts().yes || 'Evet') : (getTexts().no || 'Hayır');
        const cssClass = value ? 'bg-success-focus text-success-main' : 'bg-neutral-100 text-neutral-600';

        return '<span class="px-16 py-4 rounded-pill fw-medium text-sm ' + cssClass + '">' + escapeHtml(label) + '</span>';
    }

    function renderStatus(isActive) {
        const label = isActive ? (getTexts().active || 'Aktif') : (getTexts().passive || 'Pasif');
        const cssClass = isActive ? 'bg-success-focus text-success-main' : 'bg-danger-focus text-danger-main';

        return '<span class="px-16 py-4 rounded-pill fw-medium text-sm ' + cssClass + '">' + escapeHtml(label) + '</span>';
    }

    function renderActions(id) {
        const texts = getTexts();
        const editText = texts.edit || 'Düzenle';
        const deleteText = texts.delete || 'Sil';
        const transitionsText = window.Symplify.t
            ? window.Symplify.t('BackOffice.WorkflowTemplateTransitions.PageTitle', 'Geçişler')
            : 'Geçişler';

        const transitionUrl = getTransitionIndexUrl(id);

        return '<div class="d-flex align-items-center justify-content-end gap-2">'
            + '<a href="' + escapeHtml(transitionUrl) + '" class="w-40-px h-40-px bg-info-50 text-info-600 rounded-circle d-inline-flex align-items-center justify-content-center" title="' + escapeHtml(transitionsText) + '"><i class="ri-flow-chart"></i></a>'
            + '<button type="button" class="w-40-px h-40-px bg-primary-50 text-primary-600 rounded-circle d-inline-flex align-items-center justify-content-center js-workflow-template-update-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(editText) + '"><i class="ri-pencil-line"></i></button>'
            + '<button type="button" class="w-40-px h-40-px bg-danger-50 text-danger-600 rounded-circle d-inline-flex align-items-center justify-content-center js-workflow-template-delete-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(deleteText) + '"><i class="ri-delete-bin-line"></i></button>'
            + '</div>';
    }

    function getTransitionIndexUrl(id) {
        const $table = $(selectors.table);
        const baseUrl = $table.data('transition-index-url') || window.Symplify.WorkflowTemplates?.urls?.transitionIndex || '';
        const separator = String(baseUrl).indexOf('?') >= 0 ? '&' : '?';

        return baseUrl + separator + 'workflowTemplateId=' + encodeURIComponent(id);
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }

    function getAjaxHeaders($container) {
        const headers = {
            'X-Culture': getCurrentCulture()
        };

        const token = getAntiForgeryToken($container || $(document));

        if (token) {
            headers.RequestVerificationToken = token;
        }

        return headers;
    }

    function getAntiForgeryToken($container) {
        if (window.Symplify.Ajax?.getAntiForgeryToken) {
            return window.Symplify.Ajax.getAntiForgeryToken($container || $(document));
        }

        return ($container || $(document))
            .find('input[name="__RequestVerificationToken"]')
            .first()
            .val();
    }

    function getDataTableLanguage() {
        return window.Symplify.DataTables?.language || window.Symplify.Lookup?.dataTableLanguage || {};
    }

    function getTexts() {
        return window.Symplify.WorkflowTemplates?.texts ||
            window.Symplify.Lookup?.texts ||
            window.Symplify.Texts ||
            window.Symplify.texts ||
            {};
    }

    function showError(xhr) {
        if (window.Symplify.Ajax?.showError) {
            window.Symplify.Ajax.showError(xhr);
            return;
        }

        alert(xhr?.responseJSON?.message || xhr?.message || getTexts().genericError || 'İşlem sırasında hata oluştu.');
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return '';
        }

        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    return {
        init: init,
        reload: reload
    };
})(jQuery);