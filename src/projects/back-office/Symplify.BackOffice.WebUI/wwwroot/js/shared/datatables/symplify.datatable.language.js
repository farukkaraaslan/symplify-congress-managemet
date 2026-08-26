window.Symplify = window.Symplify || {};
window.Symplify.DataTables = window.Symplify.DataTables || {};

(function () {
    'use strict';

    function t(key, fallback) {
        if (window.Symplify && typeof window.Symplify.t === 'function') {
            return window.Symplify.t(key, fallback);
        }

        return fallback || key;
    }

    const language = {
        search: t('Common.DataTable.Search', 'Search:'),
        lengthMenu: t('Common.DataTable.LengthMenu', 'Show _MENU_ entries'),
        info: t('Common.DataTable.Info', 'Showing _START_ to _END_ of _TOTAL_ entries'),
        infoEmpty: t('Common.DataTable.InfoEmpty', 'No records available'),
        infoFiltered: t('Common.DataTable.InfoFiltered', '(filtered from _MAX_ total entries)'),
        zeroRecords: t('Common.DataTable.ZeroRecords', 'No matching records found'),
        processing: t('Common.DataTable.Processing', 'Loading...'),
        emptyTable: t('Common.DataTable.EmptyTable', 'No data available in table'),
        loadingRecords: t('Common.DataTable.LoadingRecords', 'Loading records...'),
        paginate: {
            first: t('Common.DataTable.First', 'First'),
            last: t('Common.DataTable.Last', 'Last'),
            next: t('Common.DataTable.Next', 'Next'),
            previous: t('Common.DataTable.Previous', 'Previous')
        },
        aria: {
            sortAscending: t('Common.DataTable.SortAscending', ': activate to sort column ascending'),
            sortDescending: t('Common.DataTable.SortDescending', ': activate to sort column descending')
        }
    };

    window.Symplify.dataTables = window.Symplify.dataTables || {};
    window.Symplify.dataTables.language = window.Symplify.dataTables.language || language;
    window.Symplify.DataTables.language = window.Symplify.dataTables.language;
})();
