window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplateTransitions = window.Symplify.WorkflowTemplateTransitions || {};

window.Symplify.WorkflowTemplateTransitions.table = (function ($) {
    'use strict';

    const selectors = {
        table: '#workflowTemplateTransitionsTable',
        dragHandle: '.js-lookup-drag-handle'
    };

    let table;

    function init() {
        const $table = $(selectors.table);

        if (!$table.length || $table.data('symplify-feature-table-initialized')) {
            return;
        }

        ensureReorderStyles();

        table = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: false,
            ordering: false,
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
                    data.workflowTemplateId = getWorkflowTemplateId();

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
            columns: [
                { data: 'rowNumber', name: 'rowNumber', orderable: false, searchable: false },
                { data: 'id', name: 'drag', orderable: false, searchable: false, render: renderDragHandle },
                { data: 'order', name: 'order', orderable: false, searchable: false },
                { data: 'fromStatusName', name: 'fromStatusName', orderable: false, searchable: false, render: renderText },
                { data: 'toStatusName', name: 'toStatusName', orderable: false, searchable: false, render: renderText },
                { data: 'transitionName', name: 'transitionName', orderable: false, searchable: false, render: renderText },
                { data: 'transitionDescription', name: 'transitionDescription', orderable: false, searchable: false, render: renderDescription },
                { data: 'isActive', name: 'isActive', orderable: false, searchable: false, render: renderStatus },
                { data: 'id', name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ],
            createdRow: function (row, data) {
                $(row)
                    .attr('data-id', data?.id || '')
                    .attr('data-order', data?.order || '')
                    .attr('data-transition-id', data?.transactionStatusTransitionId || '')
                    .attr('data-is-active', data?.isActive === true ? 'true' : 'false');
            },
            drawCallback: function () {
                initializeReorder();
            }
        });

        $table.data('symplify-feature-table-initialized', true);
    }

    function reload(resetPaging) {
        if (table) {
            table.ajax.reload(null, resetPaging === true);
        }
    }

    function renderDragHandle() {
        const label = getTexts().dragToReorder || getTexts().reorder || 'Sırayı değiştir';

        return '<span role="button" tabindex="0" class="d-inline-flex align-items-center justify-content-center text-neutral-500 js-lookup-drag-handle" title="' + escapeHtml(label) + '" aria-label="' + escapeHtml(label) + '"><i class="ri-draggable"></i></span>';
    }

    function renderText(value) {
        return escapeHtml(value || '');
    }

    function renderDescription(value) {
        if (!value) {
            return '<span class="text-neutral-400">-</span>';
        }

        return '<span title="' + escapeHtml(value) + '">' + escapeHtml(value) + '</span>';
    }

    function renderStatus(isActive) {
        const label = isActive ? (getTexts().active || 'Aktif') : (getTexts().passive || 'Pasif');
        const cssClass = isActive ? 'bg-success-focus text-success-main' : 'bg-danger-focus text-danger-main';

        return '<span class="px-16 py-4 rounded-pill fw-medium text-sm ' + cssClass + '">' + escapeHtml(label) + '</span>';
    }

    function renderActions(id, type, row) {
        const texts = getTexts();
        const editText = texts.edit || 'Düzenle';
        const deleteText = texts.delete || 'Sil';

        return '<div class="d-flex align-items-center justify-content-end gap-2">'
            + '<button type="button" class="w-40-px h-40-px bg-primary-50 text-primary-600 rounded-circle d-inline-flex align-items-center justify-content-center js-workflow-template-transition-update-button" data-id="' + escapeHtml(id) + '" data-transition-id="' + escapeHtml(row?.transactionStatusTransitionId || '') + '" data-is-active="' + escapeHtml(row?.isActive === true ? 'true' : 'false') + '" title="' + escapeHtml(editText) + '"><i class="ri-pencil-line"></i></button>'
            + '<button type="button" class="w-40-px h-40-px bg-danger-50 text-danger-600 rounded-circle d-inline-flex align-items-center justify-content-center js-workflow-template-transition-delete-button" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(deleteText) + '"><i class="ri-delete-bin-line"></i></button>'
            + '</div>';
    }

    function initializeReorder() {
        if (!table || !$.fn.sortable) {
            return;
        }

        const $tbody = $(table.table().body());

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
            start: function (event, ui) {
                ui.item.addClass('lookup-row-dragging');
                ui.placeholder.html('<td colspan="9">&nbsp;</td>');
            },
            sort: updateVisibleRowNumbers,
            update: function () {
                updateVisibleRowNumbers();
                persistReorder();
            },
            stop: function (event, ui) {
                ui.item.removeClass('lookup-row-dragging');
                updateVisibleRowNumbers();
            }
        });
    }

    function updateVisibleRowNumbers() {
        const pageInfo = table.page.info();

        $(table.table().body()).find('tr[data-id]').each(function (index) {
            const visibleNumber = pageInfo.start + index + 1;

            $(this).find('td').eq(0).text(visibleNumber);
            $(this).find('td').eq(2).text(visibleNumber);
        });
    }

    function persistReorder() {
        const reorderUrl = $(selectors.table).data('reorder-url');

        if (!reorderUrl) {
            reload(false);
            return;
        }

        const pageInfo = table.page.info();
        const items = [];

        $(table.table().body()).find('tr[data-id]').each(function (index) {
            const id = $(this).attr('data-id');

            if (id) {
                items.push({
                    id: id,
                    order: pageInfo.start + index + 1
                });
            }
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
            data: JSON.stringify({
                workflowTemplateId: getWorkflowTemplateId(),
                items: items
            }),
            headers: getAjaxHeaders($(document)),
            beforeSend: function (xhr) {
                const token = getAntiForgeryToken($(document));

                if (token) {
                    xhr.setRequestHeader('RequestVerificationToken', token);
                }

                xhr.setRequestHeader('X-Culture', getCurrentCulture());
            }
        }).done(function (response) {
            if (!response || response.success !== true) {
                showError(response);
                reload(false);
                return;
            }

            showSuccess(response.message || getTexts().reorderSuccess || getTexts().updated || 'Kayıt güncellendi.');
            reload(false);
        }).fail(function (xhr) {
            showError(xhr);
            reload(false);
        });
    }

    function ensureReorderStyles() {
        if (document.getElementById('symplify-lookup-reorder-styles')) {
            return;
        }

        const style = document.createElement('style');
        style.id = 'symplify-lookup-reorder-styles';
        style.textContent = '.lookup-row-dragging{opacity:.65;}.lookup-sort-placeholder td{height:56px;border:2px dashed #6b8cff;background:rgba(59,130,246,.06);}.js-lookup-drag-handle{cursor:grab;}.js-lookup-drag-handle:active{cursor:grabbing;}';
        document.head.appendChild(style);
    }

    function getWorkflowTemplateId() {
        const $table = $(selectors.table);

        const fromTable = $table.data('workflow-template-id');

        if (fromTable) {
            return fromTable;
        }

        const query = new URLSearchParams(window.location.search);

        return query.get('workflowTemplateId') || '';
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
        return window.Symplify.WorkflowTemplateTransitions?.texts ||
            window.Symplify.Lookup?.texts ||
            window.Symplify.Texts ||
            window.Symplify.texts ||
            {};
    }

    function showSuccess(message) {
        if (window.Symplify.Ajax?.showSuccess && message) {
            window.Symplify.Ajax.showSuccess(message);
            return;
        }

        if (message) {
            alert(message);
        }
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