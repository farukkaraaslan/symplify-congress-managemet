window.Symplify = window.Symplify || {};
window.Symplify.TransactionStatusTransitions = window.Symplify.TransactionStatusTransitions || {};

window.Symplify.TransactionStatusTransitions.Table = (function ($) {
    'use strict';

    let table;

    const selectors = {
        table: '#transactionStatusTransitionsTable'
    };

    function init() {
        const $table = $(selectors.table);

        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();

            if ($table.data('symplify-feature-table-initialized') === true) {
                return;
            }

            table.destroy();
            $table.find('tbody').empty();
        }

        table = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            ordering: true,
            paging: true,
            pageLength: 10,
            autoWidth: false,
            responsive: false,
            order: [[1, 'asc']],
            language: getDataTableLanguage(),
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders($(document)),
                data: function (data) {
                    data.culture = getCurrentCulture();
                    return data;
                },
                error: showError
            },
            columns: [
                {
                    data: 'rowNumber',
                    name: 'rowNumber',
                    orderable: false,
                    searchable: false,
                    className: 'text-nowrap'
                },
                {
                    data: null,
                    name: 'fromStatus',
                    orderable: true,
                    searchable: true,
                    render: function (data, type, row) {
                        return renderStatusRef(row.fromStatusName, row.fromStatusCode);
                    }
                },
                {
                    data: null,
                    name: 'toStatus',
                    orderable: true,
                    searchable: true,
                    render: function (data, type, row) {
                        return renderStatusRef(row.toStatusName, row.toStatusCode);
                    }
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

    function renderText(value) {
        return escapeHtml(value || '');
    }

    function renderStatusRef(name, code) {
        const safeName = name || '';
        const safeCode = code || '';

        if (!safeName && !safeCode) {
            return '<span class="text-neutral-400">-</span>';
        }

        if (!safeCode) {
            return escapeHtml(safeName);
        }

        if (!safeName) {
            return '<span class="badge bg-neutral-100 text-neutral-700">' + escapeHtml(safeCode) + '</span>';
        }

        return '' +
            '<div class="d-flex flex-column gap-1">' +
                '<span class="fw-medium">' + escapeHtml(safeName) + '</span>' +
                '<span class="badge bg-neutral-100 text-neutral-700 w-max-content">' + escapeHtml(safeCode) + '</span>' +
            '</div>';
    }

    function renderDescription(value) {
        const text = value || '';

        if (!text) {
            return '<span class="text-neutral-400">-</span>';
        }

        return '<span title="' + escapeHtml(text) + '">' + escapeHtml(text) + '</span>';
    }

    function renderStatus(isActive) {
        const texts = getTexts();
        const label = isActive ? (texts.active || 'Aktif') : (texts.passive || 'Pasif');
        const cssClass = isActive ? 'bg-success-focus text-success-main' : 'bg-danger-focus text-danger-main';

        return '<span class="px-16 py-4 rounded-pill fw-medium text-sm ' + cssClass + '">' + escapeHtml(label) + '</span>';
    }

    function renderActions(id) {
        const texts = getTexts();
        const editText = texts.edit || 'Düzenle';
        const deleteText = texts.delete || 'Sil';

        return '' +
            '<div class="d-flex align-items-center justify-content-end gap-2">' +
                '<button type="button" class="w-40-px h-40-px bg-primary-50 text-primary-600 rounded-circle d-inline-flex align-items-center justify-content-center js-lookup-update-button js-transaction-status-transition-update-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(editText) + '" aria-label="' + escapeHtml(editText) + '">' +
                    '<i class="ri-pencil-line"></i>' +
                '</button>' +
                '<button type="button" class="w-40-px h-40-px bg-danger-50 text-danger-600 rounded-circle d-inline-flex align-items-center justify-content-center js-lookup-delete-button js-transaction-status-transition-delete-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(deleteText) + '" aria-label="' + escapeHtml(deleteText) + '">' +
                    '<i class="ri-delete-bin-line"></i>' +
                '</button>' +
            '</div>';
    }

    function getAjaxHeaders($container) {
        const headers = {
            'X-Culture': getCurrentCulture()
        };

        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.getAntiForgeryToken === 'function') {
            const token = window.Symplify.Ajax.getAntiForgeryToken($container || $(document));

            if (token) {
                headers.RequestVerificationToken = token;
            }
        } else {
            const token = $('input[name="__RequestVerificationToken"]').first().val();

            if (token) {
                headers.RequestVerificationToken = token;
            }
        }

        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }

    function getDataTableLanguage() {
        return window.Symplify.DataTables?.language ||
            window.Symplify.Lookup?.dataTableLanguage ||
            {};
    }

    function getTexts() {
        return window.Symplify.TransactionStatusTransitions.texts ||
            window.Symplify.Lookup?.texts ||
            window.Symplify.Texts ||
            window.Symplify.texts ||
            {};
    }

    function showError(xhr) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
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
