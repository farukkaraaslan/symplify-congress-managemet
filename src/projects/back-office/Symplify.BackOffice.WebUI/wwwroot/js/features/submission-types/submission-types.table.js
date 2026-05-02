window.Symplify = window.Symplify || {};
window.Symplify.SubmissionTypes = window.Symplify.SubmissionTypes || {};

window.Symplify.SubmissionTypes.Table = (function ($) {
    'use strict';

    let table;

    const selectors = {
        table: '#submissionTypesTable',
        dragHandle: '.js-lookup-drag-handle'
    };

    function init() {
        const $table = $(selectors.table);

        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        ensureReorderStyles();
        ensureExpectedHeader($table);

        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();

            if ($table.data('symplify-feature-table-initialized') === true) {
                initializeReorder();
                updateDragHandleState();
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
            order: [[2, 'asc']],
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
                    data: 'id',
                    name: 'reorder',
                    orderable: false,
                    searchable: false,
                    className: 'text-center text-nowrap',
                    render: renderDragHandle
                },
                {
                    data: 'order',
                    name: 'order',
                    orderable: true,
                    searchable: false,
                    className: 'text-nowrap'
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
            ],
            createdRow: function (row, data) {
                $(row)
                    .attr('data-id', data && data.id ? data.id : '')
                    .attr('data-order', data && data.order ? data.order : '');
            },
            drawCallback: function () {
                initializeReorder();
                updateDragHandleState();
            }
        });

        $table.data('symplify-feature-table-initialized', true);

        $table
            .off('order.dt.submissiontypesTable search.dt.submissiontypesTable page.dt.submissiontypesTable draw.dt.submissiontypesTable')
            .on('order.dt.submissiontypesTable search.dt.submissiontypesTable page.dt.submissiontypesTable draw.dt.submissiontypesTable', function () {
                window.setTimeout(function () {
                    initializeReorder();
                    updateDragHandleState();
                }, 0);
            });
    }

    function reload(resetPaging) {
        if (table) {
            table.ajax.reload(null, resetPaging === true);
        }
    }

    function renderDragHandle() {
        const texts = getTexts();
        const label = texts.dragToReorder || texts.reorder || 'Sırayı değiştir';

        return '' +
            '<span role="button" tabindex="0" class="d-inline-flex align-items-center justify-content-center text-neutral-500 js-lookup-drag-handle" title="' + escapeHtml(label) + '" aria-label="' + escapeHtml(label) + '">' +
            '<i class="ri-draggable"></i>' +
            '</span>';
    }
    function renderText(value) {
        return escapeHtml(value || '');
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
                '<button type="button" class="w-40-px h-40-px bg-primary-50 text-primary-600 rounded-circle d-inline-flex align-items-center justify-content-center js-lookup-update-button js-submission-type-update-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(editText) + '" aria-label="' + escapeHtml(editText) + '">' +
                    '<i class="ri-pencil-line"></i>' +
                '</button>' +
                '<button type="button" class="w-40-px h-40-px bg-danger-50 text-danger-600 rounded-circle d-inline-flex align-items-center justify-content-center js-lookup-delete-button js-submission-type-delete-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(deleteText) + '" aria-label="' + escapeHtml(deleteText) + '">' +
                    '<i class="ri-delete-bin-line"></i>' +
                '</button>' +
            '</div>';
    }

    function initializeReorder() {
        if (!table) {
            return;
        }

        if ($.fn.sortable) {
            initializeSortableReorder();
            return;
        }

        initializeNativeReorder();
    }

    function initializeSortableReorder() {
        const $tbody = $(table.table().body());

        $tbody.off('.lookupNativeReorder');

        if ($tbody.data('ui-sortable')) {
            $tbody.sortable('destroy');
        }

        $tbody.sortable({
            items: 'tr[data-id]',
            handle: selectors.dragHandle,
            axis: 'y',
            cursor: 'move',
            tolerance: 'pointer',
            forcePlaceholderSize: true,
            placeholder: 'lookup-sort-placeholder',
            helper: function (event, row) {
                const $originals = row.children();
                const $helper = row.clone();

                $helper.children().each(function (index) {
                    $(this).width($originals.eq(index).width());
                });

                return $helper;
            },
            start: function (event, ui) {
                if (!isReorderAllowed()) {
                    $(this).sortable('cancel');
                    showReorderNotAllowedMessage();
                    return;
                }

                ui.item.addClass('lookup-row-dragging');
                ui.placeholder.html('<td colspan="7">&nbsp;</td>');
            },
            sort: function () {
                updateVisibleRowNumbers();
            },
            update: function () {
                updateVisibleRowNumbers();
                persistReorder();
            },
            stop: function (event, ui) {
                ui.item.removeClass('lookup-row-dragging');
                updateVisibleRowNumbers();
            }
        });

        $tbody.sortable(isReorderAllowed() ? 'enable' : 'disable');
    }

    function initializeNativeReorder() {
        const $tbody = $(table.table().body());
        let draggedRow = null;
        let dragChanged = false;

        if ($tbody.data('ui-sortable')) {
            $tbody.sortable('destroy');
        }

        $tbody.off('.lookupNativeReorder');

        $tbody.on('dragstart.lookupNativeReorder', selectors.dragHandle, function (event) {
            if (!isReorderAllowed()) {
                event.preventDefault();
                showReorderNotAllowedMessage();
                return false;
            }

            const $row = $(this).closest('tr[data-id]');

            if (!$row.length) {
                event.preventDefault();
                return false;
            }

            draggedRow = $row[0];
            dragChanged = false;
            $row.addClass('lookup-row-dragging');

            if (event.originalEvent && event.originalEvent.dataTransfer) {
                event.originalEvent.dataTransfer.effectAllowed = 'move';
                event.originalEvent.dataTransfer.setData('text/plain', $row.attr('data-id') || '');
            }

            return true;
        });

        $tbody.on('dragover.lookupNativeReorder', 'tr[data-id]', function (event) {
            if (!draggedRow || draggedRow === this) {
                return;
            }

            event.preventDefault();

            const rect = this.getBoundingClientRect();
            const mouseY = event.originalEvent.clientY;
            const shouldInsertAfter = mouseY > rect.top + rect.height / 2;

            if (shouldInsertAfter) {
                this.parentNode.insertBefore(draggedRow, this.nextSibling);
            } else {
                this.parentNode.insertBefore(draggedRow, this);
            }

            dragChanged = true;
            updateVisibleRowNumbers();
        });

        $tbody.on('drop.lookupNativeReorder', 'tr[data-id]', function (event) {
            event.preventDefault();
        });

        $tbody.on('dragend.lookupNativeReorder', selectors.dragHandle, function () {
            if (draggedRow) {
                $(draggedRow).removeClass('lookup-row-dragging');
            }

            if (draggedRow && dragChanged) {
                persistReorder();
            }

            draggedRow = null;
            dragChanged = false;
        });
    }

    function updateDragHandleState() {
        if (!table) {
            return;
        }

        const allowed = isReorderAllowed();
        const $tbody = $(table.table().body());
        const $handles = $tbody.find(selectors.dragHandle);

        if ($.fn.sortable && $tbody.data('ui-sortable')) {
            $tbody.sortable(allowed ? 'enable' : 'disable');
        }

        $handles
            .attr('draggable', allowed ? 'true' : 'false')
            .toggleClass('opacity-50', !allowed)
            .css('cursor', allowed ? 'grab' : 'not-allowed')
            .attr('title', allowed
                ? (getTexts().dragToReorder || getTexts().reorder || 'Sırayı değiştir')
                : (getTexts().reorderNotAllowedShort || 'Sıralama için arama boşken Sıra kolonunu artan kullanın.'));
    }

    function isReorderAllowed() {
        if (!table) {
            return false;
        }

        const order = table.order();
        const firstOrder = Array.isArray(order) && order.length > 0 ? order[0] : null;

        const isOrderAsc = firstOrder &&
            Number(firstOrder[0]) === 2 &&
            String(firstOrder[1] || '').toLowerCase() === 'asc';

        const hasSearch = String(table.search() || '').trim().length > 0;

        return isOrderAsc && !hasSearch;
    }

    function showReorderNotAllowedMessage() {
        const texts = getTexts();
        const message = texts.reorderNotAllowed || 'Sıralama yapmak için arama boş olmalı ve tablo Sıra kolonuna göre artan sıralanmalıdır.';

        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError({
                responseJSON: {
                    message: message
                }
            });
            return;
        }

        alert(message);
    }

    function updateVisibleRowNumbers() {
        if (!table) {
            return;
        }

        const pageInfo = table.page.info();

        $(table.table().body())
            .find('tr[data-id]')
            .each(function (index) {
                const visibleNumber = pageInfo.start + index + 1;
                $(this).find('td').eq(0).text(visibleNumber);
                $(this).find('td').eq(2).text(visibleNumber);
            });
    }

    function persistReorder() {
        if (!table) {
            return;
        }

        if (!isReorderAllowed()) {
            reload(false);
            return;
        }

        const $table = $(selectors.table);
        const reorderUrl = resolveReorderUrl($table);

        if (!reorderUrl) {
            reload(false);
            return;
        }

        const pageInfo = table.page.info();
        const items = [];

        $(table.table().body())
            .find('tr[data-id]')
            .each(function (index) {
                const id = $(this).attr('data-id');

                if (!id) {
                    return;
                }

                items.push({
                    id: id,
                    order: pageInfo.start + index + 1
                });
            });

        if (!items.length) {
            reload(false);
            return;
        }

        $.ajax({
            url: reorderUrl,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify({ items: items }),
            headers: getAjaxHeaders($(document))
        })
            .done(function (response) {
                if (!response || response.success !== true) {
                    showError(response);
                    reload(false);
                    return;
                }

                showSuccess(response.message || getTexts().reorderSuccess || getTexts().updated || 'Kayıt güncellendi.');
                reload(false);
            })
            .fail(function (xhr) {
                showError(xhr);
                reload(false);
            });
    }

    function resolveReorderUrl($table) {
        const explicitUrl = $table.data('reorder-url');

        if (explicitUrl) {
            return explicitUrl;
        }

        const sourceUrl = String($table.data('source-url') || '');

        if (!sourceUrl) {
            return null;
        }

        return sourceUrl.replace(/\/GetList(?:\?.*)?$/i, '/Reorder');
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
        return window.Symplify.SubmissionTypes.texts ||
            window.Symplify.Lookup?.texts ||
            window.Symplify.Texts ||
            window.Symplify.texts ||
            {};
    }

    function showSuccess(message) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function' && message) {
            window.Symplify.Ajax.showSuccess(message);
            return;
        }

        if (message) {
            alert(message);
        }
    }

    function showError(xhr) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(xhr);
            return;
        }

        alert(xhr?.responseJSON?.message || xhr?.message || getTexts().genericError || 'İşlem sırasında hata oluştu.');
    }

    function ensureExpectedHeader($table) {
        const $headerCells = $table.find('thead tr th');

        if ($headerCells.length === 7) {
            return;
        }

        const texts = getTexts();
        const headers = [
            '#',
            '',
            texts.order || 'Sıra',
            texts.name || 'Ad',
            texts.description || 'Açıklama',
            texts.status || 'Durum',
            texts.actions || 'İşlemler'
        ];

        const headerHtml = headers.map(function (header, index) {
            const className = index === 6 ? ' class="text-end"' : '';
            return '<th' + className + '>' + escapeHtml(header) + '</th>';
        }).join('');

        $table.find('thead').html('<tr>' + headerHtml + '</tr>');
    }

    function ensureReorderStyles() {
        if (document.getElementById('symplify-lookup-reorder-styles')) {
            return;
        }

        const style = document.createElement('style');
        style.id = 'symplify-lookup-reorder-styles';
        style.textContent = '' +
            '.lookup-row-dragging{opacity:.65;}' +
            '.lookup-sort-placeholder td{height:56px;border:2px dashed #6b8cff;background:rgba(59,130,246,.06);}' +
            '.js-lookup-drag-handle{cursor:grab;}' +
            '.js-lookup-drag-handle:active{cursor:grabbing;}';

        document.head.appendChild(style);
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
