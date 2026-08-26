window.Symplify = window.Symplify || {};
window.Symplify.Congresses = window.Symplify.Congresses || {};

window.Symplify.Congresses.Index = (function ($) {
    'use strict';

    let table;

    const selectors = {
        table: '#congressTable',
        organizationFilter: '#congressOrganizationFilter',
        statusFilter: '#congressStatusFilter',
        applyFiltersButton: '#applyCongressFilters',
        clearFiltersButton: '#clearCongressFilters',
        createButton: '#createCongressButton'
    };

    function init() {
        const $table = $(selectors.table);
        const $organizationFilter = $(selectors.organizationFilter);
        const $statusFilter = $(selectors.statusFilter);
        const $applyFiltersButton = $(selectors.applyFiltersButton);
        const $clearFiltersButton = $(selectors.clearFiltersButton);
        const $createButton = $(selectors.createButton);

        bindOrganizationFilter($organizationFilter, $createButton, $table);
        bindApplyFilters($applyFiltersButton);
        bindClearFilters($organizationFilter, $statusFilter, $clearFiltersButton, $createButton, $table);
        bindDeleteAction($table);

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
            order: [[3, 'desc']],
            columnDefs: [
                { className: 'text-nowrap', targets: [0, 1, 3, 4, 5, 6, 7] },
                { width: '145px', targets: 0 },
                { width: '120px', targets: 1 },
                { width: '32%', targets: 2 },
                { width: '190px', targets: 3 },
                { width: '165px', targets: 4 },
                { width: '95px', targets: 5 },
                { width: '115px', targets: 6 },
                { width: '115px', targets: 7 }
            ],
            language: getDataTableLanguage(),
            initComplete: function () {
                const $searchInput = $('#congressTable_filter input[type="search"]');
                $searchInput
                    .attr('placeholder', 'Kod, başlık, organizasyon veya lokasyon ara')
                    .addClass('form-control-sm');
            },
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders($(document)),
                data: function (data) {
                    data.culture = getCurrentCulture();
                    data.organizationId = getOrganizationId();
                    data.status = getStatusValue();
                    return data;
                },
                error: showError
            },
            columns: [
                { data: 'code', name: 'code', render: renderCode },
                { data: 'organizationName', name: 'organization', orderable: false, render: renderText },
                { data: null, name: 'title', render: renderTitle },
                { data: 'dateRange', name: 'startDate', render: renderText },
                { data: 'location', name: 'location', orderable: false, render: renderText },
                { data: null, name: 'language', orderable: false, searchable: false, render: renderLanguage },
                { data: null, name: 'status', render: renderStatus },
                { data: null, name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ]
        });
    }

    function bindOrganizationFilter($organizationFilter, $createButton, $table) {
        const createBaseUrl = $table.data('create-base-url');

        updateCreateButtonUrl(createBaseUrl, $organizationFilter, $createButton);

        $organizationFilter
            .off('change.congressesIndex')
            .on('change.congressesIndex', function () {
                updateCreateButtonUrl(createBaseUrl, $organizationFilter, $createButton);
            });
    }

    function bindApplyFilters($applyFiltersButton) {
        $applyFiltersButton
            .off('click.congressesIndex')
            .on('click.congressesIndex', function () {
                if (table) {
                    table.ajax.reload();
                }
            });
    }

    function bindClearFilters($organizationFilter, $statusFilter, $clearFiltersButton, $createButton, $table) {
        const createBaseUrl = $table.data('create-base-url');

        $clearFiltersButton
            .off('click.congressesIndex')
            .on('click.congressesIndex', function () {
                $organizationFilter.val('');
                $statusFilter.val(getDefaultStatusValue($statusFilter));
                updateCreateButtonUrl(createBaseUrl, $organizationFilter, $createButton);

                clearDataTableSearchInput();

                if (table) {
                    table.search('');
                    table.ajax.reload();
                }
            });
    }

    function bindDeleteAction($table) {
        $table
            .off('click.congressesDelete', '.js-congress-delete-button')
            .on('click.congressesDelete', '.js-congress-delete-button', function () {
                const $button = $(this);
                inspectAndDeleteCongress($button);
            });
    }

    async function inspectAndDeleteCongress($button) {
        const inspectUrl = ($button.data('inspect-url') || '').toString();
        const deleteUrl = ($button.data('delete-url') || '').toString();
        const title = ($button.data('title') || '').toString();

        if (!inspectUrl || !deleteUrl) {
            showToast('error', text('Common.InvalidRequest', 'Geçersiz istek.'));
            return;
        }

        setButtonLoading($button, true);

        try {
            const inspection = await ajaxJson({ url: inspectUrl, method: 'GET' });

            if (!inspection || inspection.success === false) {
                showToast('error', inspection && inspection.message ? inspection.message : text('Common.Error', 'İşlem sırasında hata oluştu.'));
                return;
            }

            if (!inspection.isSafe) {
                showUnsafeDeleteMessage(inspection);
                return;
            }

            const confirmed = await confirmDocumentOnlyDelete(inspection, title);

            if (!confirmed) {
                return;
            }

            const result = await ajaxJson({
                url: deleteUrl,
                method: 'POST',
                headers: getAjaxHeaders($(document))
            });

            if (!result || result.success === false) {
                showToast('error', result && result.message ? result.message : text('Common.Error', 'İşlem sırasında hata oluştu.'));
                return;
            }

            showToast('success', result.message || text('BackOffice.Congresses.Messages.Deleted', 'Kongre silindi.'));

            if (table) {
                table.ajax.reload(null, false);
            }
        } catch (error) {
            showToast('error', resolveAjaxErrorMessage(error));
        } finally {
            setButtonLoading($button, false);
        }
    }

    function showUnsafeDeleteMessage(inspection) {
        const items = Array.isArray(inspection.blockingDependencies)
            ? inspection.blockingDependencies
            : [];

        const html = items.length
            ? '<div class="text-start">' +
                '<p class="mb-2">' + escapeHtml(text('BackOffice.Congresses.Delete.BlockedIntro', 'Bu kongre otomatik silinemez. Önce bağlı kayıtları kontrol edin:')) + '</p>' +
                '<ul class="mb-0">' + items.map(function (item) {
                    return '<li>' + escapeHtml(item.name || '-') + ': <strong>' + escapeHtml(String(item.count || 0)) + '</strong></li>';
                }).join('') + '</ul>' +
              '</div>'
            : escapeHtml(text('BackOffice.Congresses.Delete.Blocked', 'Bu kongre otomatik silinemez.'));

        showModalMessage('error', text('BackOffice.Congresses.Delete.NotSafeTitle', 'Silme engellendi'), html);
    }

    async function confirmDocumentOnlyDelete(inspection, fallbackTitle) {
        const title = inspection.title || fallbackTitle || inspection.code || '';
        const html = '' +
            '<div class="text-start">' +
            '<p class="mb-2"><strong>' + escapeHtml(title) + '</strong></p>' +
            '<p class="mb-2">' + escapeHtml(text('BackOffice.Congresses.Delete.Warning', 'Bu işlem kongreyi, doküman kayıtlarını ve ilgili MinIO objelerini kalıcı olarak silecektir.')) + '</p>' +
            '<ul class="mb-0">' +
            '<li>' + escapeHtml(text('BackOffice.Congresses.Delete.DocumentCount', 'Doküman')) + ': <strong>' + escapeHtml(String(inspection.documentCount || 0)) + '</strong></li>' +
            '<li>' + escapeHtml(text('BackOffice.Congresses.Delete.DocumentTranslationCount', 'Doküman açıklama çevirisi')) + ': <strong>' + escapeHtml(String(inspection.documentTranslationCount || 0)) + '</strong></li>' +
            '<li>' + escapeHtml(text('BackOffice.Congresses.Delete.WorkflowRecordCount', 'Sistem workflow kaydı')) + ': <strong>' + escapeHtml(String((inspection.workflowSettingCount || 0) + (inspection.workflowTransitionCount || 0))) + '</strong></li>' +
            '</ul>' +
            '</div>';

        if (window.Swal && typeof window.Swal.fire === 'function') {
            const response = await window.Swal.fire({
                icon: 'warning',
                title: text('BackOffice.Congresses.Delete.ConfirmTitle', 'Kongre silinsin mi?'),
                html: html,
                showCancelButton: true,
                confirmButtonText: text('Common.Delete', 'Sil'),
                cancelButtonText: text('Common.Cancel', 'Vazgeç'),
                confirmButtonColor: '#dc3545'
            });

            return response.isConfirmed === true;
        }

        return window.confirm(text('BackOffice.Congresses.Delete.ConfirmText', 'Bu kongre ve MinIO dosyaları silinecek. Devam edilsin mi?'));
    }

    function ajaxJson(options) {
        return $.ajax({
            url: options.url,
            method: options.method || 'GET',
            headers: options.headers || {},
            dataType: 'json'
        });
    }

    function showModalMessage(icon, title, html) {
        if (window.Swal && typeof window.Swal.fire === 'function') {
            window.Swal.fire({ icon: icon, title: title, html: html });
            return;
        }

        window.alert($(html).text() || title);
    }

    function showToast(icon, message) {
        if (window.Swal && typeof window.Swal.fire === 'function') {
            window.Swal.fire({ icon: icon, text: message, timer: 3000, showConfirmButton: false });
            return;
        }

        window.alert(message);
    }

    function resolveAjaxErrorMessage(error) {
        if (error && error.responseJSON && error.responseJSON.message) {
            return error.responseJSON.message;
        }

        if (error && error.responseText) {
            return error.responseText;
        }

        return text('Common.Error', 'İşlem sırasında hata oluştu.');
    }

    function setButtonLoading($button, isLoading) {
        if (!$button || !$button.length) {
            return;
        }

        $button.prop('disabled', isLoading);
        $button.toggleClass('disabled', isLoading);
    }

    function updateCreateButtonUrl(createBaseUrl, $organizationFilter, $createButton) {
        if (!$createButton.length || !createBaseUrl) {
            return;
        }

        const organizationId = ($organizationFilter.val() || '').toString();

        if (!organizationId) {
            $createButton.attr('href', createBaseUrl);
            return;
        }

        const separator = createBaseUrl.indexOf('?') >= 0 ? '&' : '?';
        $createButton.attr('href', createBaseUrl + separator + 'organizationId=' + encodeURIComponent(organizationId));
    }

    function renderCode(value) {
        return '<span class="fw-semibold">' + escapeHtml(value || '-') + '</span>';
    }

    function renderTitle(row) {
        const title = row.title || '-';

        return '<span class="fw-semibold text-primary-light">' +
            escapeHtml(title) +
            '</span>';
    }

    function renderText(value) {
        return '<span class="text-neutral-700">' + escapeHtml(value || '-') + '</span>';
    }

    function renderLanguage(row) {
        const cultures = Array.isArray(row.translationCultures)
            ? row.translationCultures
            : [];

        const labels = cultures
            .map(function (culture) {
                const value = (culture || '').toString().trim();

                if (!value) {
                    return '';
                }

                return value.split('-')[0].toUpperCase();
            })
            .filter(function (value, index, items) {
                return value && items.indexOf(value) === index;
            });

        if (labels.length) {
            return '<span class="badge bg-success-focus text-success-main rounded-pill">' +
                escapeHtml(labels.join(' / ')) +
                '</span>';
        }

        const label = row.isFallback
            ? text('BackOffice.Congresses.Table.FallbackLanguage', 'Fallback')
            : text('BackOffice.Congresses.Table.CurrentLanguage', 'Geçerli');

        const css = row.isFallback
            ? 'bg-warning-focus text-warning-main'
            : 'bg-success-focus text-success-main';

        return '<span class="badge ' + css + ' rounded-pill">' +
            escapeHtml(label) +
            '</span>';
    }

    function renderStatus(row) {
        const normalized = normalizeStatus(row);
        const css = row.statusBadgeClass || normalized.css;
        const label = row.statusText || text('BackOffice.Congresses.Status.' + normalized.name, normalized.label);

        return '<span class="badge ' + escapeHtml(css) + ' rounded-pill">' + escapeHtml(label) + '</span>';
    }

    function normalizeStatus(row) {
        const numeric = Number(row.statusValue || row.status);

        if (!Number.isNaN(numeric) && numeric > 0) {
            return statusFromValue(numeric);
        }

        const raw = (row.statusName || row.status || '').toString();
        const status = raw.toLowerCase();

        if (status === 'published') {
            return { name: 'Published', label: 'Yayında', css: 'bg-success-focus text-success-main' };
        }

        if (status === 'archived') {
            return { name: 'Archived', label: 'Arşivde', css: 'bg-neutral-200 text-neutral-700' };
        }

        if (status === 'cancelled' || status === 'canceled') {
            return { name: 'Cancelled', label: 'İptal', css: 'bg-danger-focus text-danger-main' };
        }

        return { name: 'Draft', label: 'Taslak', css: 'bg-warning-focus text-warning-main' };
    }

    function statusFromValue(value) {
        switch (value) {
            case 2:
                return { name: 'Published', label: 'Yayında', css: 'bg-success-focus text-success-main' };
            case 3:
                return { name: 'Archived', label: 'Arşivde', css: 'bg-neutral-200 text-neutral-700' };
            case 4:
                return { name: 'Cancelled', label: 'İptal', css: 'bg-danger-focus text-danger-main' };
            case 1:
            default:
                return { name: 'Draft', label: 'Taslak', css: 'bg-warning-focus text-warning-main' };
        }
    }

    function renderActions(row) {
        const actionsText = text('Common.Actions', 'İşlemler');
        const editText = text('Common.Edit', 'Düzenle');
        const manageText = text('BackOffice.Congresses.Buttons.Manage', 'Yönet');
        const deleteText = text('Common.Delete', 'Sil');

        const editUrl = row.editUrl || buildEditUrl(row.id);
        const manageUrl = buildManageUrl(row.id, row.manageUrl);
        const inspectUrl = row.deleteInspectionUrl || buildDeleteInspectionUrl(row.id);
        const deleteUrl = row.deleteUrl || buildDeleteUrl(row.id);

        return '' +
            '<div class="dropdown d-inline-block">' +
                '<button type="button" ' +
                        'class="btn btn-sm btn-outline-primary-600 radius-8 dropdown-toggle" ' +
                        'data-bs-toggle="dropdown" ' +
                        'data-bs-boundary="viewport" ' +
                        'aria-expanded="false">' +
                    escapeHtml(actionsText) +
                '</button>' +
                '<ul class="dropdown-menu dropdown-menu-end">' +
                    '<li>' +
                        '<a class="dropdown-item d-flex align-items-center gap-2" ' +
                           'href="' + escapeHtml(editUrl) + '">' +
                            '<i class="ri-edit-line"></i>' +
                            '<span>' + escapeHtml(editText) + '</span>' +
                        '</a>' +
                    '</li>' +
                    '<li>' +
                        '<a class="dropdown-item d-flex align-items-center gap-2 js-congress-manage-link" ' +
                           'href="' + escapeHtml(manageUrl) + '">' +
                            '<i class="ri-settings-3-line"></i>' +
                            '<span>' + escapeHtml(manageText) + '</span>' +
                        '</a>' +
                    '</li>' +
                    '<li><hr class="dropdown-divider"></li>' +
                    '<li>' +
                        '<button type="button" ' +
                                'class="dropdown-item text-danger d-flex align-items-center gap-2 js-congress-delete-button" ' +
                                'data-inspect-url="' + escapeHtml(inspectUrl) + '" ' +
                                'data-delete-url="' + escapeHtml(deleteUrl) + '" ' +
                                'data-title="' + escapeHtml(row.title || row.code || '') + '">' +
                            '<i class="ri-delete-bin-6-line"></i>' +
                            '<span>' + escapeHtml(deleteText) + '</span>' +
                        '</button>' +
                    '</li>' +
                '</ul>' +
            '</div>';
    }

    function buildManageUrl(id, serverManageUrl) {
        const serverUrl = (serverManageUrl || '').toString();

        // Eski response veya browser cache /Congresses/Edit/{id}?tab=slider döndürebilir.
        // Yönet aksiyonu hiçbir durumda Edit sayfasına düşmemeli.
        if (serverUrl && serverUrl.toLowerCase().indexOf('/edit') < 0) {
            return serverUrl;
        }

        const $table = $(selectors.table);
        const baseUrl = ($table.data('manage-base-url') || ('/' + getCurrentCulture() + '/Congresses/Manage')).toString();

        if (!id) {
            return baseUrl;
        }

        const separator = baseUrl.endsWith('/') ? '' : '/';
        const url = baseUrl + separator + encodeURIComponent(id);
        return url + (url.indexOf('?') >= 0 ? '&' : '?') + 'tab=slider';
    }

    function buildEditUrl(id) {
        const $table = $(selectors.table);
        const baseUrl = ($table.data('edit-base-url') || ('/' + getCurrentCulture() + '/congresses/edit')).toString();

        if (!id) {
            return baseUrl;
        }

        const separator = baseUrl.endsWith('/') ? '' : '/';
        return baseUrl + separator + encodeURIComponent(id);
    }

    function buildDeleteInspectionUrl(id) {
        return buildActionUrl('DeleteInspection', id);
    }

    function buildDeleteUrl(id) {
        return buildActionUrl('DeleteDocumentOnly', id);
    }

    function buildActionUrl(action, id) {
        const culture = getCurrentCulture();

        if (!id) {
            return '/' + culture + '/Congresses/' + action;
        }

        return '/' + culture + '/Congresses/' + action + '/' + encodeURIComponent(id);
    }

    function getOrganizationId() {
        return ($(selectors.organizationFilter).val() || '').toString();
    }

    function getStatusValue() {
        const $statusFilter = $(selectors.statusFilter);
        return ($statusFilter.val() || getDefaultStatusValue($statusFilter)).toString();
    }

    function getDefaultStatusValue($statusFilter) {
        return (($statusFilter && $statusFilter.data('default-status')) || '2').toString();
    }

    function clearDataTableSearchInput() {
        const filterInputSelector = selectors.table + '_filter input[type="search"]';
        $(filterInputSelector).val('');
    }

    function getAjaxHeaders($container) {
        const headers = { 'X-Culture': getCurrentCulture() };

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
        return location.pathname.split('/').filter(Boolean)[0] || 'tr-TR';
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

    function showError(response) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(response);
        }
    }

    function escapeHtml(value) {
        return $('<div/>').text(value || '').html();
    }

    return {
        init: init,
        reload: function () {
            if (table) {
                table.ajax.reload(null, false);
            }
        }
    };
})(jQuery);

$(function () {
    window.Symplify.Congresses.Index.init();
});
