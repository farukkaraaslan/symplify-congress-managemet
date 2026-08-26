window.Symplify = window.Symplify || {};
window.Symplify.OrganizationApiKeys = window.Symplify.OrganizationApiKeys || {};

window.Symplify.OrganizationApiKeys.Table = (function ($) {
    'use strict';

    let table;

    const selectors = {
        table: '#organizationApiKeysTable',
        copyButton: '.js-organization-api-key-copy-button'
    };

    function init() {
        const $table = $(selectors.table);

        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();
            return;
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
            order: [],
            language: getDataTableLanguage(),
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders($table),
                data: function (data) {
                    data.culture = getCurrentCulture();
                    data.organizationId = $table.data('organization-id');
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
                    name: 'name',
                    orderable: true,
                    searchable: true,
                    render: renderName
                },
                {
                    data: 'environment',
                    name: 'environment',
                    orderable: true,
                    searchable: true,
                    className: 'text-nowrap',
                    render: renderEnvironment
                },
                {
                    data: 'keyType',
                    name: 'keyType',
                    orderable: true,
                    searchable: true,
                    className: 'text-nowrap',
                    render: renderKeyType
                },
                {
                    data: 'keyPrefix',
                    name: 'keyPrefix',
                    orderable: false,
                    searchable: true,
                    className: 'text-nowrap',
                    render: renderKeyPrefix
                },
                {
                    data: 'scopes',
                    name: 'scopes',
                    orderable: false,
                    searchable: true,
                    render: renderScopes
                },
                {
                    data: 'isActive',
                    name: 'isActive',
                    orderable: true,
                    searchable: false,
                    className: 'text-nowrap',
                    render: renderStatus
                },
                {
                    data: 'expiresAt',
                    name: 'expiresAt',
                    orderable: false,
                    searchable: false,
                    className: 'text-nowrap',
                    render: renderNullableDate
                },
                {
                    data: 'lastUsedAt',
                    name: 'lastUsedAt',
                    orderable: true,
                    searchable: false,
                    className: 'text-nowrap',
                    render: renderLastUsedAt
                },
                {
                    data: null,
                    name: 'actions',
                    orderable: false,
                    searchable: false,
                    className: 'text-end text-nowrap',
                    render: renderActions
                }
            ]
        });

        bindCopyButtons();
    }

    function reload(resetPaging) {
        if (table) {
            table.ajax.reload(null, resetPaging === true);
        }
    }

    function renderName(row) {
        const name = row && row.name ? row.name : '';
        const description = row && row.description ? row.description : '';

        if (!description) {
            return '<span class="fw-medium text-secondary-light">' + escapeHtml(name) + '</span>';
        }

        return '' +
            '<div class="d-flex flex-column gap-1">' +
            '<span class="fw-medium text-secondary-light">' + escapeHtml(name) + '</span>' +
            '<small class="text-neutral-500">' + escapeHtml(description) + '</small>' +
            '</div>';
    }

    function renderEnvironment(value) {
        const normalized = String(value || '').trim();

        if (!normalized) {
            return renderDash();
        }

        const cssClass = normalized.toLowerCase() === 'production'
            ? 'bg-danger-50 text-danger-600'
            : normalized.toLowerCase() === 'sandbox'
                ? 'bg-warning-100 text-warning-700'
                : 'bg-info-100 text-info-600';

        return '<span class="badge rounded-pill px-12 py-6 ' + cssClass + '">' + escapeHtml(normalized) + '</span>';
    }

    function renderKeyType(value) {
        const normalized = String(value || '').trim();

        if (!normalized) {
            return renderDash();
        }

        return '<span class="badge bg-primary-50 text-primary-600 rounded-pill px-12 py-6">' + escapeHtml(normalized) + '</span>';
    }

    function renderKeyPrefix(value) {
        const prefix = String(value || '').trim();
        const texts = getTexts();

        if (!prefix) {
            return renderDash();
        }

        return '' +
            '<div class="d-flex align-items-center gap-2">' +
            '<code class="text-sm">' + escapeHtml(prefix) + '</code>' +
            '<button type="button" class="w-32-px h-32-px bg-primary-50 text-primary-600 rounded-circle d-inline-flex align-items-center justify-content-center js-organization-api-key-copy-button" data-copy-value="' + escapeHtml(prefix) + '" title="' + escapeHtml(texts.copy || 'Kopyala') + '" aria-label="' + escapeHtml(texts.copy || 'Kopyala') + '">' +
            '<i class="ri-file-copy-line"></i>' +
            '</button>' +
            '</div>';
    }

    function renderScopes(value) {
        const scopes = normalizeScopes(value);
        const texts = getTexts();

        if (!scopes.length) {
            return '<span class="text-neutral-400">' + escapeHtml(texts.noScope || 'Kapsam yok') + '</span>';
        }

        const visibleScopes = scopes.slice(0, 3);
        const hiddenCount = scopes.length - visibleScopes.length;

        let html = '<div class="api-key-scope-list">';

        html += visibleScopes.map(function (scope) {
            return '<span class="api-key-scope-chip">' + escapeHtml(scope) + '</span>';
        }).join('');

        if (hiddenCount > 0) {
            const moreText = String(texts.showMoreScopes || '+{0} kapsam').replace('{0}', hiddenCount);
            html += '<span class="api-key-scope-chip api-key-scope-chip--more">' + escapeHtml(moreText) + '</span>';
        }

        html += '</div>';

        return html;
    }

    function renderStatus(isActive) {
        const texts = getTexts();
        const label = isActive ? (texts.active || 'Aktif') : (texts.passive || 'Pasif');
        const cssClass = isActive ? 'bg-success-100 text-success-600' : 'bg-danger-100 text-danger-600';

        return '<span class="badge rounded-pill px-12 py-6 ' + cssClass + '">' + escapeHtml(label) + '</span>';
    }

    function renderNullableDate(value) {
        const texts = getTexts();
        const text = String(value || '').trim();

        if (!text) {
            return '<span class="text-neutral-400">' + escapeHtml(texts.notExpire || 'Süresiz') + '</span>';
        }

        return escapeHtml(text);
    }

    function renderLastUsedAt(value) {
        const texts = getTexts();
        const text = String(value || '').trim();

        if (!text) {
            return '<span class="text-neutral-400">' + escapeHtml(texts.neverUsed || 'Henüz kullanılmadı') + '</span>';
        }

        return escapeHtml(text);
    }

    function renderActions(row) {
        const prefix = row && row.keyPrefix ? row.keyPrefix : '';
        const texts = getTexts();

        if (!prefix) {
            return renderDash();
        }

        return '' +
            '<div class="d-flex align-items-center justify-content-end gap-2">' +
            '<button type="button" class="w-40-px h-40-px bg-primary-50 text-primary-600 rounded-circle d-inline-flex align-items-center justify-content-center js-organization-api-key-copy-button" data-copy-value="' + escapeHtml(prefix) + '" title="' + escapeHtml(texts.copy || 'Kopyala') + '" aria-label="' + escapeHtml(texts.copy || 'Kopyala') + '">' +
            '<i class="ri-file-copy-line"></i>' +
            '</button>' +
            '</div>';
    }

    function bindCopyButtons() {
        $(document)
            .off('click.organizationApiKeysCopy', selectors.copyButton)
            .on('click.organizationApiKeysCopy', selectors.copyButton, function () {
                const value = $(this).data('copy-value');
                const texts = getTexts();

                copyToClipboard(value)
                    .then(function () {
                        showSuccess(texts.keyPrefixCopied || texts.copied || 'Kopyalandı.');
                    })
                    .catch(function () {
                        showError(texts.copyFailed || 'Kopyalama işlemi başarısız oldu.');
                    });
            });
    }

    function normalizeScopes(value) {
        if (!value) {
            return [];
        }

        if (Array.isArray(value)) {
            return value.map(normalizeText).filter(Boolean);
        }

        return String(value)
            .split(/[;,\n]+/)
            .map(normalizeText)
            .filter(Boolean);
    }

    function normalizeText(value) {
        const text = String(value || '').trim();

        return text.length ? text : null;
    }

    function copyToClipboard(value) {
        const text = String(value || '').trim();

        if (!text) {
            return Promise.reject();
        }

        if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
            return navigator.clipboard.writeText(text);
        }

        const input = document.createElement('textarea');
        input.value = text;
        input.setAttribute('readonly', 'readonly');
        input.style.position = 'absolute';
        input.style.left = '-9999px';

        document.body.appendChild(input);
        input.select();

        const copied = document.execCommand('copy');

        document.body.removeChild(input);

        return copied ? Promise.resolve() : Promise.reject();
    }

    function getAjaxHeaders($container) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.buildAjaxHeaders === 'function') {
            return window.Symplify.Ajax.buildAjaxHeaders($container || $(document));
        }

        const headers = {
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json',
            'X-Culture': getCurrentCulture()
        };

        const token = $('input[name="__RequestVerificationToken"]').first().val();

        if (token) {
            headers.RequestVerificationToken = token;
        }

        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);

        return segments.length > 0 ? segments[0] : 'tr-TR';
    }

    function getDataTableLanguage() {
        return window.Symplify.DataTables?.language ||
            window.Symplify.Lookup?.dataTableLanguage ||
            {};
    }

    function getTexts() {
        return window.Symplify.OrganizationApiKeys?.texts ||
            window.Symplify.Lookup?.texts ||
            window.Symplify.Texts ||
            window.Symplify.texts ||
            {};
    }

    function renderDash() {
        return '<span class="text-neutral-400">-</span>';
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

        console.error(response);
    }

    function escapeHtml(value) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.escapeHtml === 'function') {
            return window.Symplify.Ajax.escapeHtml(value);
        }

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