document.addEventListener('DOMContentLoaded', function () {
  if (typeof DataTable === 'undefined') return;

  document.querySelectorAll('table[data-obs-datatable]').forEach(function (tableEl) {
    if (tableEl.dataset.dtInitialized === 'true') return;

    const noOrderTargets = [];
    tableEl.querySelectorAll('thead tr:first-child th').forEach(function (th, index) {
      if (th.dataset.orderable === 'false') noOrderTargets.push(index);
    });

    const pageLength = Number(tableEl.dataset.pageLength || 10);
    const useButtons = tableEl.dataset.buttons !== 'false';
    const useScrollX = tableEl.dataset.scrollX !== 'false';
    const useSearch = tableEl.dataset.search !== 'false';
    const usePaging = tableEl.dataset.paging !== 'false';
    const useInfo = tableEl.dataset.info !== 'false';
    const hiddenColumns = (tableEl.dataset.hiddenColumns || '')
      .split(',')
      .map(function (value) { return Number(value.trim()); })
      .filter(function (value) { return Number.isInteger(value) && value >= 0; });

    const options = {
      autoWidth: true,
      scrollX: useScrollX,
      scrollCollapse: true,
      responsive: false,
      fixedHeader: false,
      pageLength: pageLength,
      lengthMenu: [[10, 20, 30, 50, 100], [10, 20, 30, 50, 100]],
      paging: usePaging,
      searching: useSearch,
      info: useInfo,
      order: [],
      titleRow: 0,
      language: {
        search: 'Ara:',
        searchPlaceholder: tableEl.dataset.searchPlaceholder || 'Tabloda ara...',
        lengthMenu: 'Sayfada _MENU_ kayıt göster',
        info: '_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor',
        infoEmpty: 'Kayıt bulunamadı',
        zeroRecords: 'Eşleşen kayıt bulunamadı',
        emptyTable: 'Gösterilecek kayıt yok',
        paginate: { previous: 'Önceki', next: 'Sonraki' }
      },
      columnDefs: [
        ...(noOrderTargets.length ? [{ targets: noOrderTargets, orderable: false }] : []),
        ...(hiddenColumns.length ? [{ targets: hiddenColumns, visible: false }] : [])
      ],
      layout: {
        topStart: [
          ...(usePaging ? ['pageLength'] : []),
          ...(useButtons ? [{
            buttons: [
              { extend: 'copyHtml5', text: 'Kopyala' },
              { extend: 'excelHtml5', text: 'Excel' },
              { extend: 'pdfHtml5', text: 'PDF' },
              { extend: 'colvis', text: 'Kolonlar' }
            ]
          }] : [])
        ],
        topEnd: useSearch ? 'search' : null,
        bottomStart: useInfo ? 'info' : null,
        bottomEnd: usePaging ? 'paging' : null
      },
      initComplete: function () {
        this.api().columns.adjust();
      }
    };


    const dt = new DataTable(tableEl, options);
    tableEl.dataset.dtInitialized = 'true';

    const dtContainer = dt.table().container();

    /*
     * scrollX açıkken DataTables görünür header'ı .dt-scroll-head içine klonlar.
     * tableEl içindeki thead ise kolon ölçümü için kullanılan sizing header olur.
     * Bu yüzden filtre eventlerini doğrudan tableEl thead'ine bağlamak görünür
     * inputların çalışmamasına neden olur. Aşağıdaki yardımcılar görünür header'ı
     * bulur ve eventleri ona bağlar.
     */
    function getAllFilterRows() {
      if (!dtContainer) return [];
      return Array.from(dtContainer.querySelectorAll('thead tr.dt-filter-row'));
    }

    function getVisibleFilterRows() {
      const rows = getAllFilterRows();
      const visibleRows = rows.filter(function (row) {
        return !row.closest('.dt-scroll-body, .dataTables_scrollBody');
      });
      return visibleRows.length ? visibleRows : rows;
    }

    function syncHeaderFilterValue(columnIndex, value, sourceControl) {
      getAllFilterRows().forEach(function (row) {
        const cell = row.children[columnIndex];
        const control = cell ? cell.querySelector('input, select') : null;
        if (control && control !== sourceControl) control.value = value;
      });
    }

    function syncExternalFilterValue(columnIndex, value) {
      document.querySelectorAll('[data-dt-filters-for="' + tableEl.id + '"] [data-dt-filter-column="' + columnIndex + '"]').forEach(function (externalControl) {
        if (externalControl.tagName === 'SELECT') {
          const optionExists = Array.from(externalControl.options || []).some(function (option) {
            return option.value === value;
          });
          externalControl.value = optionExists ? value : '';
        } else {
          externalControl.value = value;
        }
      });
    }

    function bindVisibleColumnFilters() {
      getVisibleFilterRows().forEach(function (filterRow) {
        Array.from(filterRow.children).forEach(function (th, columnIndex) {
          const control = th.querySelector('input, select');
          if (!control || control.dataset.dtFilterBound === 'true') return;

          control.dataset.dtFilterBound = 'true';

          const apply = function () {
            const value = control.value || '';

            syncHeaderFilterValue(columnIndex, value, control);
            syncExternalFilterValue(columnIndex, value);

            if (dt.column(columnIndex).search() !== value) {
              dt.column(columnIndex).search(value).draw();
            }
          };

          control.addEventListener(control.tagName === 'SELECT' ? 'change' : 'input', apply);
          control.addEventListener('click', function (event) { event.stopPropagation(); });
          control.addEventListener('keydown', function (event) { event.stopPropagation(); });
        });
      });
    }

    bindVisibleColumnFilters();

    // ColVis / kolon yeniden çizimi görünür header DOM'unu yenileyebilir.
    dt.on('draw column-visibility column-reorder', function () {
      bindVisibleColumnFilters();
    });

    // Tablo dışında yer alan kurumsal filtre paneli.
    const externalFilterRoot = document.querySelector('[data-dt-filters-for="' + tableEl.id + '"]');
    const externalApplyButtons = Array.from(document.querySelectorAll('[data-dt-filter-apply="' + tableEl.id + '"]'));
    const useExplicitExternalApply = externalApplyButtons.length > 0;

    function applyExternalFilters(button) {
      if (!externalFilterRoot) return;

      const filterScope = button && button.closest('.student-filter-advanced')
        ? button.closest('.student-filter-advanced')
        : externalFilterRoot;

      filterScope.querySelectorAll('[data-dt-filter-column]').forEach(function (control) {
        const columnIndex = Number(control.dataset.dtFilterColumn);
        if (!Number.isInteger(columnIndex)) return;

        const value = control.value || '';
        syncHeaderFilterValue(columnIndex, value, null);

        // Öğrenci yaşı için operatör desteği. Yaş kolonu yardımcı/gizli kolondur.
        if (control.id === 'studentAgeFilter') {
          const operator = externalFilterRoot.querySelector('#studentAgeOperator');
          const numericValue = Number(value);

          if (value && Number.isFinite(numericValue) && operator && operator.value !== '=') {
            const selectedOperator = operator.value;
            dt.column(columnIndex).search(function (cellData) {
              const cellValue = Number(cellData);
              if (!Number.isFinite(cellValue)) return false;
              return selectedOperator === '>=' ? cellValue >= numericValue : cellValue <= numericValue;
            });
            return;
          }
        }

        dt.column(columnIndex).search(value);
      });

      dt.draw();
    }

    if (externalFilterRoot) {
      const advancedFilters = externalFilterRoot.querySelector('.student-filter-advanced');
      const filterToggleButton = externalFilterRoot.querySelector('[data-dt-filter-toggle]');

      if (advancedFilters && filterToggleButton) {
        const toggleLabel = filterToggleButton.querySelector('[data-dt-filter-toggle-label]');
        const toggleIcon = filterToggleButton.querySelector('[data-dt-filter-toggle-icon]');

        const syncFilterToggleState = function () {
          const isOpen = advancedFilters.classList.contains('show');
          filterToggleButton.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
          if (toggleLabel) toggleLabel.textContent = isOpen ? 'Filtreleri Gizle' : 'Filtreleri Göster';
          if (toggleIcon) toggleIcon.className = isOpen ? 'ri-arrow-up-s-line' : 'ri-arrow-down-s-line';
        };

        advancedFilters.addEventListener('shown.bs.collapse', syncFilterToggleState);
        advancedFilters.addEventListener('hidden.bs.collapse', syncFilterToggleState);
        syncFilterToggleState();
      }

      externalFilterRoot.querySelectorAll('[data-dt-filter-column]').forEach(function (control) {
        if (useExplicitExternalApply && control.closest('.student-filter-advanced')) return;

        const columnIndex = Number(control.dataset.dtFilterColumn);
        if (!Number.isInteger(columnIndex)) return;

        const externalEvent = control.tagName === 'SELECT' ? 'change' : 'input';
        control.addEventListener(externalEvent, function () {
          const value = control.value || '';
          syncHeaderFilterValue(columnIndex, value, null);
          if (dt.column(columnIndex).search() !== value) {
            dt.column(columnIndex).search(value).draw();
          }
        });
      });

      if (externalFilterRoot.dataset.hideBasicOnAdvanced === 'true') {
        const advancedFilters = externalFilterRoot.querySelector('.student-filter-advanced');
        const basicFilters = externalFilterRoot.querySelector('.student-filter-basic');

        if (advancedFilters && basicFilters) {
          const syncBasicVisibility = function () {
            basicFilters.classList.toggle('d-none', advancedFilters.classList.contains('show'));
          };

          advancedFilters.addEventListener('show.bs.collapse', function () { basicFilters.classList.add('d-none'); });
          advancedFilters.addEventListener('hide.bs.collapse', function () { basicFilters.classList.remove('d-none'); });
          syncBasicVisibility();
        }
      }
    }

    externalApplyButtons.forEach(function (button) {
      button.addEventListener('click', function () {
        applyExternalFilters(button);
      });
    });

    document.querySelectorAll('[data-dt-filter-reset="' + tableEl.id + '"]').forEach(function (button) {
      button.addEventListener('click', function () {
        if (externalFilterRoot) {
          externalFilterRoot.querySelectorAll('[data-dt-filter-column]').forEach(function (control) {
            control.value = '';
          });

          const ageOperator = externalFilterRoot.querySelector('#studentAgeOperator');
          if (ageOperator) ageOperator.value = '=';
        }

        getAllFilterRows().forEach(function (filterRow) {
          filterRow.querySelectorAll('input, select').forEach(function (control) {
            control.value = '';
          });
        });

        dt.columns().search('');
        dt.search('').draw();

        const searchInput = dtContainer ? dtContainer.querySelector('.dt-search input') : null;
        if (searchInput) searchInput.value = '';
      });
    });

    /*
     * DataTable dikeyde sabit bir alana hapsedilmez. Sayfadaki kayıt sayısı
     * kadar doğal olarak büyür; yalnızca geniş tablolar için yatay scroll
     * DataTables'ın kendi scrollX yapısıyla korunur.
     */
    if (dtContainer && useScrollX) {
      dtContainer.classList.add('dt-scroll-x-only');
      dtContainer.classList.remove('dt-scroll-y-enabled');
    }

    if (document.fonts && document.fonts.ready) {
      document.fonts.ready.then(function () { dt.columns.adjust(); });
    }
  });
});
