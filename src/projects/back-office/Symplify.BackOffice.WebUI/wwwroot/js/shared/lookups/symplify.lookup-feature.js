window.Symplify = window.Symplify || {};

window.Symplify.LookupFeature = (function ($) {
    'use strict';

    const defaultHeaders = ['#', '', 'Sıra', 'Ad', 'Açıklama', 'Durum', 'İşlemler'];

    function init(options) {
        const settings = $.extend({
            tableSelector: null,
            createFormSelector: null,
            createModalSelector: null,
            updateFormSelector: null,
            updateModalSelector: null,
            deleteFormSelector: null,
            editButtonSelector: '.js-lookup-edit',
            deleteButtonSelector: '.js-lookup-delete',
            activeText: getText('active', 'Aktif'),
            passiveText: getText('passive', 'Pasif'),
            reorderSuccessText: getText('reorderSuccess', 'Sıralama güncellendi.'),
            reorderWarningText: getText('reorderNotAllowed', 'Sürükle-bırak sıralama için arama kapalıyken Sıra kolonuna göre artan sıralama kullanılmalıdır.'),
            headers: defaultHeaders
        }, options || {});

        ensureReorderStyles();
        initTable(settings);
        bindCreate(settings);
        bindUpdate(settings);
        bindDelete(settings);
    }

    function initTable(settings) {
        const $table = $(settings.tableSelector);
        if (!$table.length || !$.fn.DataTable) return;

        if ($table.hasClass('js-lookup-table') && $table.data('feature')) {
            return;
        }

        if ($.fn.DataTable.isDataTable($table)) {
            return;
        }

        const sourceUrl = $table.data('source-url');

        if (sourceUrl) {
            ensureServerSideHeader($table, settings.headers);

            $table.DataTable({
                processing: true,
                serverSide: true,
                searching: true,
                ordering: true,
                paging: true,
                pageLength: 10,
                autoWidth: false,
                responsive: true,
                order: [[2, 'asc']],
                ajax: {
                    url: sourceUrl,
                    type: 'POST',
                    headers: getAjaxHeaders(),
                    data: function (data) {
                        data.culture = getCurrentCulture();
                        return data;
                    },
                    error: function (xhr) {
                        showAjaxError(xhr);
                    }
                },
                language: window.Symplify.DataTables?.language || {},
                columns: [
                    { data: 'rowNumber', name: 'rowNumber', orderable: false, searchable: false, className: 'text-nowrap' },
                    { data: null, name: 'reorderHandle', orderable: false, searchable: false, className: 'text-center text-nowrap', render: renderReorderHandle },
                    { data: 'order', name: 'order', orderable: true, searchable: false, className: 'text-nowrap' },
                    { data: 'name', name: 'name' },
                    { data: 'isActive', name: 'isActive', render: function (value) { return renderStatus(value, settings); } },
                    { data: 'id', name: 'actions', orderable: false, searchable: false, className: 'text-end', render: renderActions }
                ],
                createdRow: function (row, data) {
                    $(row)
                        .attr('data-id', data.id)
                        .attr('data-order', data.order);
                },
                drawCallback: function () {
                    bindRowReorder($table, this.api(), settings);
                }
            });
            return;
        }

        $table.DataTable({
            pageLength: 10,
            ordering: true,
            responsive: true,
            autoWidth: false,
            language: window.Symplify.DataTables?.language || {},
            columnDefs: [
                { orderable: false, targets: [0, -1] }
            ]
        });
    }

    function bindCreate(settings) {
        $(document).on('submit', settings.createFormSelector, function (event) {
            event.preventDefault();
            const $form = $(this);
            if ($form.valid && !$form.valid()) return;

            window.Symplify.Ajax.postForm($form)
                .done(function (response) {
                    $(settings.createModalSelector).modal('hide');
                    resetForm($form);
                    window.Symplify.Ajax.showSuccess(response?.message);
                    reloadOrRefresh(settings.tableSelector);
                })
                .fail(window.Symplify.Ajax.showError);
        });
    }

    function bindUpdate(settings) {
        $(document).on('click', settings.editButtonSelector, function () {
            const id = $(this).data('id');
            const $form = $(settings.updateFormSelector);
            const loadUrl = $form.data('load-url');
            if (!id || !loadUrl) return;

            $.get(loadUrl, { id: id })
                .done(function (response) { fillUpdateForm($form, response); })
                .fail(window.Symplify.Ajax.showError);
        });

        $(document).on('submit', settings.updateFormSelector, function (event) {
            event.preventDefault();
            const $form = $(this);
            if ($form.valid && !$form.valid()) return;

            window.Symplify.Ajax.postForm($form)
                .done(function (response) {
                    $(settings.updateModalSelector).modal('hide');
                    window.Symplify.Ajax.showSuccess(response?.message);
                    reloadOrRefresh(settings.tableSelector);
                })
                .fail(window.Symplify.Ajax.showError);
        });
    }

    function bindDelete(settings) {
        $(document).on('click', settings.deleteButtonSelector, function () {
            const id = $(this).data('id');
            const $form = $(settings.deleteFormSelector);
            if (!id || !$form.length) return;

            window.Symplify.Ajax.confirm().then(function (result) {
                if (!result.isConfirmed) return;

                $.ajax({
                    url: $form.attr('action'),
                    type: $form.attr('method') || 'POST',
                    data: { id: id },
                    headers: window.Symplify.Ajax.buildTokenHeader($form)
                })
                    .done(function (response) {
                        window.Symplify.Ajax.showSuccess(response?.message);
                        reloadOrRefresh(settings.tableSelector);
                    })
                    .fail(window.Symplify.Ajax.showError);
            });
        });
    }

    function bindRowReorder($table, dataTable, settings) {
        const $tbody = $table.find('tbody');
        let draggedRow = null;

        $tbody.off('.lookupReorder');

        $tbody.on('dragstart.lookupReorder', '.js-lookup-reorder-handle', function (event) {
            if (!canReorder(dataTable, settings)) {
                event.preventDefault();
                return false;
            }

            draggedRow = $(this).closest('tr')[0];
            $(draggedRow).addClass('datatable-row-dragging');

            const originalEvent = event.originalEvent;
            if (originalEvent && originalEvent.dataTransfer) {
                originalEvent.dataTransfer.effectAllowed = 'move';
                originalEvent.dataTransfer.setData('text/plain', $(draggedRow).attr('data-id') || '');
            }
        });

        $tbody.on('dragend.lookupReorder', '.js-lookup-reorder-handle', function () {
            $tbody.find('tr').removeClass('datatable-row-dragging datatable-row-drop-target datatable-row-drop-after');
            draggedRow = null;
        });

        $tbody.on('dragover.lookupReorder', 'tr', function (event) {
            if (!draggedRow || this === draggedRow) return;

            event.preventDefault();

            const originalEvent = event.originalEvent;
            const rect = this.getBoundingClientRect();
            const insertAfter = originalEvent.clientY > rect.top + (rect.height / 2);

            $tbody.find('tr').removeClass('datatable-row-drop-target datatable-row-drop-after');
            $(this).addClass('datatable-row-drop-target');

            if (insertAfter) {
                $(this).addClass('datatable-row-drop-after');
            }
        });

        $tbody.on('dragleave.lookupReorder', 'tr', function () {
            $(this).removeClass('datatable-row-drop-target datatable-row-drop-after');
        });

        $tbody.on('drop.lookupReorder', 'tr', function (event) {
            if (!draggedRow || this === draggedRow) return;

            event.preventDefault();

            const originalEvent = event.originalEvent;
            const rect = this.getBoundingClientRect();
            const insertAfter = originalEvent.clientY > rect.top + (rect.height / 2);

            persistCurrentPageReorder($table, dataTable, draggedRow, this, insertAfter, settings);
        });
    }

    function canReorder(dataTable, settings) {
        const order = dataTable.order();
        const searchValue = dataTable.search();
        const isOrderAscending = Array.isArray(order) && order.length > 0 && order[0][0] === 2 && String(order[0][1]).toLowerCase() === 'asc';

        if (searchValue || !isOrderAscending) {
            dataTable.search('').order([[2, 'asc']]).draw();
            showWarning(settings.reorderWarningText);
            return false;
        }

        return true;
    }

    function persistCurrentPageReorder($table, dataTable, sourceRow, targetRow, insertAfter, settings) {
        const sourceId = String($(sourceRow).attr('data-id') || '');
        const targetId = String($(targetRow).attr('data-id') || '');

        if (!sourceId || !targetId || sourceId === targetId) return;

        const currentRows = dataTable
            .rows({ page: 'current', order: 'applied', search: 'applied' })
            .data()
            .toArray();

        const sourceIndex = currentRows.findIndex(row => String(row.id) === sourceId);
        const targetIndex = currentRows.findIndex(row => String(row.id) === targetId);

        if (sourceIndex < 0 || targetIndex < 0) return;

        const moved = currentRows.splice(sourceIndex, 1)[0];
        let insertIndex = targetIndex;

        if (sourceIndex < targetIndex) {
            insertIndex = targetIndex - 1;
        }

        if (insertAfter) {
            insertIndex += 1;
        }

        currentRows.splice(insertIndex, 0, moved);

        const orderValues = currentRows
            .map(row => Number(row.order))
            .filter(order => Number.isFinite(order) && order > 0)
            .sort((left, right) => left - right);

        const items = currentRows.map(function (row, index) {
            return {
                id: row.id,
                order: orderValues[index] || (index + 1)
            };
        });

        const reorderUrl = resolveReorderUrl($table);

        if (!reorderUrl) {
            showWarning(getText('reorderEndpointMissing', 'Sıralama endpoint adresi bulunamadı.'));
            return;
        }

        $.ajax({
            url: reorderUrl,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ items: items }),
            headers: getAjaxHeaders()
        })
            .done(function (response) {
                if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function') {
                    window.Symplify.Ajax.showSuccess(response?.message || settings.reorderSuccessText);
                }

                dataTable.ajax.reload(null, false);
            })
            .fail(showAjaxError);
    }

    function ensureServerSideHeader($table, headers) {
        const normalizedHeaders = Array.isArray(headers) && headers.length === 6 ? headers : defaultHeaders;
        let $thead = $table.find('thead');

        if (!$thead.length) {
            $thead = $('<thead></thead>').prependTo($table);
        }

        const currentCount = $thead.find('tr:first th').length;
        if (currentCount === 6) return;

        const html = '<tr>' + normalizedHeaders.map(function (header) {
            return '<th>' + escapeHtml(header) + '</th>';
        }).join('') + '</tr>';

        $thead.html(html);
    }

    function renderReorderHandle() {
        const label = getText('dragToReorder', 'Sırayı değiştirmek için sürükleyin');
        return '<span class="datatable-reorder-handle js-lookup-reorder-handle" draggable="true" title="' + escapeHtml(label) + '"><i class="ri-draggable"></i></span>'; 
    }

    function fillUpdateForm($form, response) {
        if (!response) return;

        $form.find('[name="Id"]').val(response.id || response.Id);
        $form.find('[name="IsActive"]').filter('[type="checkbox"]').prop('checked', response.isActive ?? response.IsActive);

        const translations = response.translations || response.Translations || [];
        translations.forEach(function (translation, index) {
            setValue($form, `Translations[${index}].LanguageId`, translation.languageId || translation.LanguageId);
            setValue($form, `Translations[${index}].Culture`, translation.culture || translation.Culture);
            setValue($form, `Translations[${index}].Name`, translation.name || translation.Name);
            setValue($form, `Translations[${index}].Description`, translation.description || translation.Description);
        });
    }

    function setValue($form, name, value) {
        $form.find(`[name="${name}"]`).val(value || '');
    }

    function renderStatus(value, settings) {
        if (value) {
            return `<span class="badge bg-success-100 text-success-600 rounded-pill px-12 py-8">${settings.activeText}</span>`;
        }
        return `<span class="badge bg-danger-100 text-danger-600 rounded-pill px-12 py-8">${settings.passiveText}</span>`;
    }

    function renderActions(id) {
        return `
            <div class="d-flex align-items-center justify-content-end gap-2">
                <button type="button" class="btn btn-info-100 text-info-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center js-lookup-edit" data-id="${escapeHtml(id)}" data-bs-toggle="modal" data-bs-target=".js-update-lookup-modal">
                    <i class="ri-edit-line"></i>
                </button>
                <button type="button" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center js-lookup-delete" data-id="${escapeHtml(id)}">
                    <i class="ri-delete-bin-line"></i>
                </button>
            </div>`;
    }

    function resetForm($form) {
        if ($form[0]) $form[0].reset();
        $form.find('.nav-link').removeClass('active').first().addClass('active');
        $form.find('.tab-pane').removeClass('show active').first().addClass('show active');
    }

    function reloadOrRefresh(tableSelector) {
        const $table = $(tableSelector);
        if ($.fn.DataTable && $.fn.DataTable.isDataTable($table)) {
            const dataTable = $table.DataTable();
            if ($table.data('source-url')) dataTable.ajax.reload(null, false);
            else window.location.reload();
            return;
        }
        window.location.reload();
    }

    function resolveReorderUrl($table) {
        const explicitUrl = $table.data('reorder-url');
        if (explicitUrl) return explicitUrl;

        const sourceUrl = $table.data('source-url');
        if (!sourceUrl) return null;

        return String(sourceUrl).replace(/GetList(\?.*)?$/i, 'Reorder$1');
    }

    function getAjaxHeaders() {
        const headers = {
            'X-Culture': getCurrentCulture()
        };

        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.buildTokenHeader === 'function') {
            return $.extend(headers, window.Symplify.Ajax.buildTokenHeader($(document)));
        }

        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.getAntiForgeryToken === 'function') {
            const token = window.Symplify.Ajax.getAntiForgeryToken();
            if (token) headers.RequestVerificationToken = token;
        }

        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }

    function showAjaxError(xhr) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(xhr);
            return;
        }

        alert(xhr?.responseJSON?.message || getText('genericError', 'İşlem sırasında hata oluştu.'));
    }

    function showWarning(message) {
        if (window.Swal && typeof window.Swal.fire === 'function') {
            window.Swal.fire({ icon: 'warning', text: message });
            return;
        }

        console.warn(message);
    }


    function getText(key, fallback) {
        const sources = [
            window.Symplify.Lookup && window.Symplify.Lookup.texts,
            window.Symplify.Texts,
            window.Symplify.texts
        ];

        for (let i = 0; i < sources.length; i++) {
            const source = sources[i];
            if (source && source[key]) {
                return source[key];
            }
        }

        return fallback;
    }

    function ensureReorderStyles() {
        if (document.getElementById('symplify-lookup-reorder-style')) return;

        const style = document.createElement('style');
        style.id = 'symplify-lookup-reorder-style';
        style.textContent = `
            .datatable-reorder-handle { cursor: grab; color: #64748b; display: inline-flex; align-items: center; justify-content: center; min-width: 24px; }
            .datatable-reorder-handle:active { cursor: grabbing; }
            tr.datatable-row-dragging { opacity: .55; }
            tr.datatable-row-drop-target { outline: 2px dashed rgba(72, 127, 255, .55); outline-offset: -2px; }
            tr.datatable-row-drop-after { box-shadow: inset 0 -2px 0 rgba(72, 127, 255, .8); }
        `;
        document.head.appendChild(style);
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) return '';

        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    return { init: init };
})(jQuery);
