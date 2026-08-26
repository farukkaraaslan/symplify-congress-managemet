window.Symplify = window.Symplify || {};
window.Symplify.Submissions = window.Symplify.Submissions || {};

window.Symplify.Submissions.Table = (function ($) {
    'use strict';

    const tables = {};
    let layoutEventsBound = false;
    let resizeTimer = null;

    function init() {
        $('.js-submissions-data-table').each(function () {
            initializeTable($(this));
        });
    }

    function initializeTable($table) {
        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        const mode = ($table.data('mode') || 'author').toString();
        const tableId = $table.attr('id') || mode;
        const isManagement = mode === 'management';

        if ($.fn.DataTable.isDataTable($table)) {
            tables[tableId] = $table.DataTable();
            bindManagementFilters($table, tableId);
            return;
        }

        tables[tableId] = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            ordering: isManagement,
            paging: true,
            pageLength: 10,
            autoWidth: false,
            responsive: false,
            scrollX: isManagement,
            scrollCollapse: isManagement,
            order: isManagement ? [[9, 'desc']] : [],
            language: getDataTableLanguage(),
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders(),
                data: function (data) {
                    if (mode === 'management') {
                        return $.extend({}, data, collectManagementFilters($table));
                    }

                    return data;
                },
                dataSrc: function (json) {
                    if (mode === 'management') {
                        updateManagementStats($table, json && json.stats ? json.stats : null);
                    }

                    return json && Array.isArray(json.data) ? json.data : [];
                },
                error: showError
            },
            columns: isManagement
                ? getManagementColumns($table)
                : getAuthorColumns($table),
            initComplete: function () {
                if (isManagement) {
                    scheduleTableAdjust(tableId);
                }
            },
            drawCallback: function () {
                if (isManagement) {
                    normalizeManagementScrollContainer($table);
                }
            }
        });

        bindManagementFilters($table, tableId);
        bindLayoutAdjustmentEvents();
    }

    function getAuthorColumns($table) {
        return [
            {
                data: null,
                name: 'actions',
                orderable: false,
                searchable: false,
                className: 'text-nowrap',
                render: function (data, type, row) {
                    return renderAuthorActions(row, $table);
                }
            },
            {
                data: 'rowNumber',
                name: 'rowNumber',
                orderable: false,
                searchable: false,
                className: 'text-nowrap fw-medium'
            },
            {
                data: null,
                name: 'submission',
                orderable: false,
                searchable: true,
                render: renderAuthorSubmission
            },
            {
                data: 'topicName',
                name: 'topic',
                orderable: false,
                searchable: true,
                render: renderText
            },
            {
                data: null,
                name: 'authors',
                orderable: true,
                searchable: true,
                render: function (data, type, row) {
                    return renderAuthorBlock(row, $table.data('author-count-format'));
                }
            },
            {
                data: null,
                name: 'payment',
                orderable: true,
                searchable: false,
                className: 'text-nowrap align-middle',
                width: '118px',
                render: renderPaymentBadge
            },
            {
                data: null,
                name: 'status',
                orderable: true,
                searchable: false,
                className: 'text-nowrap align-middle',
                width: '150px',
                render: renderStatusBadge
            },
            {
                data: null,
                name: 'submittedAt',
                orderable: false,
                searchable: false,
                render: renderDate
            }
        ];
    }

    function getManagementColumns($table) {
        return [
            {
                data: null,
                name: 'actions',
                orderable: false,
                searchable: false,
                className: 'text-nowrap align-middle',
                width: '92px',
                render: function (data, type, row) {
                    return renderManagementAction(row, $table);
                }
            },
            {
                data: 'rowNumber',
                name: 'rowNumber',
                orderable: false,
                searchable: false,
                className: 'text-nowrap fw-medium align-middle',
                width: '58px'
            },
            {
                data: null,
                name: 'submissionNumber',
                orderable: true,
                searchable: true,
                className: 'text-nowrap align-middle',
                width: '105px',
                render: renderSubmissionNumber
            },
            {
                data: null,
                name: 'title',
                orderable: true,
                searchable: true,
                className: 'align-top',
                width: '230px',
                render: renderSubmissionTitle
            },
            {
                data: null,
                name: 'congress',
                orderable: true,
                searchable: true,
                className: 'align-top',
                width: '190px',
                render: renderManagementCongress
            },
            {
                data: null,
                name: 'typeTopic',
                orderable: true,
                searchable: true,
                className: 'align-top',
                width: '145px',
                render: renderTypeTopic
            },
            {
                data: null,
                name: 'owner',
                orderable: true,
                searchable: true,
                className: 'align-top',
                width: '190px',
                render: function (data, type, row) {
                    return renderManagementOwnerBlock(row, $table);
                }
            },
            {
                data: null,
                name: 'payment',
                orderable: true,
                searchable: false,
                className: 'text-nowrap align-middle',
                width: '118px',
                render: renderPaymentBadge
            },
            {
                data: null,
                name: 'status',
                orderable: true,
                searchable: false,
                className: 'text-nowrap align-middle',
                width: '150px',
                render: renderStatusBadge
            },
            {
                data: null,
                name: 'submittedAt',
                orderable: true,
                searchable: false,
                className: 'text-nowrap align-middle',
                width: '138px',
                render: renderDate
            }
        ];
    }

    function renderAuthorSubmission(data, type, row) {
        const title = row.title || '-';
        const number = row.submissionNumber || '';
        const typeName = row.submissionTypeName || '-';

        return '' +
            '<div>' +
                '<span class="fw-semibold text-primary-light d-block">' + escapeHtml(title) + '</span>' +
                '<div class="d-flex align-items-center gap-2 mt-1 flex-wrap">' +
                    '<span class="badge bg-primary-50 text-primary-600 rounded-pill">' + escapeHtml(typeName) + '</span>' +
                    (number ? '<small class="text-neutral-500">' + escapeHtml(number) + '</small>' : '') +
                '</div>' +
            '</div>';
    }

    function renderSubmissionNumber(data, type, row) {
        const number = row.submissionNumber || row.id || '-';
        return '<span class="badge bg-primary-50 text-primary-600 rounded-pill px-10 py-6">' + escapeHtml(number) + '</span>';
    }

    function renderSubmissionTitle(data, type, row) {
        const fullTitle = row.title || '-';
        const title = truncateText(fullTitle, 95);

        return '<span class="text-xs fw-semibold text-primary-light d-block lh-sm" title="' + escapeHtml(fullTitle) + '">' + escapeHtml(title) + '</span>';
    }

    function renderManagementSubmission(row, $table) {
        const number = row.submissionNumber || row.id || '-';
        const title = truncateText(row.title || '-', 115);
        const orcidLabel = $table.data('orcid-label') || 'ORCID';
        const orcid = normalizeEmpty(row.orcid);

        return '' +
            '<div>' +
                '<div class="d-flex align-items-center gap-2 flex-wrap mb-1">' +
                    '<span class="badge bg-primary-50 text-primary-600 rounded-pill px-10 py-6">' + escapeHtml(number) + '</span>' +
                '</div>' +
                '<span class="fw-semibold text-primary-light d-block lh-sm">' + escapeHtml(title) + '</span>' +
                (orcid ? '<small class="text-neutral-500 d-block mt-1">' + escapeHtml(orcidLabel) + ': ' + escapeHtml(orcid) + '</small>' : '') +
            '</div>';
    }

    function renderManagementCongress(data, type, row) {
        const congressName = row.congressName || '-';

        return '<span class="text-primary-light fw-medium d-block lh-sm" title="' + escapeHtml(congressName) + '">' + escapeHtml(congressName) + '</span>';
    }

    function renderTypeTopic(data, type, row) {
        const typeName = row.submissionTypeName || '-';
        const topicName = row.topicName || '-';

        return '' +
            '<div class="d-flex flex-column align-items-start gap-1">' +
                '<span class="badge bg-primary-50 text-primary-600 rounded-pill px-10 py-6 d-inline-block">' + escapeHtml(typeName) + '</span>' +
                '<span class="badge bg-info-focus text-info-main rounded-pill px-10 py-6 d-inline-block">' + escapeHtml(topicName) + '</span>' +
            '</div>';
    }

    function renderAuthorBlock(row, authorCountFormat) {
        const author = row.correspondingAuthorName || '-';
        const otherAuthors = row.otherAuthorsText || '';
        const countText = formatAuthorCount(authorCountFormat, row.authorCount || 0);

        return '' +
            '<div>' +
                '<div class="d-flex align-items-center gap-2">' +
                    '<i class="ri-circle-fill text-success text-xs"></i>' +
                    '<span class="fw-semibold text-primary-light">' + escapeHtml(author) + '</span>' +
                '</div>' +
                '<div class="d-flex align-items-center gap-2 mt-1 flex-wrap">' +
                    (otherAuthors ? '<span class="text-sm text-neutral-500">' + escapeHtml(otherAuthors) + '</span><span class="text-xs text-neutral-400">•</span>' : '') +
                    '<span class="text-xs text-neutral-500">' + escapeHtml(countText) + '</span>' +
                '</div>' +
            '</div>';
    }

    function renderManagementOwnerBlock(row, $table) {
        const ownerName = normalizeEmpty(row.submissionOwnerName)
            || normalizeEmpty(row.submissionOwnerEmail)
            || normalizeEmpty($table.data('owner-missing'))
            || '-';
        const ownerEmail = normalizeEmpty(row.submissionOwnerEmail);
        const count = parseInt(row.ownerSubmissionCount || 0, 10);
        const normalizedCount = Number.isNaN(count) ? 0 : Math.max(count, 0);
        const countText = formatOwnerSubmissionCount(
            $table.data('owner-submission-count-format'),
            normalizedCount);
        const isMultiple = row.hasMultipleSubmissions === true || normalizedCount > 1;
        const badgeClass = isMultiple
            ? 'bg-warning-focus text-warning-main'
            : 'bg-neutral-100 text-neutral-600';
        const badgeTitle = isMultiple
            ? normalizeEmpty($table.data('owner-multiple-title'))
            : '';
        const authorCount = parseInt(row.authorCount || 0, 10);
        const additionalAuthorCount = Number.isNaN(authorCount)
            ? 0
            : Math.max(authorCount - 1, 0);
        const additionalAuthorText = additionalAuthorCount > 0
            ? formatAdditionalAuthorCount(
                $table.data('additional-author-count-format'),
                additionalAuthorCount)
            : '';

        return '' +
            '<div class="d-inline-block mw-100">' +
                '<span class="fw-semibold text-primary-light d-block" title="' + escapeHtml(ownerName) + '">' + escapeHtml(ownerName) + '</span>' +
                (ownerEmail && ownerEmail.toLowerCase() !== ownerName.toLowerCase()
                    ? '<small class="text-neutral-500 d-block mt-1 text-break">' + escapeHtml(ownerEmail) + '</small>'
                    : '') +
                '<div class="d-flex align-items-center gap-1 flex-wrap mt-1">' +
                    (normalizedCount > 0
                        ? '<span class="badge ' + badgeClass + ' rounded-pill px-10 py-6 text-xs" title="' + escapeHtml(badgeTitle) + '">' +
                            '<i class="ri-file-copy-2-line me-1"></i>' + escapeHtml(countText) +
                          '</span>'
                        : '') +
                    (additionalAuthorText
                        ? '<span class="text-xs text-neutral-500 text-nowrap">' + escapeHtml(additionalAuthorText) + '</span>'
                        : '') +
                '</div>' +
            '</div>';
    }

    function bindManagementFilters($table, tableId) {
        if (($table.data('mode') || '').toString() !== 'management') {
            return;
        }

        const containerSelector = $table.data('filters-container') || '#submissionManagementFilters';
        const $container = $(containerSelector);

        if (!$container.length || $container.data('submission-management-filters-bound') === true) {
            return;
        }

        $container.data('submission-management-filters-bound', true);

        $container.on('keydown.submissionManagementFilters', 'input.js-submission-management-filter', function (event) {
            if (event.key === 'Enter') {
                event.preventDefault();
            }
        });

        $container.on('click.submissionManagementFilters', '#submissionManagementApplyFilters', function () {
            reloadTable(tableId, true);
        });

        $container.on('click.submissionManagementFilters', '#submissionManagementResetFilters', function () {
            $container.find('input.js-submission-management-filter').val('');
            $container.find('select.js-submission-management-filter').val('');
            $container.find('[name="ownerMultiplicity"]').val('0');

            if (tables[tableId]) {
                tables[tableId].search('');
            }

            reloadTable(tableId, true);
        });
    }

    function reloadTable(tableId, resetPaging) {
        if (tables[tableId]) {
            tables[tableId].ajax.reload(null, resetPaging === true);
        }
    }

    function collectManagementFilters($table) {
        const containerSelector = $table.data('filters-container') || '#submissionManagementFilters';
        const $container = $(containerSelector);

        if (!$container.length) {
            return {};
        }

        return {
            searchText: ($container.find('[name="searchText"]').val() || '').toString(),
            congressId: ($container.find('[name="congressId"]').val() || '').toString(),
            transactionStatusId: ($container.find('[name="transactionStatusId"]').val() || '').toString(),
            paymentStatusId: ($container.find('[name="paymentStatusId"]').val() || '').toString(),
            topicId: ($container.find('[name="topicId"]').val() || '').toString(),
            submissionTypeId: ($container.find('[name="submissionTypeId"]').val() || '').toString(),
            ownerMultiplicity: ($container.find('[name="ownerMultiplicity"]').val() || '0').toString(),
            archiveMode: ($container.find('[name="archiveMode"]').val() || 'false').toString()
        };
    }

    function updateManagementStats($table, stats) {
        const containerSelector = $table.data('stats-container') || '#submissionManagementStats';
        const $container = $(containerSelector);

        if (!$container.length || !stats) {
            return;
        }

        const keys = ['total', 'submitted', 'reviewerProcess', 'accepted', 'rejected', 'paymentPending', 'paymentCompleted'];

        keys.forEach(function (key) {
            const value = Object.prototype.hasOwnProperty.call(stats, key) ? stats[key] : 0;
            $container.find('[data-stat-count="' + key + '"]').text(value == null ? '0' : value.toString());
        });

        $('[data-submission-list-count]').text(stats.total == null ? '0' : stats.total.toString());
    }

    function renderPaymentBadge(data, type, row) {
        return '<span class="badge ' + escapeHtml(row.paymentStatusBadgeClass || 'bg-neutral-200 text-neutral-700') + ' rounded-pill px-10 py-6 text-xs">' + escapeHtml(row.paymentStatusName || '-') + '</span>';
    }

    function renderStatusBadge(data, type, row) {
        const badgeClass = resolveTransactionBadgeClass(row);
        return '<span class="badge ' + escapeHtml(badgeClass) + ' rounded-pill px-10 py-6 text-xs">' + escapeHtml(row.transactionStatusName || '-') + '</span>';
    }

    function renderDate(data, type, row) {
        return '' +
            '<span class="fw-medium d-block text-nowrap"><i class="ri-calendar-line text-primary-600 me-1"></i>' + escapeHtml(row.displayDate || '-') + '</span>' +
            '<small class="text-neutral-500 text-nowrap"><i class="ri-time-line me-1"></i>' + escapeHtml(row.displayTime || '-') + '</small>';
    }

    function resolveTransactionBadgeClass(row) {
        const code = normalizeCode(row.transactionStatusCode || row.transactionStatusName);

        if (code.indexOf('REJECT') >= 0 || code.indexOf('REDDED') >= 0 || code.indexOf('RED') >= 0) {
            return 'bg-danger-100 text-danger-600';
        }

        if (code.indexOf('ACCEPT') >= 0 || code.indexOf('KABUL') >= 0 || code.indexOf('COMPLETED') >= 0 || code.indexOf('TAMAMLANDI') >= 0) {
            return 'bg-success-100 text-success-600';
        }

        if (code.indexOf('REVIEW') >= 0 || code.indexOf('HAKEM') >= 0 || code.indexOf('DEGERLENDIR') >= 0) {
            return 'bg-info-100 text-info-600';
        }

        if (code.indexOf('SUBMITTED') >= 0 || code.indexOf('GONDERILDI') >= 0) {
            return 'bg-warning-100 text-warning-600';
        }

        return row.transactionStatusBadgeClass || 'bg-neutral-200 text-neutral-700';
    }

    function normalizeCode(value) {
        if (!value) {
            return '';
        }

        return value.toString().trim().toUpperCase()
            .replace(/İ/g, 'I')
            .replace(/Ö/g, 'O')
            .replace(/Ü/g, 'U')
            .replace(/Ş/g, 'S')
            .replace(/Ğ/g, 'G')
            .replace(/Ç/g, 'C')
            .replace(/[^A-Z0-9]/g, '');
    }

    function normalizeEmpty(value) {
        if (value === null || value === undefined) {
            return '';
        }

        const text = value.toString().trim();
        return text === '-' ? '' : text;
    }

    function truncateText(value, maxLength) {
        const text = value === null || value === undefined ? '' : value.toString();
        const limit = maxLength || 80;

        if (text.length <= limit) {
            return text;
        }

        return text.substring(0, limit - 1).trimEnd() + '…';
    }

    function renderAuthorActions(row, $table) {
        const detailsUrl = buildUrl($table.data('details-url-template'), row.id);
        const editUrl = buildUrl($table.data('edit-url-template'), row.id);
        const deleteUrl = $table.data('delete-url') || '';
        const texts = window.Symplify.texts || window.Symplify.Texts || {};
        const viewText = window.Symplify.t ? window.Symplify.t('Common.View', 'Görüntüle') : 'Görüntüle';
        const editText = texts.edit || 'Düzenle';
        const deleteText = texts.delete || 'Sil';
        const token = getAntiForgeryToken();

        let html = '<div class="d-flex align-items-center gap-2">' +
            '<a class="btn btn-primary-100 text-primary-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center" href="' + escapeHtml(detailsUrl) + '" title="' + escapeHtml(viewText) + '">' +
                '<i class="ri-eye-line"></i>' +
            '</a>';

        if (row.canEdit) {
            html += '<a class="btn btn-info-100 text-info-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center" href="' + escapeHtml(editUrl) + '" title="' + escapeHtml(editText) + '">' +
                '<i class="ri-edit-line"></i>' +
            '</a>';
        }

        if (row.canDelete && deleteUrl) {
            html += '<form method="post" action="' + escapeHtml(deleteUrl) + '" class="d-inline js-confirm-delete"' +
                ' data-confirm-title="' + escapeHtml($table.data('delete-confirm-title') || '') + '"' +
                ' data-confirm-text="' + escapeHtml($table.data('delete-confirm-text') || '') + '"' +
                ' data-confirm-button="' + escapeHtml($table.data('delete-confirm-button') || '') + '">' +
                '<input name="__RequestVerificationToken" type="hidden" value="' + escapeHtml(token) + '" />' +
                '<input type="hidden" name="Id" value="' + escapeHtml(row.id || '') + '" />' +
                '<input type="hidden" name="CongressId" value="' + escapeHtml(row.congressId || '') + '" />' +
                '<button type="submit" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center" title="' + escapeHtml(deleteText) + '">' +
                    '<i class="ri-delete-bin-line"></i>' +
                '</button>' +
            '</form>';
        }

        html += '</div>';
        return html;
    }

    function renderManagementAction(row, $table) {
        const manageUrl = buildUrl($table.data('manage-url-template'), row.id);
        const editUrl = buildUrl($table.data('edit-url-template'), row.id);
        const deleteUrl = $table.data('delete-url') || '';
        const actionsText = $table.data('actions-text') || 'İşlem';
        const manageText = $table.data('manage-text') || $table.data('detail-text') || 'Yönet';
        const editText = $table.data('edit-text') || 'Düzenle';
        const deleteText = $table.data('delete-text') || 'Sil';
        const token = getAntiForgeryToken();
        const returnUrl = window.location.pathname + window.location.search;

        let html = '' +
            '<div class="dropdown">' +
                '<button class="btn btn-sm btn-outline-primary-600 radius-8 dropdown-toggle d-inline-flex align-items-center gap-1" type="button" data-bs-toggle="dropdown" aria-expanded="false">' +
                    escapeHtml(actionsText) +
                '</button>' +
                '<ul class="dropdown-menu shadow-2 bg-base border border-neutral-200 radius-12 py-2">' +
                    '<li>' +
                        '<a class="dropdown-item d-flex align-items-center gap-2" href="' + escapeHtml(manageUrl) + '">' +
                            '<i class="ri-settings-3-line"></i>' +
                            '<span>' + escapeHtml(manageText) + '</span>' +
                        '</a>' +
                    '</li>';

        if (row.canEdit && editUrl) {
            html += '' +
                '<li>' +
                    '<a class="dropdown-item d-flex align-items-center gap-2" href="' + escapeHtml(editUrl) + '">' +
                        '<i class="ri-edit-line"></i>' +
                        '<span>' + escapeHtml(editText) + '</span>' +
                    '</a>' +
                '</li>';
        }

        if (row.canDelete && deleteUrl) {
            html += '' +
                '<li><hr class="dropdown-divider"></li>' +
                '<li>' +
                    '<form method="post" action="' + escapeHtml(deleteUrl) + '" class="m-0 js-confirm-delete"' +
                        ' data-confirm-title="' + escapeHtml($table.data('delete-confirm-title') || '') + '"' +
                        ' data-confirm-text="' + escapeHtml($table.data('delete-confirm-text') || '') + '"' +
                        ' data-confirm-button="' + escapeHtml($table.data('delete-confirm-button') || '') + '">' +
                        '<input name="__RequestVerificationToken" type="hidden" value="' + escapeHtml(token) + '" />' +
                        '<input type="hidden" name="Id" value="' + escapeHtml(row.id || '') + '" />' +
                        '<input type="hidden" name="CongressId" value="' + escapeHtml(row.congressId || '') + '" />' +
                        '<input type="hidden" name="returnUrl" value="' + escapeHtml(returnUrl) + '" />' +
                        '<button type="submit" class="dropdown-item text-danger d-flex align-items-center gap-2" title="' + escapeHtml(deleteText) + '">' +
                            '<i class="ri-delete-bin-line"></i>' +
                            '<span>' + escapeHtml(deleteText) + '</span>' +
                        '</button>' +
                    '</form>' +
                '</li>';
        }

        html += '</ul></div>';
        return html;
    }

    function normalizeManagementScrollContainer($table) {
        const $wrapper = $table.closest('.dataTables_wrapper');
        if (!$wrapper.length) {
            return;
        }

        $wrapper.css('max-width', '100%');
        $wrapper.find('.dataTables_scroll').css({
            width: '100%',
            maxWidth: '100%'
        });
        $wrapper.find('.dataTables_scrollBody').css({
            overflowX: 'auto',
            overflowY: 'hidden',
            width: '100%',
            maxWidth: '100%'
        });
    }

    function scheduleTableAdjust(tableId) {
        window.setTimeout(function () {
            const table = tables[tableId];
            if (!table) {
                return;
            }

            table.columns.adjust();
            const node = table.table().node();
            if (node) {
                normalizeManagementScrollContainer($(node));
            }
        }, 80);
    }

    function adjustAllTables() {
        Object.keys(tables).forEach(function (tableId) {
            scheduleTableAdjust(tableId);
        });
    }

    function bindLayoutAdjustmentEvents() {
        if (layoutEventsBound) {
            return;
        }

        layoutEventsBound = true;

        $(window).on('resize.submissionManagementTable', function () {
            if (resizeTimer) {
                window.clearTimeout(resizeTimer);
            }

            resizeTimer = window.setTimeout(adjustAllTables, 150);
        });

        $(document).on(
            'click.submissionManagementTable',
            '.sidebar-toggle, .sidebar-mobile-toggle, .sidebar-close-btn',
            function () {
                window.setTimeout(adjustAllTables, 350);
            });

        document.addEventListener('layout:loaded', function () {
            window.setTimeout(adjustAllTables, 100);
        });
    }

    function getAjaxHeaders() {
        const token = getAntiForgeryToken();
        return token ? { RequestVerificationToken: token } : {};
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val() || '';
    }

    function getDataTableLanguage() {
        if (window.Symplify.DataTables && typeof window.Symplify.DataTables.getLanguage === 'function') {
            return window.Symplify.DataTables.getLanguage();
        }

        return window.Symplify.DataTables?.language || window.Symplify.dataTables?.language || {};
    }

    function showError() {
        const message = window.Symplify.t
            ? window.Symplify.t('Common.GenericError', 'İşlem sırasında bir hata oluştu.')
            : 'İşlem sırasında bir hata oluştu.';

        if (window.Swal) {
            window.Swal.fire({ icon: 'error', text: message });
            return;
        }

        console.error(message);
    }

    function buildUrl(template, id) {
        const value = template || '';
        return value.toString().replace('__id__', encodeURIComponent(id || ''));
    }

    function withHash(url, hash) {
        const cleanUrl = (url || '').toString().split('#')[0];
        return cleanUrl + '#' + encodeURIComponent(hash || 'summaryTab');
    }

    function formatAuthorCount(format, count) {
        const template = format || '{0} yazar';
        return template.replace('{0}', count);
    }

    function formatAdditionalAuthorCount(format, count) {
        const explicitTemplate = normalizeEmpty(format);
        if (explicitTemplate) {
            return explicitTemplate.replace('{0}', count);
        }

        const culture = (document.documentElement.getAttribute('lang') || '').toLowerCase();
        const fallbackTemplate = culture.indexOf('en') === 0
            ? '+ {0} more author(s)'
            : '+ {0} yazar daha';

        return fallbackTemplate.replace('{0}', count);
    }

    function formatOwnerSubmissionCount(format, count) {
        const explicitTemplate = normalizeEmpty(format);
        if (explicitTemplate) {
            return explicitTemplate.replace('{0}', count);
        }

        const culture = (document.documentElement.getAttribute('lang') || '').toLowerCase();
        const fallbackTemplate = culture.indexOf('en') === 0
            ? '{0} submissions'
            : '{0} bildiri';

        return fallbackTemplate.replace('{0}', count);
    }

    function getInitials(fullName) {
        if (!fullName || fullName === '-') {
            return '?';
        }

        const cleanedName = fullName
            .replace(/\b(prof|dr|doç|doc|assoc|asst|öğr|ogr|üyesi|uyesi|arş|ars|gör|gor)\.?/gi, ' ')
            .replace(/\s+/g, ' ')
            .trim();
        const parts = (cleanedName || fullName).split(' ').filter(Boolean);
        if (parts.length === 1) {
            return parts[0].substring(0, 2).toUpperCase();
        }

        return (parts[0][0] + parts[1][0]).toUpperCase();
    }

    function renderText(value) {
        return escapeHtml(value || '-');
    }

    function escapeHtml(value) {
        return $('<div/>').text(value == null ? '' : value.toString()).html();
    }

    return {
        init: init,
        reload: function (tableId, resetPaging) {
            if (tables[tableId]) {
                tables[tableId].ajax.reload(null, resetPaging === true);
            }
        }
    };
})(jQuery);

$(function () {
    if (window.Symplify.Submissions.Table) {
        window.Symplify.Submissions.Table.init();
    }
});
