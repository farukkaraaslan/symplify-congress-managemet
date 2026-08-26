window.Symplify = window.Symplify || {};
window.Symplify.ReviewerEvaluations = window.Symplify.ReviewerEvaluations || {};

window.Symplify.ReviewerEvaluations.Table = (function ($) {
    'use strict';

    const tables = {};

    function init() {
        $('.js-reviewer-evaluations-data-table').each(function () {
            initializeTable($(this));
        });
    }

    function initializeTable($table) {
        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        const tableId = $table.attr('id') || 'reviewerEvaluationsTable';

        if ($.fn.DataTable.isDataTable($table)) {
            tables[tableId] = $table.DataTable();
            bindFilters($table, tableId);
            return;
        }

        tables[tableId] = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            ordering: false,
            paging: true,
            pageLength: 10,
            autoWidth: false,
            responsive: false,
            language: getDataTableLanguage(),
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders(),
                data: function (data) {
                    return $.extend({}, data, collectFilters($table));
                },
                dataSrc: function (json) {
                    updateStats($table, json && json.stats ? json.stats : null);
                    return json && Array.isArray(json.data) ? json.data : [];
                },
                error: showError
            },
            columns: getColumns($table)
        });

        bindFilters($table, tableId);
    }

    function getColumns($table) {
        return [
            {
                data: null,
                name: 'actions',
                orderable: false,
                searchable: false,
                className: 'text-nowrap',
                render: function (data, type, row) {
                    return renderAction(row, $table);
                }
            },
            {
                data: 'submissionNumber',
                name: 'submissionNumber',
                orderable: true,
                searchable: true,
                className: 'fw-medium text-nowrap',
                render: renderText
            },
            {
                data: 'submissionTypeName',
                name: 'type',
                orderable: true,
                searchable: true,
                render: renderText
            },
            {
                data: null,
                name: 'title',
                orderable: true,
                searchable: true,
                render: function (data, type, row) {
                    return renderTitle(row, $table);
                }
            },
            {
                data: 'topicName',
                name: 'topic',
                orderable: true,
                searchable: true,
                render: renderText
            },
            {
                data: 'congressName',
                name: 'congress',
                orderable: true,
                searchable: true,
                render: function (data) {
                    return '<span class="fw-medium d-block min-w-220-px">' + escapeHtml(data || '-') + '</span>';
                }
            },
            {
                data: null,
                name: 'assignedDate',
                orderable: true,
                searchable: false,
                render: renderAssignedDate
            },
            {
                data: null,
                name: 'dueDate',
                orderable: true,
                searchable: false,
                render: function (data, type, row) {
                    return renderDueDate(row, $table);
                }
            },
            {
                data: null,
                name: 'status',
                orderable: true,
                searchable: false,
                render: renderStatusBadge
            },
            {
                data: null,
                name: 'recommendation',
                orderable: true,
                searchable: false,
                render: renderRecommendationBadge
            }
        ];
    }

    function bindFilters($table, tableId) {
        const containerSelector = $table.data('filters-container') || '#reviewerEvaluationFilters';
        const $container = $(containerSelector);

        if (!$container.length || $container.data('reviewer-evaluation-filters-bound') === true) {
            return;
        }

        $container.data('reviewer-evaluation-filters-bound', true);

        let inputFilterTimer = null;

        $container.on('input.reviewerEvaluationFilters', 'input.js-reviewer-evaluation-filter', function () {
            window.clearTimeout(inputFilterTimer);
            inputFilterTimer = window.setTimeout(function () {
                reloadTable(tableId, true);
            }, 400);
        });

        $container.on('keydown.reviewerEvaluationFilters', 'input.js-reviewer-evaluation-filter', function (event) {
            if (event.key === 'Enter') {
                event.preventDefault();
                window.clearTimeout(inputFilterTimer);
                reloadTable(tableId, true);
            }
        });

        $container.on('change.reviewerEvaluationFilters', 'select.js-reviewer-evaluation-filter', function () {
            reloadTable(tableId, true);
        });

        $container.on('click.reviewerEvaluationFilters', '#reviewerEvaluationResetFilters', function () {
            $container.find('input.js-reviewer-evaluation-filter').val('');
            $container.find('select.js-reviewer-evaluation-filter').val('');

            if (tables[tableId]) {
                tables[tableId].search('');
            }

            reloadTable(tableId, true);
        });
    }

    function collectFilters($table) {
        const containerSelector = $table.data('filters-container') || '#reviewerEvaluationFilters';
        const $container = $(containerSelector);

        if (!$container.length) {
            return {};
        }

        return {
            searchText: ($container.find('[name="searchText"]').val() || '').toString(),
            congressId: ($container.find('[name="congressId"]').val() || '').toString(),
            status: ($container.find('[name="status"]').val() || '').toString(),
            topicId: ($container.find('[name="topicId"]').val() || '').toString(),
            submissionTypeId: ($container.find('[name="submissionTypeId"]').val() || '').toString()
        };
    }

    function updateStats($table, stats) {
        const containerSelector = $table.data('stats-container') || '#reviewerEvaluationStats';
        const $container = $(containerSelector);

        if (!$container.length || !stats) {
            return;
        }

        const keys = ['total', 'pending', 'inProgress', 'completed', 'dueSoon'];

        keys.forEach(function (key) {
            const value = Object.prototype.hasOwnProperty.call(stats, key) ? stats[key] : 0;
            $container.find('[data-stat-count="' + key + '"]').text(value == null ? '0' : value.toString());
        });
    }

    function renderAction(row, $table) {
        const url = buildUrl($table.data('evaluate-url-template'), row.evaluationId);
        const text = t(row.actionText, row.isCompleted ? 'Görüntüle' : 'Değerlendir');
        const icon = row.actionIcon || (row.isCompleted ? 'ri-eye-line' : 'ri-edit-line');
        const buttonClass = row.isCompleted ? 'btn-primary-100 text-primary-600' : 'btn-primary-600';

        return '' +
            '<a class="btn ' + buttonClass + ' radius-8 px-14 py-8 d-inline-flex align-items-center gap-2" href="' + escapeHtml(url) + '">' +
                '<i class="' + escapeHtml(icon) + '"></i> ' + escapeHtml(text) +
            '</a>';
    }

    function renderTitle(row, $table) {
        const blindText = $table.data('blind-review-text') || t('BackOffice.ReviewerEvaluations.BlindReview.Short', 'Yazar bilgisi gizlidir');

        return '' +
            '<div class="min-w-260-px">' +
                '<span class="fw-semibold text-primary-light d-block">' + escapeHtml(row.title || '-') + '</span>' +
                '<small class="text-neutral-500">' + escapeHtml(blindText) + '</small>' +
            '</div>';
    }

    function renderAssignedDate(data, type, row) {
        return '' +
            '<span class="fw-medium d-block"><i class="ri-calendar-line text-primary-600 me-1"></i>' + escapeHtml(row.assignedDate || '-') + '</span>' +
            '<small class="text-neutral-500">' + escapeHtml(row.assignedTime || '-') + '</small>';
    }

    function renderDueDate(row, $table) {
        const dueText = resolveDueText(row, $table);
        const textClass = row.isOverdue ? 'text-danger' : 'text-neutral-500';
        const dateClass = row.isOverdue ? 'text-danger' : '';

        return '' +
            '<span class="fw-medium d-block ' + dateClass + '"><i class="ri-alarm-warning-line me-1"></i>' + escapeHtml(row.dueDate || '-') + '</span>' +
            '<small class="' + textClass + '">' + escapeHtml(dueText) + '</small>';
    }

    function resolveDueText(row, $table) {
        if (row.isCompleted) {
            return t('BackOffice.ReviewerEvaluations.Status.Completed', 'Tamamlandı');
        }

        if (row.isOverdue) {
            return $table.data('due-overdue-text') || t('BackOffice.ReviewerEvaluations.Due.Overdue', 'Süre geçti');
        }

        if ((row.daysRemaining || 0) <= 0) {
            return $table.data('due-today-text') || t('BackOffice.ReviewerEvaluations.Due.Today', 'Bugün');
        }

        const template = $table.data('due-remaining-format') || t('BackOffice.ReviewerEvaluations.Due.RemainingFormat', '{0} gün kaldı');
        return template.replace('{0}', row.daysRemaining || 0);
    }

    function renderStatusBadge(data, type, row) {
        return '<span class="badge ' + escapeHtml(row.statusBadgeClass || 'bg-neutral-200 text-neutral-700') + ' rounded-pill px-12 py-8">' + escapeHtml(t(row.statusText, row.statusText || '-')) + '</span>';
    }

    function renderRecommendationBadge(data, type, row) {
        return '<span class="badge bg-neutral-200 text-neutral-700 rounded-pill px-12 py-8">' + escapeHtml(t(row.recommendationText, row.recommendationText || '-')) + '</span>';
    }

    function reloadTable(tableId, resetPaging) {
        if (tables[tableId]) {
            tables[tableId].ajax.reload(null, resetPaging === true);
        }
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
        const message = t('Common.GenericError', 'İşlem sırasında bir hata oluştu.');

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

    function renderText(value) {
        return escapeHtml(value || '-');
    }

    function t(key, fallback) {
        if (window.Symplify && typeof window.Symplify.t === 'function') {
            return window.Symplify.t(key, fallback || key || '');
        }

        return fallback || key || '';
    }

    function escapeHtml(value) {
        return $('<div/>').text(value == null ? '' : value.toString()).html();
    }

    return {
        init: init,
        reload: function (tableId, resetPaging) {
            reloadTable(tableId, resetPaging);
        }
    };
})(jQuery);

$(function () {
    if (window.Symplify.ReviewerEvaluations.Table) {
        window.Symplify.ReviewerEvaluations.Table.init();
    }
});
