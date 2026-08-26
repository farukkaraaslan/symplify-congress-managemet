window.Symplify = window.Symplify || {};
window.Symplify.Users = window.Symplify.Users || {};

window.Symplify.Users.Table = (function ($) {
    'use strict';

    let table = null;
    let resizeTimer = null;
    let layoutEventsBound = false;

    function init() {
        const $table = $('.js-users-data-table');

        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        initializeSelect2($table);
        bindFilters($table);

        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();
            scheduleTableAdjust($table);
            return;
        }

        table = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            searchDelay: 350,
            ordering: true,
            paging: true,
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            autoWidth: false,
            responsive: false,
            scrollX: true,
            scrollCollapse: true,
            language: getDataTableLanguage(),
            order: [[10, 'desc']],
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders(),
                data: function (data) {
                    return $.extend({}, data, collectFilters($table));
                },
                dataSrc: function (json) {
                    const filteredCount = json && Number.isFinite(Number(json.recordsFiltered))
                        ? Number(json.recordsFiltered)
                        : 0;

                    $('[data-users-list-count]').text(filteredCount.toString());

                    return json && Array.isArray(json.data)
                        ? json.data
                        : [];
                },
                error: showError
            },
            columns: getColumns($table),
            initComplete: function () {
                styleGeneratedControls($table);
                scheduleTableAdjust($table);
            },
            drawCallback: function () {
                normalizeScrollContainer($table);
            }
        });

        bindLayoutAdjustmentEvents($table);
    }

    function getColumns($table) {
        return [
            {
                data: null,
                name: 'actions',
                orderable: false,
                searchable: false,
                className: 'text-center text-nowrap align-middle',
                width: '96px',
                render: function (data, type, row) {
                    return renderActions(row, $table);
                }
            },
            {
                data: 'rowNumber',
                name: 'rowNumber',
                orderable: false,
                searchable: false,
                className: 'text-nowrap fw-semibold align-middle',
                width: '48px',
                render: renderText
            },
            {
                data: null,
                name: 'fullName',
                orderable: true,
                searchable: true,
                className: 'align-middle',
                width: '210px',
                render: renderUser
            },
            {
                data: 'email',
                name: 'email',
                orderable: true,
                searchable: true,
                className: 'text-nowrap align-middle',
                width: '190px',
                render: renderEmail
            },
            {
                data: 'phoneNumber',
                name: 'phoneNumber',
                orderable: true,
                searchable: true,
                className: 'text-nowrap align-middle',
                width: '145px',
                render: renderPhone
            },
            {
                data: 'institution',
                name: 'institution',
                orderable: true,
                searchable: true,
                className: 'align-middle',
                width: '180px',
                render: renderInstitution
            },
            {
                data: null,
                name: 'location',
                orderable: false,
                searchable: true,
                className: 'align-middle',
                width: '125px',
                render: renderLocation
            },
            {
                data: null,
                name: 'access',
                orderable: false,
                searchable: true,
                className: 'align-middle',
                width: '190px',
                render: renderAccess
            },
            {
                data: 'rolesText',
                name: 'roles',
                orderable: false,
                searchable: true,
                className: 'align-middle',
                width: '100px',
                render: renderRoles
            },
            {
                data: null,
                name: 'status',
                orderable: false,
                searchable: false,
                className: 'align-middle',
                width: '150px',
                render: function (data, type, row) {
                    return renderStatus(row, $table);
                }
            },
            {
                data: 'createdDate',
                name: 'createdDate',
                orderable: true,
                searchable: false,
                className: 'text-nowrap align-middle',
                width: '130px',
                render: renderText
            }
        ];
    }

    function initializeSelect2($table) {
        if (!$.fn.select2) {
            console.error(
                'Select2 yüklenemedi. /lib/select2/dist/js/select2.min.js yolunu ve LibMan restore işlemini kontrol edin.'
            );
            return;
        }

        const containerSelector = $table.data('filters-container') || '#usersFilters';
        const $container = $(containerSelector);

        $container.find('.js-users-select2').each(function () {
            const $select = $(this);

            if ($select.data('select2')) {
                return;
            }

            const pageCulture = (document.documentElement.lang || '').toLowerCase();
            const select2Language = pageCulture.startsWith('tr') ? 'tr' : 'en';

            $select.select2({
                width: '100%',
                placeholder: ($select.data('placeholder') || '').toString(),
                minimumResultsForSearch: 0,
                language: select2Language
            });
        });
    }

    function bindFilters($table) {
        const containerSelector = $table.data('filters-container') || '#usersFilters';
        const $container = $(containerSelector);

        if (!$container.length || $container.data('users-filters-bound') === true) {
            return;
        }

        $container.data('users-filters-bound', true);

        $container.on('click.usersFilters', '#usersApplyFilters', function () {
            reload(true);
        });

        $container.on('click.usersFilters', '#usersResetFilters', async function () {
            setSelectValue($container.find('#usersOrganizationId'), '');
            setSelectValue($container.find('#usersRoleName'), '');
            setSelectValue($container.find('#usersEmailConfirmed'), '');
            setSelectValue($container.find('#usersCountryId'), '');
            setSelectValue($container.find('#usersAccountStatus'), '');

            await Promise.all([
                reloadDependentSelect({
                    select: $container.find('#usersCongressId'),
                    url: $container.find('#usersOrganizationId').data('congress-url'),
                    parameters: {},
                    placeholder: getPlaceholder($container.find('#usersCongressId'))
                }),
                reloadDependentSelect({
                    select: $container.find('#usersStateId'),
                    url: $container.find('#usersCountryId').data('state-url'),
                    parameters: {},
                    placeholder: getPlaceholder($container.find('#usersStateId'))
                })
            ]);

            if (table) {
                table.search('');
            }

            clearDataTableSearch($table);
            reload(true);
        });

        $container.on('change.usersDependent', '#usersOrganizationId', async function () {
            await reloadDependentSelect({
                select: $container.find('#usersCongressId'),
                url: $(this).data('congress-url'),
                parameters: { organizationId: $(this).val() || '' },
                placeholder: getPlaceholder($container.find('#usersCongressId'))
            });
        });

        $container.on('change.usersDependent', '#usersCountryId', async function () {
            await reloadDependentSelect({
                select: $container.find('#usersStateId'),
                url: $(this).data('state-url'),
                parameters: { countryId: $(this).val() || '' },
                placeholder: getPlaceholder($container.find('#usersStateId'))
            });
        });
    }

    function collectFilters($table) {
        const containerSelector = $table.data('filters-container') || '#usersFilters';
        const $container = $(containerSelector);

        return {
            organizationId: getFilterValue($container, '#usersOrganizationId'),
            congressId: getFilterValue($container, '#usersCongressId'),
            roleName: getFilterValue($container, '#usersRoleName'),
            emailConfirmed: getFilterValue($container, '#usersEmailConfirmed'),
            countryId: getFilterValue($container, '#usersCountryId'),
            stateId: getFilterValue($container, '#usersStateId'),
            accountStatus: getFilterValue($container, '#usersAccountStatus')
        };
    }

    async function reloadDependentSelect(options) {
        const $select = options.select;
        const url = (options.url || '').toString();
        const placeholder = options.placeholder || 'Tümü';

        if (!$select.length || !url) {
            return;
        }

        const queryString = $.param(options.parameters || {});
        const requestUrl = queryString ? url + '?' + queryString : url;

        $select.prop('disabled', true);
        resetSelect($select, placeholder);

        try {
            const response = await fetch(requestUrl, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) {
                return;
            }

            const items = await response.json();

            if (Array.isArray(items)) {
                items.forEach(function (item) {
                    const option = new Option(
                        item.text || '',
                        item.value || '',
                        false,
                        Boolean(item.selected));

                    $select.append(option);
                });
            }
        } finally {
            $select.prop('disabled', false);
            notifySelect2($select);
        }
    }

    function resetSelect($select, placeholder) {
        $select.empty().append(new Option(placeholder, ''));
        $select.val('');
        notifySelect2($select);
    }

    function setSelectValue($select, value) {
        $select.val(value);
        notifySelect2($select);
    }

    function notifySelect2($select) {
        if ($.fn.select2 && $select.data('select2')) {
            $select.trigger('change.select2');
        }
    }

    function getPlaceholder($select) {
        return ($select.data('placeholder') || $select.find('option[value=""]').first().text() || 'Tümü')
            .toString();
    }

    function getFilterValue($container, selector) {
        return ($container.find(selector).val() || '').toString();
    }

    function clearDataTableSearch($table) {
        const $wrapper = $table.closest('.dataTables_wrapper, .dt-container');
        $wrapper.find('.dataTables_filter input, .dt-search input').val('');
    }

    function reload(resetPaging) {
        if (table) {
            table.ajax.reload(null, resetPaging === true);
        }
    }

    function styleGeneratedControls($table) {
        const $wrapper = $table.closest('.dataTables_wrapper, .dt-container');

        $wrapper.find('.dataTables_length select, .dt-length select')
            .addClass('form-select radius-8');

        $wrapper.find('.dataTables_filter input, .dt-search input')
            .addClass('form-control radius-8');
    }

    function normalizeScrollContainer($table) {
        return $table;
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

        $(window).on('resize.usersTable', function () {
            if (resizeTimer) {
                window.clearTimeout(resizeTimer);
            }

            resizeTimer = window.setTimeout(function () {
                scheduleTableAdjust($table);
            }, 150);
        });

        $(document).on(
            'click.usersTable',
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

    function renderUser(data, type, row) {
        const fullName = normalizeEmpty(row.fullName);
        const title = normalizeEmpty(row.titleShortName);
        const orcid = normalizeEmpty(row.orcid);
        const initials = getInitials(fullName);
        const displayedName = title === '-' ? fullName : title + ' ' + fullName;

        return '' +
            '<div class="d-flex align-items-center gap-2">' +
                '<span class="w-40-px h-40-px bg-primary-50 text-primary-600 rounded-circle d-flex justify-content-center align-items-center flex-shrink-0 me-12 fw-semibold">' +
                    escapeHtml(initials) +
                '</span>' +
                '<div class="flex-grow-1">' +
                    '<span class="text-md mb-0 fw-semibold text-primary-light d-block text-wrap lh-sm">' +
                        escapeHtml(displayedName) +
                    '</span>' +
                    (orcid !== '-'
                        ? '<small class="text-neutral-500 d-block">ORCID: ' + escapeHtml(orcid) + '</small>'
                        : '') +
                '</div>' +
            '</div>';
    }

    function renderEmail(data) {
        const value = normalizeEmpty(data);

        if (value === '-') {
            return emptyValue();
        }

        return '<a href="mailto:' + escapeAttribute(value) + '" class="text-primary-600 text-hover-primary-700">' +
            escapeHtml(value) +
            '</a>';
    }

    function renderPhone(data) {
        const value = normalizeEmpty(data);

        if (value === '-') {
            return emptyValue();
        }

        const phoneHref = value.replace(/[^+\d]/g, '');

        return '<a href="tel:' + escapeAttribute(phoneHref) + '" class="text-primary-light">' +
            escapeHtml(value) +
            '</a>';
    }

    function renderInstitution(data) {
        const value = normalizeEmpty(data);

        if (value === '-') {
            return emptyValue();
        }

        return '<div class="text-wrap" title="' + escapeAttribute(value) + '">' +
            escapeHtml(value) +
            '</div>';
    }

    function renderLocation(data, type, row) {
        const country = normalizeEmpty(row.countryName);
        const state = normalizeEmpty(row.stateName);

        if (country === '-' && state === '-') {
            return emptyValue();
        }

        return '' +
            '<div class="d-flex flex-column">' +
                (country !== '-'
                    ? '<span class="d-block fw-medium">' + escapeHtml(country) + '</span>'
                    : '') +
                (state !== '-'
                    ? '<small class="text-neutral-500 d-block">' + escapeHtml(state) + '</small>'
                    : '') +
            '</div>';
    }

    function renderAccess(data, type, row) {
        const organization = normalizeEmpty(row.organizationShortName || row.organizationName);
        const congress = normalizeEmpty(row.defaultCongressName);

        if (organization === '-' && congress === '-') {
            return emptyValue();
        }

        return '' +
            '<div class="d-flex flex-column align-items-start">' +
                (organization !== '-'
                    ? '<span class="badge bg-primary-50 text-primary-600 rounded-pill mb-1">' +
                        escapeHtml(organization) +
                      '</span>'
                    : '') +
                (congress !== '-'
                    ? '<small class="text-neutral-500 d-block text-wrap" title="' +
                        escapeAttribute(congress) +
                      '">' +
                        escapeHtml(congress) +
                      '</small>'
                    : '') +
            '</div>';
    }

    function renderRoles(data) {
        const value = normalizeEmpty(data);

        if (value === '-') {
            return emptyValue();
        }

        return value
            .split(',')
            .map(function (role) { return role.trim(); })
            .filter(Boolean)
            .map(function (role) {
                return '<span class="badge bg-info-50 text-info-600 rounded-pill me-1 mb-1">' +
                    escapeHtml(role) +
                    '</span>';
            })
            .join('');
    }

    function renderStatus(row, $table) {
        const labels = [];

        if (row.isBlacklisted === true) {
            labels.push(statusBadge('danger', $table.data('blacklisted-text') || 'Kara Liste'));
        } else if (row.isLockedOut === true) {
            labels.push(statusBadge('warning', $table.data('locked-text') || 'Kilitli'));
        } else {
            labels.push(statusBadge('success', $table.data('active-text') || 'Aktif'));
        }

        if (row.emailConfirmed === true) {
            labels.push(statusBadge('primary', $table.data('email-confirmed-text') || 'E-posta Onaylı'));
        } else {
            labels.push(statusBadge('neutral', $table.data('email-unconfirmed-text') || 'E-posta Onaysız'));
        }

        if (row.organizationAccessIsActive === false) {
            labels.push(statusBadge('neutral', $table.data('org-inactive-text') || 'Erişim Pasif'));
        }

        return '<div class="d-flex flex-wrap gap-1">' + labels.join('') + '</div>';
    }

    function statusBadge(tone, text) {
        const classMap = {
            success: 'bg-success-100 text-success-600',
            warning: 'bg-warning-100 text-warning-600',
            danger: 'bg-danger-100 text-danger-600',
            primary: 'bg-primary-50 text-primary-600',
            neutral: 'bg-neutral-200 text-neutral-700'
        };

        return '<span class="badge ' + (classMap[tone] || classMap.neutral) + ' rounded-pill">' +
            escapeHtml(text) +
            '</span>';
    }

    function renderActions(row, $table) {
        const detailsTemplate = ($table.data('details-url-template') || '').toString();
        const editTemplate = ($table.data('edit-url-template') || '').toString();
        const id = encodeURIComponent(row.id || '');
        const detailsUrl = detailsTemplate.replace('__id__', id);
        const editUrl = editTemplate.replace('__id__', id);
        const detailsText = $table.data('details-text') || 'Detay';
        const editText = $table.data('edit-text') || 'Düzenle';

        return '' +
            '<div class="d-flex align-items-center justify-content-center gap-2">' +
                '<a href="' + escapeAttribute(detailsUrl) + '"' +
                   ' title="' + escapeAttribute(detailsText) + '"' +
                   ' class="bg-info-50 bg-hover-info-100 text-info-600 w-40-px h-40-px d-flex justify-content-center align-items-center rounded-circle">' +
                    '<i class="ri-eye-line text-lg"></i>' +
                '</a>' +
                '<a href="' + escapeAttribute(editUrl) + '"' +
                   ' title="' + escapeAttribute(editText) + '"' +
                   ' class="bg-success-50 bg-hover-success-100 text-success-600 w-40-px h-40-px d-flex justify-content-center align-items-center rounded-circle">' +
                    '<i class="ri-edit-line text-lg"></i>' +
                '</a>' +
            '</div>';
    }

    function renderText(data) {
        const value = normalizeEmpty(data);
        return value === '-' ? emptyValue() : escapeHtml(value);
    }

    function emptyValue() {
        return '<span class="text-neutral-400">-</span>';
    }

    function normalizeEmpty(value) {
        const normalized = value === null || value === undefined
            ? ''
            : value.toString().trim();

        return normalized.length ? normalized : '-';
    }

    function getInitials(fullName) {
        if (!fullName || fullName === '-') {
            return 'K';
        }

        const parts = fullName.split(/\s+/).filter(Boolean);

        return parts
            .slice(0, 2)
            .map(function (part) { return part.charAt(0); })
            .join('')
            .toUpperCase();
    }

    function getAjaxHeaders() {
        const token = $('input[name="__RequestVerificationToken"]').first().val();
        return token ? { RequestVerificationToken: token } : {};
    }

    function getDataTableLanguage() {
        if (window.Symplify.DataTables &&
            typeof window.Symplify.DataTables.getLanguage === 'function') {
            return window.Symplify.DataTables.getLanguage();
        }

        return window.Symplify.DataTables?.language ||
            window.Symplify.dataTables?.language ||
            {};
    }

    function showError(xhr) {
        const message = xhr && xhr.responseJSON && xhr.responseJSON.message
            ? xhr.responseJSON.message
            : 'Kullanıcı listesi yüklenirken bir sorun oluştu.';

        if (window.Swal) {
            window.Swal.fire({
                icon: 'error',
                title: message
            });
        }
    }

    function escapeHtml(value) {
        return $('<div/>')
            .text(value === null || value === undefined ? '' : value)
            .html();
    }

    function escapeAttribute(value) {
        return escapeHtml(value).replace(/"/g, '&quot;');
    }

    return {
        init: init,
        reload: reload
    };
}(jQuery));

$(function () {
    window.Symplify.Users.Table.init();
});
