window.Symplify = window.Symplify || {};
window.Symplify.CongressBoardMembers = window.Symplify.CongressBoardMembers || {};

window.Symplify.CongressBoardMembers.Index = (function ($) {
    'use strict';

    const selectors = {
        panel: '#congressBoardMemberPanel',
        table: '#congressBoardMembersTable',
        boardTable: '#congressBoardsTable',
        modalContainer: '#congressBoardMemberModalContainer',
        createButton: '#openCreateBoardMemberModalButton',
        excelUploadButton: '#openBoardMemberExcelUploadModalButton',
        createBoardButton: '#openCreateCongressBoardModalButton',
        createForm: '#createCongressBoardMemberForm',
        updateForm: '#updateCongressBoardMemberForm',
        signatureForm: '#updateBoardMemberSignatureForm',
        createBoardForm: '#createCongressBoardForm',
        updateBoardForm: '#updateCongressBoardForm',
        excelUploadForm: '#uploadCongressBoardMembersExcelForm',
        boardFilter: '.committee-board-filter',
        titleFilter: '.committee-title-filter',
        statusFilter: '.committee-status-filter',
        resetFilter: '.committee-filter-reset',
        memberDragHandle: '.js-board-member-drag-handle',
        boardDragHandle: '.js-congress-board-drag-handle'
    };

    let table;
    let boardTable;

    function init() {
        if (!$(selectors.panel).length || !$(selectors.table).length) return;

        loadBoards();
        loadFilterOptions();
        initializeTable();
        bindEvents();
        injectReorderStyles();
    }

    function bindEvents() {
        $(document).off('click.boardMembersCreate', selectors.createButton).on('click.boardMembersCreate', selectors.createButton, openCreateModal);
        $(document).off('click.boardMembersExcel', selectors.excelUploadButton).on('click.boardMembersExcel', selectors.excelUploadButton, openExcelUploadModal);
        $(document).off('click.boardsCreate', selectors.createBoardButton).on('click.boardsCreate', selectors.createBoardButton, openCreateBoardModal);
        $(document).off('click.boardMembersEdit', '.js-edit-board-member').on('click.boardMembersEdit', '.js-edit-board-member', openUpdateModal);
        $(document).off('click.boardMembersSignature', '.js-signature-board-member').on('click.boardMembersSignature', '.js-signature-board-member', openSignatureModal);
        $(document).off('click.boardMembersDelete', '.js-delete-board-member').on('click.boardMembersDelete', '.js-delete-board-member', deleteMember);
        $(document).off('click.boardsEdit', '.js-edit-congress-board').on('click.boardsEdit', '.js-edit-congress-board', openUpdateBoardModal);
        $(document).off('click.boardsDelete', '.js-delete-congress-board').on('click.boardsDelete', '.js-delete-congress-board', deleteBoard);
        $(document).off('submit.boardMembersCreate', selectors.createForm).on('submit.boardMembersCreate', selectors.createForm, submitForm);
        $(document).off('submit.boardMembersUpdate', selectors.updateForm).on('submit.boardMembersUpdate', selectors.updateForm, submitForm);
        $(document).off('submit.boardMembersSignatureUpdate', selectors.signatureForm).on('submit.boardMembersSignatureUpdate', selectors.signatureForm, submitForm);
        $(document).off('submit.boardsCreate', selectors.createBoardForm).on('submit.boardsCreate', selectors.createBoardForm, submitBoardForm);
        $(document).off('submit.boardsUpdate', selectors.updateBoardForm).on('submit.boardsUpdate', selectors.updateBoardForm, submitBoardForm);
        $(document).off('submit.boardMembersExcelUpload', selectors.excelUploadForm).on('submit.boardMembersExcelUpload', selectors.excelUploadForm, submitExcelUpload);
        $(document).off('change.boardMembersFilters', selectors.boardFilter + ',' + selectors.titleFilter + ',' + selectors.statusFilter).on('change.boardMembersFilters', selectors.boardFilter + ',' + selectors.titleFilter + ',' + selectors.statusFilter, reload);
        $(document).off('click.boardMembersFilterReset', selectors.resetFilter).on('click.boardMembersFilterReset', selectors.resetFilter, resetFilters);
    }

    function loadBoards() {
        const $panel = $(selectors.panel);
        const url = $panel.data('board-list-url');

        if (!url || !$(selectors.boardTable).length) return;

        $.get(url)
            .done(function (response) {
                const items = response && response.items ? response.items : [];
                initializeBoardTable(items);
                fillBoardFilterFromBoards(items);
            })
            .fail(function (xhr) {
                initializeBoardTable([]);
                showError(xhr);
            });
    }

    function initializeBoardTable(items) {
        const $table = $(selectors.boardTable);

        if (!$.fn.DataTable) {
            renderBoardsFallback(items);
            return;
        }

        if ($.fn.DataTable.isDataTable($table)) {
            boardTable = $table.DataTable();
            boardTable.clear();
            boardTable.rows.add(items || []);
            boardTable.draw(false);
            return;
        }

        $table.find('tbody').empty();

        boardTable = $table.DataTable({
            data: items || [],
            searching: true,
            ordering: true,
            paging: false,
            info: false,
            autoWidth: false,
            responsive: false,
            order: [[0, 'asc']],
            columns: [
                { data: 'order', name: 'order', orderable: true, searchable: false, className: 'text-nowrap', render: renderBoardOrder },
                { data: 'name', name: 'name', orderable: true, searchable: true, render: renderBoardName },
                { data: 'description', name: 'description', orderable: true, searchable: true, render: renderText },
                { data: 'isActive', name: 'isActive', orderable: true, searchable: false, className: 'text-nowrap', render: renderStatus },
                { data: null, name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderBoardActions }
            ],
            rowCallback: function (row, data) {
                $(row)
                    .attr('data-id', data && data.id ? data.id : '')
                    .attr('data-order', data && data.order ? data.order : '');
            },
            drawCallback: function () {
                initializeBoardReorder();
                updateBoardDragHandleState();
            },
            language: getDataTableLanguage()
        });
    }

    function renderBoardsFallback(items) {
        const $tbody = $(selectors.boardTable).find('tbody');
        $tbody.empty();

        if (!items || !items.length) {
            $tbody.append('<tr><td colspan="5" class="text-center text-neutral-500 py-24">' + escapeHtml(text('noBoards', 'Henüz kurul türü eklenmedi. Üye eklemeden önce kurul ekleyin.')) + '</td></tr>');
            return;
        }

        items.forEach(function (item) {
            $tbody.append('<tr data-id="' + escapeHtml(item.id) + '">' +
                '<td class="text-nowrap">' + renderBoardOrder(item.order, 'display', item) + '</td>' +
                '<td>' + renderBoardName(item.name, 'display', item) + '</td>' +
                '<td>' + renderText(item.description) + '</td>' +
                '<td class="text-nowrap">' + renderStatus(item.isActive) + '</td>' +
                '<td class="text-end text-nowrap">' + renderBoardActions(null, 'display', item) + '</td>' +
            '</tr>');
        });
    }

    function fillBoardFilterFromBoards(items) {
        const options = (items || [])
            .filter(function (item) { return item && item.isActive === true && item.name; })
            .map(function (item) { return { value: item.name, text: item.name }; });

        fillSelect($(selectors.boardFilter), options, text('all', 'Tümü'));
    }

    function refreshBoardsAndDependentData() {
        loadBoards();
        loadFilterOptions();
        reload(false);
    }

    function loadFilterOptions() {
        const $panel = $(selectors.panel);
        const url = $panel.data('filter-options-url');

        if (!url) return;

        $.get(url)
            .done(function (response) {
                fillSelect($(selectors.boardFilter), response ? response.boardOptions : null, text('all', 'Tümü'));
                fillSelect($(selectors.titleFilter), response ? response.academicTitleOptions : null, text('all', 'Tümü'));
            })
            .fail(showError);
    }

    function fillSelect($select, items, firstText) {
        const currentValue = $select.val();
        $select.empty();
        $select.append($('<option/>').attr('value', '').text(firstText));

        (items || []).forEach(function (item) {
            if (!item || !item.value) return;
            $select.append($('<option/>').attr('value', item.value).text(item.text || item.value));
        });

        if (currentValue) $select.val(currentValue);
    }

    function initializeTable() {
        const $panel = $(selectors.panel);
        const $table = $(selectors.table);

        if (!$.fn.DataTable) {
            console.error('DataTables plugin bulunamadı. Congress board members tablosu başlatılamadı.');
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
            pageLength: 25,
            autoWidth: false,
            responsive: false,
            order: [[0, 'asc']],
            ajax: {
                url: $panel.data('source-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: function (data) {
                    data.congressId = $panel.data('congress-id');
                    data.boardName = $(selectors.boardFilter).val();
                    data.academicTitle = $(selectors.titleFilter).val();
                    data.status = $(selectors.statusFilter).val();
                    return data;
                },
                dataSrc: function (json) {
                    updateSummary(json ? json.summary : null);
                    return json && json.data ? json.data : [];
                },
                error: showError
            },
            columns: [
                { data: 'order', name: 'order', orderable: true, searchable: false, className: 'text-nowrap', render: renderMemberOrder },
                { data: 'boardName', name: 'boardName', orderable: true, searchable: true, render: renderText },
                { data: 'academicTitle', name: 'academicTitle', orderable: true, searchable: true, render: renderText },
                { data: 'fullName', name: 'fullName', orderable: true, searchable: true, render: renderFullName },
                { data: 'institution', name: 'institution', orderable: true, searchable: true, render: renderText },
                { data: null, name: 'isAcceptanceLetterSigner', orderable: true, searchable: false, className: 'text-nowrap', render: renderSignatureAuthority },
                { data: 'isActive', name: 'isActive', orderable: true, searchable: false, className: 'text-nowrap', render: renderStatus },
                { data: null, name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ],
            rowCallback: function (row, data) {
                $(row)
                    .attr('data-id', data && data.id ? data.id : '')
                    .attr('data-board-id', data && data.congressBoardId ? data.congressBoardId : '')
                    .attr('data-order', data && data.order ? data.order : '');
            },
            drawCallback: function () {
                initializeMemberReorder();
                updateMemberDragHandleState();
                initializeAvatarFallbacks($(selectors.table));
            },
            language: getDataTableLanguage()
        });
    }

    function openCreateModal() {
        $.get($(selectors.panel).data('create-modal-url'))
            .done(function (html) { showModalHtml(html, '#createCommitteeMemberModal'); })
            .fail(showError);
    }

    function openExcelUploadModal() {
        $.get($(selectors.panel).data('excel-upload-modal-url'))
            .done(function (html) { showModalHtml(html, '#committeeExcelUploadModal'); })
            .fail(showError);
    }

    function openUpdateModal() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        $.get($panel.data('edit-modal-url'), { id: $button.data('id'), congressId: $panel.data('congress-id') })
            .done(function (html) { showModalHtml(html, '#updateCommitteeMemberModal'); })
            .fail(showError);
    }

    function openSignatureModal() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        $.get($panel.data('signature-modal-url'), { id: $button.data('id'), congressId: $panel.data('congress-id') })
            .done(function (html) { showModalHtml(html, '#updateBoardMemberSignatureModal'); })
            .fail(showError);
    }

    function openCreateBoardModal() {
        $.get($(selectors.panel).data('board-create-modal-url'))
            .done(function (html) { showModalHtml(html, '#createCongressBoardModal'); })
            .fail(showError);
    }

    function openUpdateBoardModal() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        $.get($panel.data('board-edit-modal-url'), { id: $button.data('id'), congressId: $panel.data('congress-id') })
            .done(function (html) { showModalHtml(html, '#updateCongressBoardModal'); })
            .fail(showError);
    }

    function submitForm(event) {
        event.preventDefault();
        const $form = $(this);

        if (hasJQueryValidation() && !$form.valid()) {
            focusFirstInvalidField($form);
            return;
        }

        setBusy($form, true);

        postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    if (renderValidationErrors($form, response)) return;
                    showError(response);
                    return;
                }

                hideModal($form.closest('.modal'));
                reload(false);
                showSuccess(response.message || text('saved', 'Kayıt kaydedildi.'));
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) return;
                showError(xhr);
            })
            .always(function () { setBusy($form, false); });
    }

    function submitBoardForm(event) {
        event.preventDefault();
        const $form = $(this);

        if (hasJQueryValidation() && !$form.valid()) {
            focusFirstInvalidField($form);
            return;
        }

        setBusy($form, true);

        postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    if (renderValidationErrors($form, response)) return;
                    showError(response);
                    return;
                }

                hideModal($form.closest('.modal'));
                refreshBoardsAndDependentData();
                showSuccess(response.message || text('saved', 'Kayıt kaydedildi.'));
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) return;
                showError(xhr);
            })
            .always(function () { setBusy($form, false); });
    }

    function submitExcelUpload(event) {
        event.preventDefault();
        const $form = $(this);
        const $result = $form.find('[data-excel-upload-result]');

        setBusy($form, true);
        $result.addClass('d-none').empty();

        postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    renderExcelResult($result, response, false);
                    return;
                }

                renderExcelResult($result, response, true);
                reload(false);
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) return;
                showError(xhr);
            })
            .always(function () { setBusy($form, false); });
    }

    function deleteMember() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        confirmAction({
            title: text('deleteConfirmTitle', 'Emin misiniz?'),
            text: text('deleteConfirmText', 'Bu kurul üyesi silinecek.'),
            confirmButtonText: text('deleteConfirmButton', 'Sil')
        }).then(function (result) {
            if (!result || result.isConfirmed !== true) return;

            $.ajax({
                url: $panel.data('delete-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: { id: $button.data('id'), congressId: $panel.data('congress-id') }
            })
                .done(function (response) {
                    if (!response || response.success !== true) { showError(response); return; }
                    reload(false);
                    showSuccess(response.message || text('deleted', 'Kayıt silindi.'));
                })
                .fail(showError);
        });
    }

    function deleteBoard() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        confirmAction({
            title: text('deleteConfirmTitle', 'Emin misiniz?'),
            text: text('deleteBoardConfirmText', 'Bu kurul türü silinecek. Bu kurula bağlı üye varsa silme işlemi yapılmaz.'),
            confirmButtonText: text('deleteConfirmButton', 'Sil')
        }).then(function (result) {
            if (!result || result.isConfirmed !== true) return;

            $.ajax({
                url: $panel.data('board-delete-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: { id: $button.data('id'), congressId: $panel.data('congress-id') }
            })
                .done(function (response) {
                    if (!response || response.success !== true) { showError(response); return; }
                    refreshBoardsAndDependentData();
                    showSuccess(response.message || text('deleted', 'Kayıt silindi.'));
                })
                .fail(showError);
        });
    }

    function initializeBoardReorder() {
        if (!boardTable) return;
        initializeReorderForTable(boardTable, selectors.boardDragHandle, isBoardReorderAllowed, persistBoardReorder, showBoardReorderNotAllowedMessage, 'boardReorder');
    }

    function initializeMemberReorder() {
        if (!table) return;
        initializeReorderForTable(table, selectors.memberDragHandle, isMemberReorderAllowed, persistMemberReorder, showMemberReorderNotAllowedMessage, 'memberReorder');
    }

    function initializeReorderForTable(dataTable, handleSelector, allowFn, persistFn, notAllowedFn, namespace) {
        const $tbody = $(dataTable.table().body());

        if ($.fn.sortable) {
            $tbody.off('.' + namespace + 'Native');

            if ($tbody.data('ui-sortable')) {
                $tbody.sortable('destroy');
            }

            $tbody.sortable({
                items: 'tr[data-id]',
                handle: handleSelector,
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
                    if (!allowFn()) {
                        $(this).sortable('cancel');
                        notAllowedFn();
                        return;
                    }

                    ui.item.addClass('lookup-row-dragging');
                    ui.placeholder.html('<td colspan="' + ui.item.children().length + '">&nbsp;</td>');
                },
                update: function () {
                    updateVisibleOrderValues(dataTable);
                    persistFn();
                },
                stop: function (event, ui) {
                    ui.item.removeClass('lookup-row-dragging');
                    updateVisibleOrderValues(dataTable);
                }
            });

            $tbody.sortable(allowFn() ? 'enable' : 'disable');
            return;
        }

        initializeNativeReorderForTable(dataTable, handleSelector, allowFn, persistFn, notAllowedFn, namespace);
    }

    function initializeNativeReorderForTable(dataTable, handleSelector, allowFn, persistFn, notAllowedFn, namespace) {
        const $tbody = $(dataTable.table().body());
        let draggedRow = null;
        let dragChanged = false;

        $tbody.off('.' + namespace + 'Native');

        $tbody.on('dragstart.' + namespace + 'Native', handleSelector, function (event) {
            if (!allowFn()) {
                event.preventDefault();
                notAllowedFn();
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

        $tbody.on('dragover.' + namespace + 'Native', 'tr[data-id]', function (event) {
            if (!draggedRow || draggedRow === this) return;

            event.preventDefault();

            const rect = this.getBoundingClientRect();
            const mouseY = event.originalEvent.clientY;
            const shouldInsertAfter = mouseY > rect.top + rect.height / 2;

            if (shouldInsertAfter) this.parentNode.insertBefore(draggedRow, this.nextSibling);
            else this.parentNode.insertBefore(draggedRow, this);

            dragChanged = true;
            updateVisibleOrderValues(dataTable);
        });

        $tbody.on('drop.' + namespace + 'Native', 'tr[data-id]', function (event) {
            event.preventDefault();
        });

        $tbody.on('dragend.' + namespace + 'Native', handleSelector, function () {
            if (draggedRow) $(draggedRow).removeClass('lookup-row-dragging');
            if (draggedRow && dragChanged) persistFn();
            draggedRow = null;
            dragChanged = false;
        });
    }

    function isBoardReorderAllowed() {
        if (!boardTable) return false;

        const order = boardTable.order();
        const firstOrder = Array.isArray(order) && order.length > 0 ? order[0] : null;
        const isOrderAsc = firstOrder && Number(firstOrder[0]) === 0 && String(firstOrder[1] || '').toLowerCase() === 'asc';
        const hasSearch = String(boardTable.search() || '').trim().length > 0;

        return isOrderAsc && !hasSearch;
    }

    function isMemberReorderAllowed() {
        if (!table) return false;

        const order = table.order();
        const firstOrder = Array.isArray(order) && order.length > 0 ? order[0] : null;
        const isOrderAsc = firstOrder && Number(firstOrder[0]) === 0 && String(firstOrder[1] || '').toLowerCase() === 'asc';
        const hasSearch = String(table.search() || '').trim().length > 0;
        const boardIds = new Set();

        $(table.table().body()).find('tr[data-id]').each(function () {
            const boardId = $(this).attr('data-board-id');
            if (boardId) boardIds.add(boardId);
        });

        return isOrderAsc && !hasSearch && boardIds.size <= 1;
    }

    function updateBoardDragHandleState() {
        updateDragHandleState(boardTable, selectors.boardDragHandle, isBoardReorderAllowed, text('dragHandle', 'Sırayı değiştirmek için sürükleyin'), text('reorderNotAllowedShort', 'Sıralama için arama boşken Sıra No kolonunu artan kullanın.'));
    }

    function updateMemberDragHandleState() {
        updateDragHandleState(table, selectors.memberDragHandle, isMemberReorderAllowed, text('dragHandle', 'Sırayı değiştirmek için sürükleyin'), text('memberReorderNotAllowedShort', 'Üye sıralaması için arama boş olmalı, Sıra No artan olmalı ve görünen kayıtlar tek kurul türüne ait olmalıdır.'));
    }

    function updateDragHandleState(dataTable, handleSelector, allowFn, allowedTitle, blockedTitle) {
        if (!dataTable) return;

        const allowed = allowFn();
        const $tbody = $(dataTable.table().body());
        const $handles = $tbody.find(handleSelector);

        if ($.fn.sortable && $tbody.data('ui-sortable')) {
            $tbody.sortable(allowed ? 'enable' : 'disable');
        }

        $handles
            .attr('draggable', allowed ? 'true' : 'false')
            .toggleClass('opacity-50', !allowed)
            .css('cursor', allowed ? 'grab' : 'not-allowed')
            .attr('title', allowed ? allowedTitle : blockedTitle);
    }

    function showBoardReorderNotAllowedMessage() {
        showError({ responseJSON: { message: text('reorderNotAllowed', 'Sıralama yapmak için arama boş olmalı ve tablo Sıra No kolonuna göre artan sıralanmalıdır.') } });
    }

    function showMemberReorderNotAllowedMessage() {
        showError({ responseJSON: { message: text('memberReorderNotAllowed', 'Kurul üyesi sıralaması için arama boş olmalı, Sıra No kolonu artan seçilmeli ve görünen kayıtlar tek kurul türüne ait olmalıdır.') } });
    }

    function updateVisibleOrderValues(dataTable) {
        if (!dataTable) return;

        const pageInfo = dataTable.page && typeof dataTable.page.info === 'function'
            ? dataTable.page.info()
            : { start: 0 };

        $(dataTable.table().body()).find('tr[data-id]').each(function (index) {
            const visibleNumber = (pageInfo.start || 0) + index + 1;
            $(this).find('.js-order-value').text(visibleNumber);
        });
    }

    function persistBoardReorder() {
        if (!boardTable || !isBoardReorderAllowed()) {
            reloadBoards(false);
            return;
        }

        const $panel = $(selectors.panel);
        const reorderUrl = $panel.data('board-reorder-url');

        if (!reorderUrl) {
            showError({ responseJSON: { message: text('reorderEndpointMissing', 'Sıralama endpoint adresi bulunamadı.') } });
            reloadBoards(false);
            return;
        }

        const items = collectReorderItems(boardTable);

        if (!items.length) {
            reloadBoards(false);
            return;
        }

        postReorder(reorderUrl, items)
            .done(function (response) {
                if (!response || response.success !== true) {
                    showError(response);
                    reloadBoards(false);
                    return;
                }

                showSuccess(response.message || text('reordered', 'Sıralama güncellendi.'));
                loadBoards();
            })
            .fail(function (xhr) {
                showError(xhr);
                reloadBoards(false);
            });
    }

    function persistMemberReorder() {
        if (!table || !isMemberReorderAllowed()) {
            reload(false);
            return;
        }

        const $panel = $(selectors.panel);
        const reorderUrl = $panel.data('reorder-url');

        if (!reorderUrl) {
            showError({ responseJSON: { message: text('reorderEndpointMissing', 'Sıralama endpoint adresi bulunamadı.') } });
            reload(false);
            return;
        }

        const items = collectReorderItems(table);

        if (!items.length) {
            reload(false);
            return;
        }

        postReorder(reorderUrl, items)
            .done(function (response) {
                if (!response || response.success !== true) {
                    showError(response);
                    reload(false);
                    return;
                }

                showSuccess(response.message || text('reordered', 'Sıralama güncellendi.'));
                reload(false);
            })
            .fail(function (xhr) {
                showError(xhr);
                reload(false);
            });
    }

    function collectReorderItems(dataTable) {
        const pageInfo = dataTable.page && typeof dataTable.page.info === 'function'
            ? dataTable.page.info()
            : { start: 0 };
        const items = [];

        $(dataTable.table().body()).find('tr[data-id]').each(function (index) {
            const id = $(this).attr('data-id');
            if (!id) return;

            items.push({
                id: id,
                order: (pageInfo.start || 0) + index + 1
            });
        });

        return items;
    }

    function postReorder(url, items) {
        return $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify({ items: items }),
            headers: buildAjaxHeaders($(selectors.panel))
        });
    }

    function reloadBoards(resetPaging) {
        if (boardTable) boardTable.draw(resetPaging === true);
        else loadBoards();
    }

    function showModalHtml(html, modalSelector) {
        cleanupModalArtifacts();
        $(selectors.modalContainer).empty();

        const $html = $(html);
        const $modal = $html.filter(modalSelector).add($html.find(modalSelector)).first();

        if (!$modal.length) { showError(text('modalNotFound', 'Modal içeriği yüklenemedi.')); return; }

        $modal.appendTo(document.body);
        ensureScrollableModal($modal);
        initializeModal($modal);

        const modalElement = $modal[0];

        $modal.one('hidden.bs.modal', function () {
            const instance = bootstrap.Modal.getInstance(modalElement);
            if (instance) instance.dispose();
            $modal.remove();
            cleanupModalArtifacts();
        });

        bootstrap.Modal.getOrCreateInstance(modalElement, { backdrop: true, focus: true, keyboard: true }).show();
    }

    function ensureScrollableModal($modal) {
        injectScrollableModalStyles();

        $modal.addClass('symplify-scrollable-modal');
        $modal.find('.modal-dialog').addClass('modal-dialog-scrollable');
        $modal.find('.modal-content > form').addClass('symplify-scrollable-modal-form');
    }

    function injectScrollableModalStyles() {
        if (document.getElementById('symplify-scrollable-modal-style')) return;

        const style = document.createElement('style');
        style.id = 'symplify-scrollable-modal-style';
        style.textContent = `
            .symplify-scrollable-modal .modal-dialog { max-height: calc(100vh - 1rem); }
            .symplify-scrollable-modal .modal-content { max-height: calc(100vh - 1rem); overflow: hidden; }
            .symplify-scrollable-modal .modal-content > form,
            .symplify-scrollable-modal .symplify-scrollable-modal-form { display: flex; flex-direction: column; max-height: calc(100vh - 1rem); min-height: 0; }
            .symplify-scrollable-modal .modal-header,
            .symplify-scrollable-modal .modal-footer { flex-shrink: 0; }
            .symplify-scrollable-modal .modal-body { flex: 1 1 auto; min-height: 0; overflow-y: auto; overscroll-behavior: contain; }
            @media (min-width: 576px) {
                .symplify-scrollable-modal .modal-dialog,
                .symplify-scrollable-modal .modal-content,
                .symplify-scrollable-modal .modal-content > form,
                .symplify-scrollable-modal .symplify-scrollable-modal-form { max-height: calc(100vh - 3.5rem); }
            }
        `;
        document.head.appendChild(style);
    }

    function injectReorderStyles() {
        if (document.getElementById('symplify-board-reorder-styles')) return;

        const style = document.createElement('style');
        style.id = 'symplify-board-reorder-styles';
        style.textContent = `
            .lookup-sort-placeholder td,
            .lookup-sort-placeholder { background: rgba(72, 127, 255, .08) !important; border: 1px dashed rgba(72, 127, 255, .45) !important; height: 48px; }
            .lookup-row-dragging { opacity: .75; box-shadow: 0 8px 24px rgba(15, 23, 42, .16); }
            .js-board-member-drag-handle,
            .js-congress-board-drag-handle { user-select: none; cursor: grab; }
        `;
        document.head.appendChild(style);
    }

    function initializeModal($modal) {
        $modal.find('form').each(function () {
            const $form = $(this);
            if (window.Symplify.Forms && typeof window.Symplify.Forms.initialize === 'function') {
                window.Symplify.Forms.initialize($form);
            } else if ($.validator && $.validator.unobtrusive) {
                $form.removeData('validator');
                $form.removeData('unobtrusiveValidation');
                $.validator.unobtrusive.parse($form);
            }
        });

        if (window.Symplify.Dropzone && typeof window.Symplify.Dropzone.initAll === 'function') {
            window.Symplify.Dropzone.initAll($modal);
        }

        initializeAvatarFallbacks($modal);
        initializeSignatureFilePreview($modal);
    }

    function reload(resetPaging) {
        if (table) table.ajax.reload(null, resetPaging === true);
    }

    function resetFilters() {
        $(selectors.boardFilter + ',' + selectors.titleFilter + ',' + selectors.statusFilter).val('');
        reload(true);
    }

    function updateSummary(summary) {
        summary = summary || {};
        $('[data-committee-summary="total"]').text(summary.total || 0);
        $('[data-committee-summary="organizing"]').text(summary.organizing || 0);
        $('[data-committee-summary="scientific"]').text(summary.scientific || 0);
        $('[data-committee-summary="secretariat"]').text(summary.secretariat || 0);
    }

    function renderBoardOrder(value, type, row) {
        if (type !== 'display') return value || 0;
        return renderOrderWithHandle(value, 'js-congress-board-drag-handle');
    }

    function renderMemberOrder(value, type, row) {
        if (type !== 'display') return value || 0;
        return renderOrderWithHandle(value, 'js-board-member-drag-handle');
    }

    function renderOrderWithHandle(value, handleClass) {
        const orderText = value || '-';

        return '<span class="d-inline-flex align-items-center gap-2">' +
            '<i class="ri-draggable text-neutral-500 ' + handleClass + '" aria-hidden="true"></i>' +
            '<span class="fw-medium text-secondary-light js-order-value">' + escapeHtml(orderText) + '</span>' +
        '</span>';
    }

    function renderBoardName(value, type, row) {
        if (type !== 'display') return value || '';
        return '<span class="fw-medium text-secondary-light">' + escapeHtml(value || '-') + '</span>' +
            (row && row.isFallback ? ' <span class="badge bg-warning-light text-warning rounded-pill ms-1">fallback</span>' : '');
    }

    function initializeSignatureFilePreview($modal) {
        if (!$modal || !$modal.length) {
            return;
        }

        const $input = $modal.find('[data-signature-file-input]');
        const $section = $modal.find('[data-signature-preview-section]');
        const $preview = $modal.find('[data-signature-preview]');
        const $label = $modal.find('[data-signature-preview-label]');

        if (!$input.length || !$section.length || !$preview.length) {
            return;
        }

        let objectUrl = null;

        const revokeObjectUrl = function () {
            if (!objectUrl) {
                return;
            }

            URL.revokeObjectURL(objectUrl);
            objectUrl = null;
        };

        $input.off('change.boardMemberSignaturePreview')
            .on('change.boardMemberSignaturePreview', function () {
                revokeObjectUrl();

                const file = this.files && this.files.length
                    ? this.files[0]
                    : null;

                if (!file) {
                    return;
                }

                objectUrl = URL.createObjectURL(file);

                $preview
                    .attr('src', objectUrl)
                    .removeClass('d-none');

                $section.removeClass('d-none');

                if ($label.length) {
                    $label.text(
                        text(
                            'signatureNewPreview',
                            'Yeni imza önizlemesi'
                        )
                    );
                }
            });

        $modal.one('hidden.bs.modal.boardMemberSignaturePreview', function () {
            revokeObjectUrl();
        });
    }

    function renderFullName(value, type, row) {
        if (type !== 'display') {
            return value || '';
        }

        const fullName = value || '-';
        const initials = getInitials(fullName);
        const imageUrl = row && row.imagePreviewUrl
            ? String(row.imagePreviewUrl)
            : '';

        const avatar =
            '<span class="position-relative d-inline-flex w-36-px h-36-px flex-shrink-0">' +
                '<span class="js-board-member-avatar-fallback position-absolute top-0 start-0 w-36-px h-36-px rounded-circle bg-primary-50 text-primary-600 border d-flex align-items-center justify-content-center fw-semibold text-xs">' +
                    escapeHtml(initials) +
                '</span>' +
                (imageUrl
                    ? '<img src="' + escapeHtml(imageUrl) + '" alt="" loading="lazy" class="js-board-member-avatar-image position-absolute top-0 start-0 w-36-px h-36-px rounded-circle object-fit-cover border" />'
                    : '') +
            '</span>';

        return '<div class="d-flex align-items-center gap-2">' +
            avatar +
            '<span class="fw-medium text-secondary-light">' +
                escapeHtml(fullName) +
            '</span>' +
        '</div>';
    }

    function getInitials(fullName) {
        const parts = String(fullName || '')
            .trim()
            .split(/\s+/)
            .filter(Boolean);

        if (!parts.length) {
            return '?';
        }

        const first = parts[0].charAt(0);
        const last = parts.length > 1
            ? parts[parts.length - 1].charAt(0)
            : '';

        try {
            return (first + last).toLocaleUpperCase('tr-TR');
        } catch (_) {
            return (first + last).toUpperCase();
        }
    }

    function initializeAvatarFallbacks($root) {
        if (!$root || !$root.length) {
            return;
        }

        $root.find('.js-board-member-avatar-image').each(function () {
            const image = this;
            const $image = $(image);

            if ($image.data('avatar-fallback-bound') === true) {
                return;
            }

            $image.data('avatar-fallback-bound', true);

            const hideBrokenImage = function () {
                image.classList.add('d-none');
            };

            image.addEventListener('error', hideBrokenImage, { once: true });

            if (image.complete && image.naturalWidth === 0) {
                hideBrokenImage();
            }
        });
    }

    function renderText(value) {
        return value ? escapeHtml(value) : '<span class="text-neutral-400">-</span>';
    }

    function renderStatus(value, type) {
        if (type !== 'display') return value === true ? 1 : 0;
        return value === true
            ? '<span class="badge bg-success-light text-success rounded-pill">' + escapeHtml(text('active', 'Aktif')) + '</span>'
            : '<span class="badge bg-neutral-200 text-neutral-700 rounded-pill">' + escapeHtml(text('passive', 'Pasif')) + '</span>';
    }

    function renderSignatureAuthority(row) {
        if (!row || row.isAcceptanceLetterSigner !== true) {
            return '<span class="text-neutral-400">-</span>';
        }

        const signatureState = row.hasSignature === true
            ? ''
            : ' <i class="ri-error-warning-line text-warning-600" title="' + escapeHtml(text('BackOffice.CongressBoardMembers.Signature.Missing', 'İmza görseli eksik')) + '"></i>';

        return '<span class="badge bg-primary-50 text-primary-600 rounded-pill">' + escapeHtml(text('BackOffice.CongressBoardMembers.Signature.Authorized', 'Yetkili')) + '</span>' + signatureState;
    }

    function renderActions(row) {
        const id = row && row.id ? row.id : '';
        return '<div class="d-flex align-items-center justify-content-end gap-2">' +
            '<button type="button" aria-label="' + escapeHtml(text('signatureSettings', 'İmza ayarları')) + '" title="' + escapeHtml(text('signatureSettings', 'İmza ayarları')) + '" class="btn btn-warning-100 text-warning-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-signature-board-member" data-id="' + escapeHtml(id) + '"><i class="ri-pen-nib-line"></i></button>' +
            '<button type="button" aria-label="' + escapeHtml(text('edit', 'Düzenle')) + '" class="btn btn-primary-100 text-primary-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-edit-board-member" data-id="' + escapeHtml(id) + '"><i class="ri-edit-line"></i></button>' +
            '<button type="button" aria-label="' + escapeHtml(text('delete', 'Sil')) + '" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-delete-board-member" data-id="' + escapeHtml(id) + '"><i class="ri-delete-bin-line"></i></button>' +
            '</div>';
    }

    function renderBoardActions(data, type, row) {
        const id = row && row.id ? row.id : '';
        return '<div class="d-flex align-items-center justify-content-end gap-2">' +
            '<button type="button" aria-label="' + escapeHtml(text('edit', 'Düzenle')) + '" class="btn btn-primary-100 text-primary-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-edit-congress-board" data-id="' + escapeHtml(id) + '"><i class="ri-edit-line"></i></button>' +
            '<button type="button" aria-label="' + escapeHtml(text('delete', 'Sil')) + '" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-delete-congress-board" data-id="' + escapeHtml(id) + '"><i class="ri-delete-bin-line"></i></button>' +
            '</div>';
    }

    function renderExcelResult($container, response, success) {
        const cssClass = success ? 'alert-success' : 'alert-warning';
        let html = '<div class="alert ' + cssClass + ' mb-0">';
        html += '<div class="fw-semibold mb-1">' + escapeHtml(response && response.message ? response.message : text('excelResult', 'Excel sonucu')) + '</div>';
        if (response) html += '<div>' + escapeHtml(text('imported', 'Aktarılan')) + ': ' + escapeHtml(response.importedCount || 0) + ' / ' + escapeHtml(text('skipped', 'Atlanan')) + ': ' + escapeHtml(response.skippedCount || 0) + '</div>';
        if (response && response.errors && response.errors.length) {
            html += '<ul class="mb-0 mt-2">';
            response.errors.forEach(function (error) { html += '<li>' + escapeHtml(error) + '</li>'; });
            html += '</ul>';
        }
        html += '</div>';
        $container.removeClass('d-none').html(html);
    }

    function postForm($form) {
        const formData = new FormData($form[0]);
        return $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: buildAjaxHeaders($form)
        });
    }

    function hideModal($modal) {
        if ($modal && $modal.length) bootstrap.Modal.getOrCreateInstance($modal[0]).hide();
    }

    function cleanupModalArtifacts() {
        if ($('.modal.show').length) return;
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css({ overflow: '', paddingRight: '' });
    }

    function buildAjaxHeaders($source) {
        const token = $source.find('input[name="__RequestVerificationToken"]').first().val() || $('input[name="__RequestVerificationToken"]').first().val();
        return token ? { RequestVerificationToken: token } : {};
    }

    function getDataTableLanguage() {
        if (window.Symplify && window.Symplify.DataTables && typeof window.Symplify.DataTables.getLanguage === 'function') {
            return window.Symplify.DataTables.getLanguage();
        }

        return {
            search: 'Ara:',
            lengthMenu: '_MENU_ kayıt göster',
            info: '_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor',
            infoEmpty: 'Kayıt bulunamadı',
            zeroRecords: 'Eşleşen kayıt bulunamadı',
            paginate: { first: 'İlk', last: 'Son', next: 'Sonraki', previous: 'Önceki' }
        };
    }

    function renderValidationErrors($form, payload) {
        const response = payload && payload.responseJSON ? payload.responseJSON : payload;
        if (!response || !response.errors) return false;

        $form.find('[data-valmsg-for]').empty();
        Object.keys(response.errors).forEach(function (key) {
            const messages = response.errors[key];
            let $message = $form.find('[data-valmsg-for="' + key + '"]');
            if (!$message.length && key === 'Translations') $message = $form.find('[data-valmsg-for^="Translations"]').first();
            if ($message.length) $message.text(messages && messages.length ? messages[0] : '');
        });

        focusFirstInvalidField($form);
        return true;
    }

    function showSuccess(message) {
        if (window.Swal) Swal.fire({ icon: 'success', title: message, timer: 1600, showConfirmButton: false });
        else alert(message);
    }

    function showError(error) {
        const message = extractMessage(error);
        if (window.Swal) Swal.fire({ icon: 'error', title: text('errorTitle', 'Hata'), text: message });
        else alert(message);
    }

    function confirmAction(options) {
        if (window.Swal) {
            return Swal.fire({ icon: 'warning', title: options.title, text: options.text, showCancelButton: true, confirmButtonText: options.confirmButtonText, cancelButtonText: text('cancel', 'Vazgeç') });
        }
        return Promise.resolve({ isConfirmed: window.confirm(options.text) });
    }

    function extractMessage(error) {
        if (!error) return text('genericError', 'İşlem sırasında bir sorun oluştu.');
        if (typeof error === 'string') return error;
        if (error.responseJSON && error.responseJSON.message) return error.responseJSON.message;
        if (error.message) return error.message;
        if (error.statusText) return error.statusText;
        return text('genericError', 'İşlem sırasında bir sorun oluştu.');
    }

    function setBusy($form, isBusy) {
        $form.find('button[type="submit"]').prop('disabled', isBusy);
    }

    function hasJQueryValidation() {
        return !!($.validator && $.validator.unobtrusive);
    }

    function focusFirstInvalidField($form) {
        const $invalid = $form.find('.input-validation-error, .is-invalid').first();
        if ($invalid.length) $invalid.trigger('focus');
    }

    function text(key, fallback) {
        if (window.Symplify) {
            if (window.Symplify.texts && window.Symplify.texts[key]) return window.Symplify.texts[key];
            if (window.Symplify.resources && window.Symplify.resources[key] && window.Symplify.resources[key] !== key) return window.Symplify.resources[key];
            if (window.Symplify.Resources && window.Symplify.Resources[key] && window.Symplify.Resources[key] !== key) return window.Symplify.Resources[key];
        }

        return fallback;
    }

    function escapeHtml(value) {
        return String(value === null || value === undefined ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    return { init: init, reload: reload, loadBoards: loadBoards };
})(jQuery);

$(function () {
    window.Symplify.CongressBoardMembers.Index.init();
});
