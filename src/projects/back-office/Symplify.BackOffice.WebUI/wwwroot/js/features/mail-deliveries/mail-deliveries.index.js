window.Symplify = window.Symplify || {};
window.Symplify.MailDeliveries = window.Symplify.MailDeliveries || {};

window.Symplify.MailDeliveries.Index = (function ($) {
    'use strict';

    const selectors = {
        table: '#mailDeliveriesTable',
        filterForm: '#mailDeliveryFilterForm',
        clearFilters: '#mailDeliveryClearFilters',
        search: '#mailDeliverySearch',
        organizationId: '#mailDeliveryOrganizationId',
        congressId: '#mailDeliveryCongressId',
        mailType: '#mailDeliveryMailType',
        transportStatus: '#mailDeliveryTransportStatus',
        deliveryStatus: '#mailDeliveryDeliveryStatus',
        dateFrom: '#mailDeliveryDateFrom',
        dateTo: '#mailDeliveryDateTo',
        detailButton: '.js-mail-delivery-detail',
        detailModal: '#mailDeliveryDetailModal',
        detailContent: '#mailDeliveryDetailContent'
    };

    const transportStatus = {
        pending: 1,
        sent: 2,
        failed: 3,
        cancelled: 4,
        processing: 5
    };

    const deliveryStatus = {
        unknown: 0,
        notTracked: 1,
        pending: 10,
        delivered: 20,
        delayed: 30,
        bounced: 40,
        rejected: 50,
        complaint: 60,
        renderingFailed: 70
    };

    let table = null;
    let modal = null;
    let initialModalContent = '';
    let resizeTimer = null;
    let layoutEventsBound = false;

    function init() {
        const $table = $(selectors.table);
        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        initializeTable($table);
        initializeModal();
        bindEvents();
        bindLayoutAdjustmentEvents($table);
    }

    function initializeTable($table) {
        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();
            return;
        }

        table = $table.DataTable({
            processing: true,
            serverSide: true,
            responsive: false,
            scrollX: true,
            scrollCollapse: true,
            autoWidth: false,
            searching: false,
            ordering: true,
            paging: true,
            pageLength: 25,
            lengthMenu: [10, 25, 50, 100],
            order: [[1, 'desc']],
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: buildAjaxHeaders(),
                data: function (data) {
                    data.organizationId = valueOf(selectors.organizationId);
                    data.congressId = valueOf(selectors.congressId);
                    data.mailType = valueOf(selectors.mailType);
                    data.status = valueOf(selectors.transportStatus);
                    data.deliveryStatus = valueOf(selectors.deliveryStatus);
                    data.dateFrom = valueOf(selectors.dateFrom);
                    data.dateTo = valueOf(selectors.dateTo);
                    data.search = valueOf(selectors.search);
                    return data;
                },
                error: function (xhr) {
                    if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
                        window.Symplify.Ajax.showError(xhr);
                        return;
                    }

                    console.error('Mail delivery DataTable request failed.', xhr);
                }
            },
            columns: [
                {
                    data: null,
                    name: 'actions',
                    orderable: false,
                    searchable: false,
                    className: 'text-center text-nowrap align-middle',
                    width: '72px',
                    render: renderActions
                },
                {
                    data: 'createdAt',
                    name: 'createdDate',
                    className: 'text-nowrap align-middle',
                    width: '145px',
                    render: renderDate
                },
                {
                    data: null,
                    name: 'mailType',
                    className: 'text-nowrap align-middle',
                    width: '150px',
                    render: renderMailType
                },
                {
                    data: null,
                    name: 'recipient',
                    className: 'align-middle',
                    width: '220px',
                    render: renderRecipient
                },
                {
                    data: null,
                    name: 'context',
                    orderable: false,
                    searchable: false,
                    className: 'align-middle',
                    width: '230px',
                    render: renderContext
                },
                {
                    data: 'subject',
                    name: 'subject',
                    className: 'align-middle',
                    width: '240px',
                    render: renderSubject
                },
                {
                    data: null,
                    name: 'status',
                    className: 'text-nowrap align-middle',
                    width: '120px',
                    render: renderTransport
                },
                {
                    data: null,
                    name: 'deliveryStatus',
                    className: 'align-middle',
                    width: '260px',
                    render: renderDelivery
                }
            ],
            language: getDataTableLanguage(),
            initComplete: function () {
                styleGeneratedControls($table);
                scheduleTableAdjust($table);
            },
            drawCallback: function () {
                normalizeScrollContainer($table);
            }
        });

        $table.on('xhr.dt', function (_, __, json) {
            if (json && json.summary) {
                updateSummary(json.summary);
            }
        });
    }

    function styleGeneratedControls($table) {
        const $wrapper = $table.closest('.dataTables_wrapper, .dt-container');

        $wrapper.find('.dataTables_length select, .dt-length select')
            .addClass('form-select radius-8');

        $wrapper.find('.dataTables_filter input, .dt-search input')
            .addClass('form-control radius-8');
    }

    function normalizeScrollContainer($table) {
        const $wrapper = $table.closest('.dataTables_wrapper, .dt-container');

        $wrapper.find('.dataTables_scroll, .dt-scroll')
            .css({
                'max-width': '100%',
                'width': '100%'
            });

        $wrapper.find('.dataTables_scrollBody, .dt-scroll-body')
            .css({
                'max-width': '100%',
                'overflow-x': 'auto'
            });
    }

    function scheduleTableAdjust($table) {
        window.setTimeout(function () {
            if (!table) {
                return;
            }

            table.columns.adjust();
            normalizeScrollContainer($table);
        }, 100);
    }

    function bindLayoutAdjustmentEvents($table) {
        if (layoutEventsBound) {
            return;
        }

        layoutEventsBound = true;

        $(window).on('resize.mailDeliveriesTable', function () {
            if (resizeTimer) {
                window.clearTimeout(resizeTimer);
            }

            resizeTimer = window.setTimeout(function () {
                scheduleTableAdjust($table);
            }, 150);
        });

        $(document).on(
            'click.mailDeliveriesTable',
            '.sidebar-toggle, .sidebar-mobile-toggle, .sidebar-close-btn',
            function () {
                window.setTimeout(function () {
                    scheduleTableAdjust($table);
                }, 350);
            });

        document.addEventListener('layout:loaded', function () {
            window.setTimeout(function () {
                scheduleTableAdjust($table);
            }, 100);
        });
    }

    function initializeModal() {
        const modalElement = document.querySelector(selectors.detailModal);
        const contentElement = document.querySelector(selectors.detailContent);

        if (!modalElement || !contentElement || typeof bootstrap === 'undefined') {
            return;
        }

        modal = bootstrap.Modal.getOrCreateInstance(modalElement);
        initialModalContent = contentElement.innerHTML;
    }

    function bindEvents() {
        $(document)
            .off('submit.mailDeliveriesFilter', selectors.filterForm)
            .on('submit.mailDeliveriesFilter', selectors.filterForm, function (event) {
                event.preventDefault();
                reload(true);
            })
            .off('click.mailDeliveriesClear', selectors.clearFilters)
            .on('click.mailDeliveriesClear', selectors.clearFilters, clearFilters)
            .off('click.mailDeliveriesDetail', selectors.detailButton)
            .on('click.mailDeliveriesDetail', selectors.detailButton, openDetail);
    }

    function clearFilters() {
        $(selectors.filterForm).find('input[type="search"], input[type="date"], select').val('');
        reload(true);
    }

    function reload(resetPaging) {
        if (!table) {
            return;
        }

        table.ajax.reload(null, resetPaging === true);
    }

    function renderActions(_, __, row) {
        const url = row.detailUrl || '';
        const title = text('BackOffice.MailDeliveries.Actions.Detail', 'Detay');

        return '' +
            '<button type="button" ' +
            'class="btn btn-sm btn-outline-primary-600 radius-8 js-mail-delivery-detail" ' +
            'data-url="' + escapeHtml(url) + '" ' +
            'title="' + escapeHtml(title) + '">' +
            '<i class="ri-eye-line"></i>' +
            '</button>';
    }

    function renderDate(value) {
        if (!value) {
            return '-';
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return '-';
        }

        return escapeHtml(date.toLocaleString(getCurrentCulture(), {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }));
    }

    function renderMailType(_, __, row) {
        return '<span class="badge bg-primary-50 text-primary-600">' +
            escapeHtml(row.mailTypeText || row.mailTypeName || '-') +
            '</span>';
    }

    function renderRecipient(_, __, row) {
        const name = row.recipientName || row.recipientEmail || '-';
        const email = row.recipientEmail || '';

        return '' +
            '<div class="fw-semibold">' + escapeHtml(name) + '</div>' +
            (email
                ? '<div class="text-secondary-light text-sm">' + escapeHtml(email) + '</div>'
                : '');
    }

    function renderContext(_, __, row) {
        const organization = row.organizationName || '-';
        const congress = row.congressName || '-';
        const submission = row.submissionNumber || '';

        return '' +
            '<div>' + escapeHtml(organization) + '</div>' +
            '<div class="text-secondary-light text-sm">' + escapeHtml(congress) + '</div>' +
            (submission
                ? '<div class="text-primary-600 text-sm">' + escapeHtml(submission) + '</div>'
                : '');
    }

    function renderSubject(value) {
        const subject = value || '-';
        return '<div class="text-truncate d-block" style="width:220px;max-width:220px" title="' +
            escapeHtml(subject) + '">' + escapeHtml(subject) + '</div>';
    }

    function renderTransport(_, __, row) {
        const statusValue = Number(row.status);
        let cssClass = 'bg-neutral-200 text-neutral-700';

        if (statusValue === transportStatus.sent) {
            cssClass = 'bg-success-focus text-success-main';
        } else if (statusValue === transportStatus.failed) {
            cssClass = 'bg-danger-focus text-danger-main';
        } else if (statusValue === transportStatus.pending || statusValue === transportStatus.processing) {
            cssClass = 'bg-warning-focus text-warning-main';
        }

        return '<span class="badge ' + cssClass + '">' +
            escapeHtml(row.statusText || row.statusName || '-') +
            '</span>';
    }

    function renderDelivery(_, __, row) {
        const statusValue = Number(row.deliveryStatus);
        let cssClass = 'bg-neutral-200 text-neutral-700';

        if (statusValue === deliveryStatus.delivered) {
            cssClass = 'bg-success-focus text-success-main';
        } else if (
            statusValue === deliveryStatus.bounced ||
            statusValue === deliveryStatus.rejected ||
            statusValue === deliveryStatus.renderingFailed
        ) {
            cssClass = 'bg-danger-focus text-danger-main';
        } else if (statusValue === deliveryStatus.delayed) {
            cssClass = 'bg-warning-focus text-warning-main';
        } else if (statusValue === deliveryStatus.complaint) {
            cssClass = 'bg-purple-100 text-purple-600';
        } else if (statusValue === deliveryStatus.pending) {
            cssClass = 'bg-info-focus text-info-main';
        }

        let errorText = '';
        const isProviderFailure = [
            deliveryStatus.bounced,
            deliveryStatus.rejected,
            deliveryStatus.complaint,
            deliveryStatus.renderingFailed
        ].includes(statusValue);

        if (isProviderFailure && row.deliveryDiagnosticCode) {
            errorText = row.deliveryDiagnosticCode;
        } else if (Number(row.status) === transportStatus.failed && row.lastError) {
            errorText = row.lastError;
        }

        return '' +
            '<span class="badge ' + cssClass + '">' +
            escapeHtml(row.deliveryStatusText || row.deliveryStatusName || '-') +
            '</span>' +
            (errorText
                ? '<div class="text-danger-600 text-sm mt-1 text-truncate d-block" style="width:240px;max-width:240px" title="' +
                  escapeHtml(errorText) + '">' + escapeHtml(errorText) + '</div>'
                : '');
    }

    async function openDetail(event) {
        const button = event.currentTarget;
        const url = button.dataset.url;
        const contentElement = document.querySelector(selectors.detailContent);

        if (!url || !modal || !contentElement) {
            return;
        }

        button.disabled = true;
        contentElement.innerHTML = initialModalContent;
        modal.show();

        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error('Mail delivery detail request failed.');
            }

            contentElement.innerHTML = await response.text();
        } catch (error) {
            contentElement.innerHTML = '' +
                '<div class="modal-header">' +
                '<h6 class="modal-title">' + escapeHtml(text('BackOffice.MailDeliveries.Detail.Title', 'E-posta Gönderim Detayı')) + '</h6>' +
                '<button type="button" class="btn-close" data-bs-dismiss="modal"></button>' +
                '</div>' +
                '<div class="modal-body">' +
                '<div class="alert alert-danger mb-0">' + escapeHtml(text('BackOffice.MailDeliveries.Detail.LoadError', 'Detay bilgisi yüklenemedi.')) + '</div>' +
                '</div>';

            console.error(error);
        } finally {
            button.disabled = false;
        }
    }

    function updateSummary(summary) {
        setText('#mailDeliverySummaryTotal', summary.total);
        setText('#mailDeliverySummaryPending', summary.pendingTransport);
        setText('#mailDeliverySummaryDelivered', summary.delivered);
        setText('#mailDeliverySummaryBounced', summary.bounced);
        setText('#mailDeliverySummaryFailed', summary.failedTransport);
    }

    function setText(selector, value) {
        const element = document.querySelector(selector);
        if (element) {
            element.textContent = String(value == null ? 0 : value);
        }
    }

    function valueOf(selector) {
        const value = $(selector).val();
        return value == null ? '' : String(value).trim();
    }

    function buildAjaxHeaders() {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.buildAjaxHeaders === 'function') {
            return window.Symplify.Ajax.buildAjaxHeaders($('#mailDeliveryAntiForgeryForm'));
        }

        const token = $('#mailDeliveryAntiForgeryForm input[name="__RequestVerificationToken"]').val();
        return token ? { RequestVerificationToken: token } : {};
    }

    function getDataTableLanguage() {
        return (window.Symplify && window.Symplify.dataTables && window.Symplify.dataTables.language)
            || (window.Symplify && window.Symplify.DataTables && window.Symplify.DataTables.language)
            || {
                processing: 'İşleniyor...',
                lengthMenu: '_MENU_ kayıt göster',
                info: '_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor',
                infoEmpty: 'Kayıt bulunamadı',
                zeroRecords: 'Eşleşen kayıt bulunamadı',
                paginate: {
                    first: 'İlk',
                    last: 'Son',
                    next: 'Sonraki',
                    previous: 'Önceki'
                }
            };
    }

    function getCurrentCulture() {
        return location.pathname.split('/').filter(Boolean)[0]
            || document.documentElement.lang
            || 'tr-TR';
    }

    function text(key, fallback) {
        return window.Symplify && typeof window.Symplify.t === 'function'
            ? window.Symplify.t(key, fallback)
            : fallback;
    }

    function escapeHtml(value) {
        return $('<div/>').text(value == null ? '' : String(value)).html();
    }

    return {
        init: init,
        reload: reload
    };
})(jQuery);

$(function () {
    window.Symplify.MailDeliveries.Index.init();
});
