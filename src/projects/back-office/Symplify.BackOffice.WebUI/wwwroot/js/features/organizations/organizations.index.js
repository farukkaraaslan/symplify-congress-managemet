window.Symplify = window.Symplify || {};
window.Symplify.Organizations = window.Symplify.Organizations || {};

window.Symplify.Organizations.Index = (function ($) {
    'use strict';

    const selectors = {
        table: '#organizationsTable',
        deleteForm: '#deleteOrganizationForm',
        deleteButton: '.js-organization-delete'
    };

    let table;

    function init() {
        const $table = $(selectors.table);

        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        initializeTable($table);
        bindEvents();
    }

    function initializeTable($table) {
        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();
            return;
        }

        table = $table.DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            autoWidth: false,
            searching: true,
            ordering: true,
            paging: true,
            pageLength: 10,
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: window.Symplify.Ajax.buildAjaxHeaders($(document)),
                data: function (data) {
                    data.culture = getCurrentCulture();
                    return data;
                },
                error: function (xhr) {
                    window.Symplify.Ajax.showError(xhr);
                }
            },
            columns: [
                { data: 'rowNumber', name: 'rowNumber', orderable: false, searchable: false, className: 'text-nowrap' },
                { data: null, name: 'name', render: renderOrganization },
                { data: 'code', name: 'code', render: renderCode },
                { data: 'brandColor', name: 'brandColor', render: renderBrandColor },
                { data: 'isActive', name: 'isActive', render: renderStatus },
                { data: 'activeApiKeyCount', orderable: false, searchable: false, render: renderApiKeyCount },
                { data: 'lastUpdatedAt', name: 'updatedDate', defaultContent: '-' },
                { data: null, orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ],
            language: getDataTableLanguage()
        });
    }

    function bindEvents() {
        $(document)
            .off('click.organizationsDelete', selectors.deleteButton)
            .on('click.organizationsDelete', selectors.deleteButton, deleteOrganization);
    }

    function deleteOrganization() {
        const $button = $(this);
        const id = $button.data('id');
        const name = $button.data('name') || '';

        window.Symplify.Ajax.confirm({
            title: text('BackOffice.Organizations.DeleteConfirmTitle', 'Organizasyon silinsin mi?'),
            text: name
                ? text('BackOffice.Organizations.DeleteConfirmTextWithName', 'Bu organizasyon silinecek:') + ' ' + name
                : text('BackOffice.Organizations.DeleteConfirmText', 'Bu organizasyon silinecek.'),
            confirmButtonText: text('Common.Delete', 'Sil')
        }).then(function (result) {
            if (!result || result.isConfirmed !== true) {
                return;
            }

            const $form = $(selectors.deleteForm);
            const deleteUrl = $(selectors.table).data('delete-url') || $form.attr('action');

            $.ajax({
                url: deleteUrl,
                type: $form.attr('method') || 'POST',
                data: { id: id },
                headers: window.Symplify.Ajax.buildAjaxHeaders($form.length ? $form : $(document))
            })
                .done(function (response) {
                    if (!response || response.success !== true) {
                        window.Symplify.Ajax.showError(response);
                        return;
                    }

                    reload(false);
                    window.Symplify.Ajax.showSuccess(response.message || text('Common.Deleted', 'Kayıt silindi.'));
                })
                .fail(function (xhr) {
                    window.Symplify.Ajax.showError(xhr);
                });
        });
    }

    function renderOrganization(row) {
        const logoPath = row.logoLightPath || row.logoPath || row.logoDarkPath || '';
        const avatar = logoPath
            ? '<img src="' + escapeHtml(logoPath) + '" alt="" class="w-40-px h-40-px rounded-circle border object-fit-cover flex-shrink-0" />'
            : '<span class="w-40-px h-40-px rounded-circle bg-primary-50 text-primary-600 d-flex align-items-center justify-content-center fw-semibold flex-shrink-0">' + escapeHtml((row.code || 'OR').substring(0, 2).toUpperCase()) + '</span>';

        return '' +
            '<div class="d-flex align-items-center gap-3">' +
            avatar +
            '<div>' +
            '<span class="fw-semibold text-primary-light d-block">' + escapeHtml(row.name) + '</span>' +
            '<small class="text-neutral-500">' + escapeHtml(row.shortName || '') + '</small>' +
            '</div>' +
            '</div>';
    }

    function renderCode(value) {
        return '<span class="fw-medium">' + escapeHtml(value || '-') + '</span>';
    }

    function renderBrandColor(value) {
        const color = normalizeBrandColor(value);

        if (!color) {
            return '<span class="text-neutral-400">-</span>';
        }

        return '' +
            '<span class="d-inline-flex align-items-center gap-2">' +
            '<span class="d-inline-block rounded-circle border" style="width:18px;height:18px;background:' + escapeHtml(color) + ';"></span>' +
            '<span class="fw-medium">' + escapeHtml(color) + '</span>' +
            '</span>';
    }

    function renderStatus(isActive) {
        const label = isActive ? text('Common.Active', 'Aktif') : text('Common.Passive', 'Pasif');
        const cssClass = isActive ? 'bg-success-100 text-success-600' : 'bg-danger-100 text-danger-600';

        return '<span class="badge ' + cssClass + ' rounded-pill px-12 py-8">' + escapeHtml(label) + '</span>';
    }

    function renderApiKeyCount(value) {
        const suffix = text('BackOffice.Organizations.Table.ActiveApiKeysSuffix', 'aktif anahtar');
        return (value || 0) + ' ' + escapeHtml(suffix);
    }

    function renderActions(row) {
        const $table = $(selectors.table);
        const editBaseUrl = $table.data('edit-base-url') || '';
        const apiKeysBaseUrl = $table.data('api-keys-base-url') || '';
        const manageApiKeysText = text('BackOffice.Organizations.Buttons.ApiKeys', 'API Keyleri Yönet');
        const editText = text('Common.Edit', 'Düzenle');
        const deleteText = text('Common.Delete', 'Sil');

        return '' +
            '<div class="d-flex align-items-center justify-content-end gap-2">' +
            '<a href="' + escapeHtml(apiKeysBaseUrl) + '?organizationId=' + encodeURIComponent(row.id) + '" class="btn btn-primary-600 radius-8 px-12 py-8 d-inline-flex align-items-center gap-2" title="' + escapeHtml(manageApiKeysText) + '">' +
            '<i class="ri-key-2-line"></i>' + escapeHtml(manageApiKeysText) +
            '</a>' +
            '<a href="' + escapeHtml(editBaseUrl) + '?id=' + encodeURIComponent(row.id) + '" class="btn btn-warning-100 text-warning-600 radius-8 px-12 py-8 w-40-px h-40-px d-inline-flex align-items-center justify-content-center" title="' + escapeHtml(editText) + '">' +
            '<i class="ri-edit-line"></i>' +
            '</a>' +
            '<button type="button" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 w-40-px h-40-px d-inline-flex align-items-center justify-content-center js-organization-delete" data-id="' + escapeHtml(row.id) + '" data-name="' + escapeHtml(row.name) + '" title="' + escapeHtml(deleteText) + '">' +
            '<i class="ri-delete-bin-line"></i>' +
            '</button>' +
            '</div>';
    }

    function reload(resetPaging) {
        if (table) {
            table.ajax.reload(null, resetPaging === true);
        }
    }

    function normalizeBrandColor(value) {
        const color = String(value || '').trim();
        return /^#[0-9a-fA-F]{6}$/.test(color) ? color.toUpperCase() : '';
    }

    function getCurrentCulture() {
        return location.pathname.split('/').filter(Boolean)[0] || document.documentElement.lang || 'tr-TR';
    }

    function getDataTableLanguage() {
        return (window.Symplify && window.Symplify.dataTables && window.Symplify.dataTables.language)
            || (window.Symplify && window.Symplify.DataTables && window.Symplify.DataTables.language)
            || {
                search: 'Ara:',
                lengthMenu: '_MENU_ kayıt göster',
                info: '_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor',
                infoEmpty: 'Kayıt bulunamadı',
                zeroRecords: 'Eşleşen kayıt bulunamadı',
                paginate: { first: 'İlk', last: 'Son', next: 'Sonraki', previous: 'Önceki' }
            };
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
        init: init,
        reload: reload
    };
})(jQuery);

$(function () {
    window.Symplify.Organizations.Index.init();
});
