window.Symplify = window.Symplify || {};
window.Symplify.TransactionStatuses = window.Symplify.TransactionStatuses || {};

window.Symplify.TransactionStatuses.Table = (function ($) {
    'use strict';

    let table;
    const selectors = {
        table: '#transactionStatusesTable',
        phaseFilter: '#transactionStatusPhaseFilter',
        dragHandle: '.js-lookup-drag-handle'
    };

    function init() {
        const $table = $(selectors.table);
        if (!$table.length || !$.fn.DataTable) return;
        ensureReorderStyles();

        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();
            if ($table.data('symplify-feature-table-initialized') === true) { initializeReorder(); updateDragHandleState(); return; }
            table.destroy(); $table.find('tbody').empty();
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
            order: [[2, 'asc'], [3, 'asc']],
            language: getDataTableLanguage(),
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders($(document)),
                data: function (data) {
                    data.culture = getCurrentCulture();
                    const phaseId = getSelectedPhaseId();
                    if (phaseId) data.transactionStatusPhaseId = phaseId;
                    return data;
                },
                error: showError
            },
            columns: [
                { data: 'rowNumber', name: 'rowNumber', orderable: false, searchable: false, className: 'text-nowrap' },
                { data: 'id', name: 'reorder', orderable: false, searchable: false, className: 'text-center text-nowrap', render: renderDragHandle },
                { data: 'transactionStatusPhaseName', name: 'phase', orderable: true, searchable: true, render: renderPhase },
                { data: 'order', name: 'order', orderable: true, searchable: false, className: 'text-nowrap' },
                { data: 'code', name: 'code', orderable: true, searchable: true, render: renderCode },
                { data: 'name', name: 'name', orderable: true, searchable: true, render: renderText },
                { data: 'description', name: 'description', orderable: false, searchable: true, render: renderDescription },
                { data: 'isEditable', name: 'isEditable', orderable: true, searchable: false, render: renderBoolean },
                { data: 'isFinal', name: 'isFinal', orderable: true, searchable: false, render: renderBoolean },
                { data: 'isActive', name: 'isActive', orderable: true, searchable: false, render: renderStatus },
                { data: 'id', name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ],
            createdRow: function (row, data) { $(row).attr('data-id', data?.id || '').attr('data-order', data?.order || '').attr('data-phase-id', data?.transactionStatusPhaseId || ''); },
            drawCallback: function () { initializeReorder(); updateDragHandleState(); }
        });

        $table.data('symplify-feature-table-initialized', true);
        $table.off('order.dt.transactionStatuses search.dt.transactionStatuses page.dt.transactionStatuses draw.dt.transactionStatuses')
            .on('order.dt.transactionStatuses search.dt.transactionStatuses page.dt.transactionStatuses draw.dt.transactionStatuses', function () { window.setTimeout(function () { initializeReorder(); updateDragHandleState(); }, 0); });

        $(document).off('change.transactionStatusesPhaseFilter', selectors.phaseFilter).on('change.transactionStatusesPhaseFilter', selectors.phaseFilter, function () {
            if (getSelectedPhaseId()) table.order([[3, 'asc']]); else table.order([[2, 'asc'], [3, 'asc']]);
            reload(true);
            updateDragHandleState();
        });
    }

    function reload(resetPaging) { if (table) table.ajax.reload(null, resetPaging === true); }
    function getSelectedPhaseId() { const value = Number($(selectors.phaseFilter).val()); return value > 0 ? value : null; }
    function renderDragHandle() { const label = getTexts().dragToReorder || getTexts().reorder || 'Sırayı değiştir'; return '<span role="button" tabindex="0" class="d-inline-flex align-items-center justify-content-center text-neutral-500 js-lookup-drag-handle" title="' + escapeHtml(label) + '" aria-label="' + escapeHtml(label) + '"><i class="ri-draggable"></i></span>'; }
    function renderPhase(value, type, row) { const name = value || ''; const code = row?.transactionStatusPhaseCode || ''; const text = name ? name + (code ? ' (' + code + ')' : '') : code; return escapeHtml(text || '-'); }
    function renderCode(value) { return '<span class="badge bg-neutral-100 text-neutral-700">' + escapeHtml(value || '') + '</span>'; }
    function renderText(value) { return escapeHtml(value || ''); }
    function renderDescription(value) { if (!value) return '<span class="text-neutral-400">-</span>'; return '<span title="' + escapeHtml(value) + '">' + escapeHtml(value) + '</span>'; }
    function renderBoolean(value) { const label = value ? (getTexts().yes || 'Evet') : (getTexts().no || 'Hayır'); const cssClass = value ? 'bg-success-focus text-success-main' : 'bg-neutral-100 text-neutral-600'; return '<span class="px-16 py-4 rounded-pill fw-medium text-sm ' + cssClass + '">' + escapeHtml(label) + '</span>'; }
    function renderStatus(isActive) { const label = isActive ? (getTexts().active || 'Aktif') : (getTexts().passive || 'Pasif'); const cssClass = isActive ? 'bg-success-focus text-success-main' : 'bg-danger-focus text-danger-main'; return '<span class="px-16 py-4 rounded-pill fw-medium text-sm ' + cssClass + '">' + escapeHtml(label) + '</span>'; }
    function renderActions(id) { const texts = getTexts(); const editText = texts.edit || 'Düzenle'; const deleteText = texts.delete || 'Sil'; return '<div class="d-flex align-items-center justify-content-end gap-2"><button type="button" class="w-40-px h-40-px bg-primary-50 text-primary-600 rounded-circle d-inline-flex align-items-center justify-content-center js-transaction-status-update-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(editText) + '"><i class="ri-pencil-line"></i></button><button type="button" class="w-40-px h-40-px bg-danger-50 text-danger-600 rounded-circle d-inline-flex align-items-center justify-content-center js-transaction-status-delete-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(deleteText) + '"><i class="ri-delete-bin-line"></i></button></div>'; }

    function initializeReorder() { if (!table) return; if ($.fn.sortable) { initializeSortableReorder(); return; } }
    function initializeSortableReorder() {
        const $tbody = $(table.table().body());
        if ($tbody.data('ui-sortable')) $tbody.sortable('destroy');
        $tbody.sortable({
            items: 'tr[data-id]', handle: selectors.dragHandle, axis: 'y', cursor: 'move', tolerance: 'pointer', forcePlaceholderSize: true, placeholder: 'lookup-sort-placeholder',
            start: function (event, ui) { if (!isReorderAllowed()) { $(this).sortable('cancel'); showReorderNotAllowedMessage(); return; } ui.item.addClass('lookup-row-dragging'); ui.placeholder.html('<td colspan="11">&nbsp;</td>'); },
            sort: updateVisibleRowNumbers,
            update: function () { updateVisibleRowNumbers(); persistReorder(); },
            stop: function (event, ui) { ui.item.removeClass('lookup-row-dragging'); updateVisibleRowNumbers(); }
        });
        $tbody.sortable(isReorderAllowed() ? 'enable' : 'disable');
    }
    function updateDragHandleState() { if (!table) return; const allowed = isReorderAllowed(); const $tbody = $(table.table().body()); const $handles = $tbody.find(selectors.dragHandle); if ($.fn.sortable && $tbody.data('ui-sortable')) $tbody.sortable(allowed ? 'enable' : 'disable'); $handles.attr('draggable', allowed ? 'true' : 'false').toggleClass('opacity-50', !allowed).css('cursor', allowed ? 'grab' : 'not-allowed').attr('title', allowed ? (getTexts().dragToReorder || 'Sırayı değiştir') : (getTexts().reorderNotAllowedShort || 'Sıralama için önce faz filtresi seçin ve Sıra kolonunu artan kullanın.')); }
    function isReorderAllowed() { if (!getSelectedPhaseId()) return false; const order = table.order(); const firstOrder = Array.isArray(order) && order.length > 0 ? order[0] : null; const isOrderAsc = firstOrder && Number(firstOrder[0]) === 3 && String(firstOrder[1] || '').toLowerCase() === 'asc'; return isOrderAsc && String(table.search() || '').trim().length === 0; }
    function showReorderNotAllowedMessage() { showError({ responseJSON: { message: getTexts().reorderNotAllowed || 'Sıralama için önce faz filtresi seçilmeli, arama boş olmalı ve tablo Sıra kolonuna göre artan sıralanmalıdır.' } }); }
    function updateVisibleRowNumbers() { const pageInfo = table.page.info(); $(table.table().body()).find('tr[data-id]').each(function (index) { const visibleNumber = pageInfo.start + index + 1; $(this).find('td').eq(0).text(visibleNumber); $(this).find('td').eq(3).text(visibleNumber); }); }
    function persistReorder() { if (!isReorderAllowed()) { reload(false); return; } const reorderUrl = $(selectors.table).data('reorder-url'); if (!reorderUrl) { reload(false); return; } const pageInfo = table.page.info(); const phaseId = getSelectedPhaseId(); const items = []; $(table.table().body()).find('tr[data-id]').each(function (index) { const id = Number($(this).attr('data-id')); if (id > 0) items.push({ id: id, order: pageInfo.start + index + 1 }); }); if (!items.length) { reload(false); return; } $.ajax({ url: reorderUrl, type: 'POST', contentType: 'application/json; charset=utf-8', dataType: 'json', data: JSON.stringify({ transactionStatusPhaseId: phaseId, items: items }), headers: getAjaxHeaders($(document)) }).done(function (response) { if (!response || response.success !== true) { showError(response); reload(false); return; } showSuccess(response.message || getTexts().reorderSuccess || getTexts().updated || 'Kayıt güncellendi.'); reload(false); }).fail(function (xhr) { showError(xhr); reload(false); }); }
    function getAjaxHeaders($container) { const headers = { 'X-Culture': getCurrentCulture() }; const token = window.Symplify.Ajax?.getAntiForgeryToken ? window.Symplify.Ajax.getAntiForgeryToken($container || $(document)) : $('input[name="__RequestVerificationToken"]').first().val(); if (token) headers.RequestVerificationToken = token; return headers; }
    function getCurrentCulture() { const segments = window.location.pathname.split('/').filter(Boolean); return segments.length > 0 ? segments[0] : ''; }
    function getDataTableLanguage() { return window.Symplify.DataTables?.language || window.Symplify.Lookup?.dataTableLanguage || {}; }
    function getTexts() { return window.Symplify.TransactionStatuses.texts || window.Symplify.Lookup?.texts || window.Symplify.Texts || window.Symplify.texts || {}; }
    function showSuccess(message) { if (window.Symplify.Ajax?.showSuccess && message) { window.Symplify.Ajax.showSuccess(message); return; } if (message) alert(message); }
    function showError(xhr) { if (window.Symplify.Ajax?.showError) { window.Symplify.Ajax.showError(xhr); return; } alert(xhr?.responseJSON?.message || xhr?.message || getTexts().genericError || 'İşlem sırasında hata oluştu.'); }
    function ensureReorderStyles() { if (document.getElementById('symplify-lookup-reorder-styles')) return; const style = document.createElement('style'); style.id = 'symplify-lookup-reorder-styles'; style.textContent = '.lookup-row-dragging{opacity:.65;}.lookup-sort-placeholder td{height:56px;border:2px dashed #6b8cff;background:rgba(59,130,246,.06);}.js-lookup-drag-handle{cursor:grab;}.js-lookup-drag-handle:active{cursor:grabbing;}'; document.head.appendChild(style); }
    function escapeHtml(value) { if (value === null || value === undefined) return ''; return String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#039;'); }

    return { init: init, reload: reload };
})(jQuery);
