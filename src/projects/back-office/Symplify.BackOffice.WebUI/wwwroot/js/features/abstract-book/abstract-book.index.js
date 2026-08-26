(function () {
    'use strict';

    const root = document.getElementById('abstractBookRoot');
    if (!root) return;

    const form = document.getElementById('abstractBookExportForm');
    if (!form) return;

    function normalizeText(value) {
        return (value || '')
            .toLocaleLowerCase(document.documentElement.lang || 'tr-TR')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .trim();
    }

    function closeMultiSelects(exceptWrapper) {
        document.querySelectorAll('.program-filter-multiselect.is-open').forEach(wrapper => {
            if (wrapper === exceptWrapper) return;
            wrapper.classList.remove('is-open');
            wrapper.querySelector('.program-filter-multiselect__menu')?.classList.add('d-none');
            wrapper.querySelector('.program-filter-multiselect__toggle')
                ?.setAttribute('aria-expanded', 'false');
        });
    }

    function initializeMultiSelects(scope) {
        (scope || document)
            .querySelectorAll('select.js-abstract-book-filter-multi[multiple]')
            .forEach(select => {
                if (select.dataset.multiselectReady === 'true') return;

                select.dataset.multiselectReady = 'true';
                select.classList.add('program-filter-multiselect__native', 'd-none');

                const wrapper = document.createElement('div');
                wrapper.className = 'program-filter-multiselect position-relative';
                select.parentNode.insertBefore(wrapper, select);
                wrapper.appendChild(select);

                const toggle = document.createElement('button');
                toggle.type = 'button';
                toggle.className = 'program-filter-multiselect__toggle form-select d-flex align-items-center justify-content-between gap-2 text-start';
                toggle.setAttribute('aria-haspopup', 'listbox');
                toggle.setAttribute('aria-expanded', 'false');

                const summary = document.createElement('span');
                summary.className = 'program-filter-multiselect__summary d-flex align-items-center gap-1 flex-wrap flex-grow-1 min-w-0';

                const chevron = document.createElement('i');
                chevron.className = 'ri-arrow-down-s-line program-filter-multiselect__chevron';
                chevron.setAttribute('aria-hidden', 'true');
                toggle.append(summary, chevron);

                const menu = document.createElement('div');
                menu.className = 'program-filter-multiselect__menu position-absolute top-100 start-0 end-0 z-3 mt-4 p-12 bg-base border radius-12 shadow-sm d-none';

                const searchWrap = document.createElement('div');
                searchWrap.className = 'program-filter-multiselect__search-wrap input-group mb-2';

                const searchIcon = document.createElement('i');
                searchIcon.className = 'ri-search-line input-group-text';
                searchIcon.setAttribute('aria-hidden', 'true');

                const search = document.createElement('input');
                search.type = 'search';
                search.className = 'program-filter-multiselect__search form-control';
                search.autocomplete = 'off';
                search.placeholder = select.dataset.searchPlaceholder || '';
                searchWrap.append(searchIcon, search);

                const toolbar = document.createElement('div');
                toolbar.className = 'program-filter-multiselect__toolbar d-flex align-items-center justify-content-between gap-2 mb-2';

                const selectAllButton = document.createElement('button');
                selectAllButton.type = 'button';
                selectAllButton.className = 'program-filter-multiselect__action btn btn-sm btn-outline-primary-600';
                selectAllButton.textContent = select.dataset.selectAllText || '';

                const clearButton = document.createElement('button');
                clearButton.type = 'button';
                clearButton.className = 'program-filter-multiselect__action btn btn-sm btn-outline-neutral-500';
                clearButton.textContent = select.dataset.clearText || '';
                toolbar.append(selectAllButton, clearButton);

                const optionsContainer = document.createElement('div');
                optionsContainer.className = 'program-filter-multiselect__options d-flex flex-column gap-1 max-h-258-px overflow-auto';
                optionsContainer.setAttribute('role', 'listbox');
                optionsContainer.setAttribute('aria-multiselectable', 'true');

                const noOptions = document.createElement('div');
                noOptions.className = 'program-filter-multiselect__no-options d-none';
                noOptions.textContent = select.dataset.noOptionsText || '';

                const optionRows = Array.from(select.options).map((option, index) => {
                    const row = document.createElement('label');
                    row.className = 'program-filter-multiselect__option d-flex align-items-center gap-2 px-8 py-8 radius-8 bg-hover-primary-50 cursor-pointer';
                    row.dataset.searchText = normalizeText(option.textContent);
                    row.setAttribute('role', 'option');

                    const checkbox = document.createElement('input');
                    checkbox.type = 'checkbox';
                    checkbox.className = 'form-check-input';
                    checkbox.checked = option.selected;
                    checkbox.disabled = option.disabled;
                    checkbox.dataset.optionIndex = String(index);

                    const optionText = document.createElement('span');
                    optionText.className = 'program-filter-multiselect__option-text text-sm text-primary-light';
                    optionText.textContent = option.textContent;
                    row.append(checkbox, optionText);
                    optionsContainer.appendChild(row);

                    checkbox.addEventListener('change', function () {
                        option.selected = checkbox.checked;
                        row.setAttribute('aria-selected', checkbox.checked ? 'true' : 'false');
                        select.dispatchEvent(new Event('change', { bubbles: true }));
                        renderSummary();
                    });

                    return { row, checkbox, option };
                });

                function renderSummary() {
                    summary.replaceChildren();
                    const selected = Array.from(select.selectedOptions);
                    if (selected.length === 0) {
                        const placeholder = document.createElement('span');
                        placeholder.className = 'program-filter-multiselect__placeholder text-neutral-500 text-truncate';
                        placeholder.textContent = select.dataset.placeholder || '';
                        summary.appendChild(placeholder);
                        return;
                    }

                    selected.slice(0, 2).forEach(option => {
                        const chip = document.createElement('span');
                        chip.className = 'program-filter-multiselect__chip badge bg-primary-50 text-primary-600 rounded-pill';
                        chip.textContent = option.textContent;
                        summary.appendChild(chip);
                    });

                    if (selected.length > 2) {
                        const more = document.createElement('span');
                        more.className = 'program-filter-multiselect__more badge bg-neutral-200 text-neutral-700 rounded-pill';
                        more.textContent = `+${selected.length - 2}`;
                        summary.appendChild(more);
                    }
                }

                function synchronizeFromSelect() {
                    optionRows.forEach(({ checkbox, option, row }) => {
                        checkbox.checked = option.selected;
                        row.setAttribute('aria-selected', option.selected ? 'true' : 'false');
                    });
                    renderSummary();
                }

                function setAllVisible(selected) {
                    optionRows.forEach(({ row, checkbox, option }) => {
                        if (row.classList.contains('d-none') || checkbox.disabled) return;
                        checkbox.checked = selected;
                        option.selected = selected;
                        row.setAttribute('aria-selected', selected ? 'true' : 'false');
                    });
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    renderSummary();
                }

                toggle.addEventListener('click', function () {
                    const willOpen = !wrapper.classList.contains('is-open');
                    closeMultiSelects(wrapper);
                    wrapper.classList.toggle('is-open', willOpen);
                    menu.classList.toggle('d-none', !willOpen);
                    toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
                    if (willOpen) window.setTimeout(() => search.focus(), 0);
                });

                search.addEventListener('input', function () {
                    const query = normalizeText(search.value);
                    let visibleCount = 0;
                    optionRows.forEach(({ row }) => {
                        const visible = !query || row.dataset.searchText.includes(query);
                        row.classList.toggle('d-none', !visible);
                        if (visible) visibleCount += 1;
                    });
                    noOptions.classList.toggle('d-none', visibleCount > 0);
                });

                selectAllButton.addEventListener('click', () => setAllVisible(true));
                clearButton.addEventListener('click', () => setAllVisible(false));
                select.addEventListener('change', synchronizeFromSelect);

                menu.append(searchWrap, toolbar, optionsContainer, noOptions);
                wrapper.append(toggle, menu);
                synchronizeFromSelect();
            });
    }

    initializeMultiSelects(document);

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.program-filter-multiselect')) closeMultiSelects();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') closeMultiSelects();
    });

    const candidates = Array.from(
        document.querySelectorAll('#abstractBookCandidatesData [data-abstract-book-candidate]')
    );
    const countElements = [
        document.getElementById('abstractBookFilteredCount'),
        document.getElementById('abstractBookActionCount')
    ].filter(Boolean);
    const noCandidates = document.getElementById('abstractBookNoCandidates');
    const exportButtons = Array.from(document.querySelectorAll('.js-abstract-export-button'));

    function selectedValues(name) {
        const select = form.querySelector(`[name="${name}"]`);
        if (!select) return new Set();
        return new Set(Array.from(select.selectedOptions).map(option => option.value.toLowerCase()));
    }

    function updateFilterPreview() {
        const preset = Number.parseInt(
            form.querySelector('[name="Export.SubmissionScopePreset"]')?.value || '1',
            10
        );
        const workflowStatuses = selectedValues('Export.WorkflowStatusCodes');
        const paymentStatuses = selectedValues('Export.PaymentStatusIds');
        const submissionTypes = selectedValues('Export.SubmissionTypeIds');
        const topics = selectedValues('Export.TopicIds');
        const search = normalizeText(
            form.querySelector('[name="Export.SubmissionSearchText"]')?.value || ''
        );

        const count = candidates.filter(candidate => {
            const isAccepted = candidate.dataset.isAccepted === 'true';
            const isPaid = candidate.dataset.isPaid === 'true';
            if (preset === 1 && !isAccepted) return false;
            if (preset === 2 && !isPaid) return false;
            if (preset === 3 && (!isAccepted || !isPaid)) return false;

            const workflow = (candidate.dataset.workflowStatus || '').toLowerCase();
            const payment = (candidate.dataset.paymentStatusId || '').toLowerCase();
            const type = (candidate.dataset.submissionTypeId || '').toLowerCase();
            const topic = (candidate.dataset.topicId || '').toLowerCase();

            if (workflowStatuses.size > 0 && !workflowStatuses.has(workflow)) return false;
            if (paymentStatuses.size > 0 && !paymentStatuses.has(payment)) return false;
            if (submissionTypes.size > 0 && !submissionTypes.has(type)) return false;
            if (topics.size > 0 && !topics.has(topic)) return false;

            return !search || normalizeText(candidate.dataset.search || '').includes(search);
        }).length;

        countElements.forEach(element => { element.textContent = String(count); });
        noCandidates?.classList.toggle('d-none', count > 0);
        exportButtons.forEach(button => {
            button.classList.toggle('is-disabled', count === 0);
            button.setAttribute('aria-disabled', count === 0 ? 'true' : 'false');
        });
        return count;
    }

    document.querySelectorAll('.js-abstract-book-filter').forEach(element => {
        element.addEventListener('input', updateFilterPreview);
        element.addEventListener('change', updateFilterPreview);
    });

    document.getElementById('clearAbstractBookFilters')?.addEventListener('click', function () {
        [
            'Export.WorkflowStatusCodes',
            'Export.PaymentStatusIds',
            'Export.SubmissionTypeIds',
            'Export.TopicIds'
        ].forEach(name => {
            const select = form.querySelector(`[name="${name}"]`);
            if (!select) return;
            Array.from(select.options).forEach(option => { option.selected = false; });
            select.dispatchEvent(new Event('change', { bubbles: true }));
        });

        const search = form.querySelector('[name="Export.SubmissionSearchText"]');
        if (search) search.value = '';
        updateFilterPreview();
    });

    function showError(message) {
        if (window.Swal) {
            window.Swal.fire({
                icon: 'warning',
                title: root.dataset.validationTitle || 'Kontrol gerekli',
                text: message
            });
            return;
        }
        window.alert(message);
    }

    function validateExportRequest() {
        const count = updateFilterPreview();
        if (count === 0) {
            showError(root.dataset.noResultsMessage || 'Seçilen filtrelere uygun bildiri bulunamadı.');
            return false;
        }

        const includeTurkish = form.querySelector('[name="Export.IncludeTurkishContent"]')?.checked;
        const includeEnglish = form.querySelector('[name="Export.IncludeEnglishContent"]')?.checked;
        if (!includeTurkish && !includeEnglish) {
            showError(root.dataset.contentLanguageMessage || 'En az bir içerik dili seçilmelidir.');
            return false;
        }

        if (!form.checkValidity()) {
            form.reportValidity();
            return false;
        }

        return true;
    }

    const coverInput = form.querySelector('[name="Export.CoverImageFile"]');
    const coverSelectedInput = document.getElementById('abstractBookCoverImageSelected');

    function synchronizeCoverSelection() {
        const hasSelectedFile = Boolean(coverInput?.files?.length);
        if (coverSelectedInput) {
            coverSelectedInput.value = hasSelectedFile ? 'true' : 'false';
        }
    }

    coverInput?.addEventListener('change', synchronizeCoverSelection);
    synchronizeCoverSelection();

    function validateCoverFile() {
        synchronizeCoverSelection();
        const coverFile = coverInput?.files?.[0];
        if (!coverFile) return true;

        const maxBytes = 8 * 1024 * 1024;
        if (coverFile.size <= 0) {
            showError('Kapak görseli boş görünüyor. Başka bir PNG veya JPG dosyası seçin.');
            return false;
        }

        if (coverFile.size > maxBytes) {
            showError('Kapak görseli en fazla 8 MB olabilir.');
            return false;
        }

        const name = (coverFile.name || '').toLocaleLowerCase('en-US');
        const type = (coverFile.type || '').toLocaleLowerCase('en-US');
        const supportedExtension = name.endsWith('.png')
            || name.endsWith('.jpg')
            || name.endsWith('.jpeg');
        const supportedMimeType = type === 'image/png' || type === 'image/jpeg';

        if (!supportedExtension && !supportedMimeType) {
            showError('Kapak için yalnızca PNG veya JPG dosyası yükleyebilirsiniz.');
            return false;
        }

        return true;
    }

    // Dosya çıktıları fetch/blob ile indirilmez. Firefox ve ters proxy katmanında
    // dosya başarıyla oluşmasına rağmen görülen yanıltıcı "NetworkError" hatasını
    // engellemek için tarayıcının doğal multipart form indirme akışı kullanılır.
    form.addEventListener('submit', function (event) {
        if (!validateExportRequest() || !validateCoverFile()) {
            event.preventDefault();
        }
    });

    updateFilterPreview();
})();
