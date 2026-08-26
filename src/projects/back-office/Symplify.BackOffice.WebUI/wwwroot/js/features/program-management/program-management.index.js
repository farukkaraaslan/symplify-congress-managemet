(function ($) {
    'use strict';

    const root = document.getElementById('programManagementRoot');
    if (!root) return;

    const congressId = root.dataset.congressId;
    const token = document.querySelector('#programAjaxTokenForm input[name="__RequestVerificationToken"]')?.value || '';
    const activeDayStorageKey = `program-management-active-day:${congressId}`;

    const programBookExportForm = document.getElementById('programBookExportForm');
    const programBookCoverInput = programBookExportForm?.querySelector('[data-program-book-cover-input]');
    const programBookCoverSelected = programBookExportForm?.querySelector('[data-program-book-cover-selected]');

    function synchronizeProgramBookCoverSelection() {
        if (!programBookCoverSelected) return;
        programBookCoverSelected.value = programBookCoverInput?.files?.length > 0 ? 'true' : 'false';
    }

    if (programBookCoverInput) {
        programBookCoverInput.addEventListener('change', synchronizeProgramBookCoverSelection);
    }

    if (programBookExportForm) {
        programBookExportForm.addEventListener('submit', synchronizeProgramBookCoverSelection);
    }

    const questionAnswerToggle = document.querySelector('[data-question-answer-toggle]');
    const questionAnswerDuration = document.querySelector('[data-question-answer-duration]');
    const questionAnswerDurationWrapper = document.querySelector('[data-question-answer-duration-wrapper]');

    function synchronizeQuestionAnswerSettings() {
        if (!questionAnswerToggle || !questionAnswerDuration) return;

        const enabled = questionAnswerToggle.checked;
        questionAnswerDuration.disabled = !enabled;
        questionAnswerDuration.setAttribute('aria-disabled', enabled ? 'false' : 'true');
        questionAnswerDurationWrapper?.classList.toggle('opacity-50', !enabled);
    }

    questionAnswerToggle?.addEventListener('change', synchronizeQuestionAnswerSettings);
    synchronizeQuestionAnswerSettings();

    const sessionBreakToggle = document.querySelector('[data-session-break-toggle]');
    const sessionBreakDuration = document.querySelector('[data-session-break-duration]');
    const sessionBreakDurationWrapper = document.querySelector('[data-session-break-duration-wrapper]');

    function synchronizeSessionBreakSettings() {
        if (!sessionBreakToggle || !sessionBreakDuration) return;

        const enabled = sessionBreakToggle.checked;
        sessionBreakDuration.disabled = !enabled;
        sessionBreakDuration.setAttribute('aria-disabled', enabled ? 'false' : 'true');
        sessionBreakDurationWrapper?.classList.toggle('opacity-50', !enabled);
    }

    sessionBreakToggle?.addEventListener('change', synchronizeSessionBreakSettings);
    synchronizeSessionBreakSettings();


    function normalizeFilterOptionText(value) {
        return (value || '')
            .toLocaleLowerCase(document.documentElement.lang || undefined)
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '');
    }

    function closeProgramMultiSelects(exceptWrapper) {
        document.querySelectorAll('.program-filter-multiselect.is-open').forEach(wrapper => {
            if (wrapper === exceptWrapper) return;
            wrapper.classList.remove('is-open');
            wrapper.querySelector('.program-filter-multiselect__menu')?.classList.add('d-none');
            wrapper.querySelector('.program-filter-multiselect__toggle')?.setAttribute('aria-expanded', 'false');
        });
    }

    function initializeProgramFilterMultiSelects(scope) {
        const container = scope || document;

        container.querySelectorAll('select.js-program-filter-multi[multiple]').forEach(select => {
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
                row.dataset.searchText = normalizeFilterOptionText(option.textContent);
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
                closeProgramMultiSelects(wrapper);
                wrapper.classList.toggle('is-open', willOpen);
                toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
                if (willOpen) {
                    window.setTimeout(() => search.focus(), 0);
                }
            });

            search.addEventListener('input', function () {
                const query = normalizeFilterOptionText(search.value);
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

    initializeProgramFilterMultiSelects(document);

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.program-filter-multiselect')) {
            closeProgramMultiSelects();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        closeProgramMultiSelects();
    });

    function getOkButtonText() {
        const lang = (document.documentElement.getAttribute('lang') || '').toLowerCase();
        return lang.startsWith('en') ? 'OK' : 'Tamam';
    }

    function showError(message) {
        const resolvedMessage = message || 'Beklenmeyen bir hata oluştu.';

        if (window.Swal) {
            return Swal.fire({
                icon: 'error',
                title: 'İşlem tamamlanamadı',
                text: resolvedMessage,
                showConfirmButton: true,
                confirmButtonText: getOkButtonText(),
                allowOutsideClick: false,
                allowEscapeKey: true,
                timer: null,
                timerProgressBar: false
            });
        }

        window.alert(resolvedMessage);
        return Promise.resolve();
    }

    function reloadAfterError(message) {
        return showError(message).then(() => window.location.reload());
    }

    function postJson(url, payload) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        }).then(async response => {
            const data = await response.json().catch(() => ({}));
            if (!response.ok || data.success === false) {
                throw new Error(data.message || 'İşlem tamamlanamadı.');
            }
            return data;
        });
    }

    const submissionCandidateElements = Array.from(
        document.querySelectorAll('#programSubmissionCandidatesData [data-program-candidate]')
    );
    const filteredSubmissionCountElement = document.getElementById('programFilteredSubmissionCount');
    const noFilterCandidatesElement = document.getElementById('programNoFilterCandidates');
    const clearSubmissionFiltersButton = document.getElementById('clearProgramSubmissionFilters');

    function getSelectedFilterValues(form, name) {
        const select = form?.querySelector(`[name="${name}"]`);
        if (!select) return new Set();
        return new Set(Array.from(select.selectedOptions).map(option => option.value.toLowerCase()));
    }

    function updateSubmissionFilterPreview() {
        const form = document.getElementById('programGenerateForm');
        if (!form || submissionCandidateElements.length === 0) {
            if (filteredSubmissionCountElement) filteredSubmissionCountElement.textContent = '0';
            if (noFilterCandidatesElement) noFilterCandidatesElement.classList.remove('d-none');
            return 0;
        }

        const preset = Number.parseInt(
            form.querySelector('[name="Generate.SubmissionScopePreset"]')?.value || '1',
            10
        );
        const workflowStatuses = getSelectedFilterValues(form, 'Generate.WorkflowStatusCodes');
        const paymentStatuses = getSelectedFilterValues(form, 'Generate.PaymentStatusIds');
        const submissionTypes = getSelectedFilterValues(form, 'Generate.SubmissionTypeIds');
        const topics = getSelectedFilterValues(form, 'Generate.TopicIds');
        const search = normalizeSearchText(
            form.querySelector('[name="Generate.SubmissionSearchText"]')?.value || ''
        );

        const count = submissionCandidateElements.filter(candidate => {
            const isAccepted = candidate.dataset.isAccepted === 'true';
            const isPaid = candidate.dataset.isPaid === 'true';

            if (preset === 1 && !isAccepted) return false;
            if (preset === 2 && !isPaid) return false;
            if (preset === 3 && (!isAccepted || !isPaid)) return false;

            const workflowStatus = (candidate.dataset.workflowStatus || '').toLowerCase();
            const paymentStatusId = (candidate.dataset.paymentStatusId || '').toLowerCase();
            const submissionTypeId = (candidate.dataset.submissionTypeId || '').toLowerCase();
            const topicId = (candidate.dataset.topicId || '').toLowerCase();

            if (workflowStatuses.size > 0 && !workflowStatuses.has(workflowStatus)) return false;
            if (paymentStatuses.size > 0 && !paymentStatuses.has(paymentStatusId)) return false;
            if (submissionTypes.size > 0 && !submissionTypes.has(submissionTypeId)) return false;
            if (topics.size > 0 && !topics.has(topicId)) return false;

            if (search) {
                const haystack = normalizeSearchText(candidate.dataset.search || '');
                if (!haystack.includes(search)) return false;
            }

            return true;
        }).length;

        if (filteredSubmissionCountElement) filteredSubmissionCountElement.textContent = String(count);
        if (noFilterCandidatesElement) noFilterCandidatesElement.classList.toggle('d-none', count > 0);
        return count;
    }

    document.querySelectorAll('.js-program-submission-filter').forEach(element => {
        element.addEventListener('input', updateSubmissionFilterPreview);
        element.addEventListener('change', updateSubmissionFilterPreview);
    });

    if (clearSubmissionFiltersButton) {
        clearSubmissionFiltersButton.addEventListener('click', function () {
            const form = document.getElementById('programGenerateForm');
            if (!form) return;

            ['Generate.WorkflowStatusCodes', 'Generate.PaymentStatusIds', 'Generate.SubmissionTypeIds', 'Generate.TopicIds']
                .forEach(name => {
                    const select = form.querySelector(`[name="${name}"]`);
                    if (!select) return;
                    Array.from(select.options).forEach(option => { option.selected = false; });
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                });

            const searchInput = form.querySelector('[name="Generate.SubmissionSearchText"]');
            if (searchInput) searchInput.value = '';
            updateSubmissionFilterPreview();
        });
    }


    const sessionOfficialsForm = document.getElementById('sessionOfficialsForm');
    const sessionOfficialsSessionId = document.getElementById('sessionOfficialsSessionId');
    const sessionOfficialsModalTitle = document.getElementById('sessionOfficialsModalTitle');
    const sessionChairOfficialKey = document.getElementById('sessionChairOfficialKey');
    const sessionViceChairOfficialKey = document.getElementById('sessionViceChairOfficialKey');
    const sessionOfficialSearch = document.getElementById('sessionOfficialSearch');
    const sessionOfficialNoResults = document.getElementById('sessionOfficialNoResults');
    const saveSessionOfficialsButton = document.getElementById('saveSessionOfficialsButton');
    const officialScopeButtons = Array.from(document.querySelectorAll('[data-official-scope]'));
    const notAssignedLabel = sessionChairOfficialKey?.querySelector('option[value=""]')?.textContent || 'Atanmadı';

    const officialCandidates = Array.from(
        document.querySelectorAll('#programOfficialCandidatesData [data-official-key]')
    ).map(element => {
        const parsedTitleOrder = Number.parseInt(element.dataset.officialTitleOrder || '', 10);

        return {
            key: element.dataset.officialKey || '',
            source: element.dataset.officialSource || '',
            id: element.dataset.officialId || '',
            name: element.dataset.officialName || '',
            institution: element.dataset.officialInstitution || '',
            email: element.dataset.officialEmail || '',
            titleOrder: Number.isFinite(parsedTitleOrder) && parsedTitleOrder > 0
                ? parsedTitleOrder
                : Number.MAX_SAFE_INTEGER
        };
    }).filter(candidate => candidate.key && candidate.id && candidate.name);

    let activeOfficialScope = 'session';
    let currentSessionAuthorIds = new Set();

    function normalizeSearchText(value) {
        return (value || '')
            .toLocaleLowerCase('tr-TR')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .trim();
    }

    function formatOfficialLabel(candidate) {
        return candidate.institution
            ? `${candidate.name} — ${candidate.institution}`
            : candidate.name;
    }

    function candidateMatchesScope(candidate) {
        if (activeOfficialScope === 'board') return candidate.source === 'board';
        if (activeOfficialScope === 'all') return candidate.source === 'author';
        return candidate.source === 'author' && currentSessionAuthorIds.has(candidate.id.toLowerCase());
    }

    function getFilteredOfficialCandidates() {
        const search = normalizeSearchText(sessionOfficialSearch?.value);

        return officialCandidates
            .filter(candidate => candidateMatchesScope(candidate))
            .filter(candidate => {
                if (!search) return true;
                const haystack = normalizeSearchText(
                    `${candidate.name} ${candidate.institution} ${candidate.email}`
                );
                return haystack.includes(search);
            })
            .sort((left, right) => {
                const titleOrderCompare = left.titleOrder - right.titleOrder;
                if (titleOrderCompare !== 0) return titleOrderCompare;

                const nameCompare = left.name.localeCompare(right.name, 'tr-TR', { sensitivity: 'base' });
                if (nameCompare !== 0) return nameCompare;
                return left.institution.localeCompare(right.institution, 'tr-TR', { sensitivity: 'base' });
            });
    }

    function rebuildOfficialSelect(select, selectedKey, candidates) {
        if (!select) return;

        const selectedCandidate = officialCandidates.find(candidate => candidate.key === selectedKey);
        const candidateMap = new Map(candidates.map(candidate => [candidate.key, candidate]));
        if (selectedCandidate && !candidateMap.has(selectedCandidate.key)) {
            candidateMap.set(selectedCandidate.key, selectedCandidate);
        }

        select.innerHTML = '';
        const emptyOption = document.createElement('option');
        emptyOption.value = '';
        emptyOption.textContent = notAssignedLabel;
        select.appendChild(emptyOption);

        Array.from(candidateMap.values()).forEach(candidate => {
            const option = document.createElement('option');
            option.value = candidate.key;
            option.textContent = formatOfficialLabel(candidate);
            option.dataset.source = candidate.source;
            select.appendChild(option);
        });

        select.value = selectedKey || '';
    }

    function refreshOfficialCandidates() {
        const chairSelectedKey = sessionChairOfficialKey?.value || '';
        const viceSelectedKey = sessionViceChairOfficialKey?.value || '';
        const candidates = getFilteredOfficialCandidates();

        rebuildOfficialSelect(sessionChairOfficialKey, chairSelectedKey, candidates);
        rebuildOfficialSelect(sessionViceChairOfficialKey, viceSelectedKey, candidates);

        if (sessionOfficialNoResults) {
            sessionOfficialNoResults.classList.toggle('d-none', candidates.length > 0);
        }
    }

    function setOfficialScope(scope) {
        activeOfficialScope = scope || 'session';
        officialScopeButtons.forEach(button => {
            const isActive = button.dataset.officialScope === activeOfficialScope;
            button.classList.toggle('active', isActive);
            button.classList.toggle('btn-primary-600', isActive);
            button.classList.toggle('btn-outline-primary-600', !isActive);
        });
        refreshOfficialCandidates();
    }

    function parseOfficialKey(value) {
        if (!value) {
            return { authorId: null, boardMemberId: null };
        }

        const separatorIndex = value.indexOf(':');
        if (separatorIndex <= 0) {
            return { authorId: null, boardMemberId: null };
        }

        const source = value.substring(0, separatorIndex);
        const id = value.substring(separatorIndex + 1);
        if (!id) {
            return { authorId: null, boardMemberId: null };
        }

        return source === 'board'
            ? { authorId: null, boardMemberId: id }
            : { authorId: id, boardMemberId: null };
    }

    officialScopeButtons.forEach(button => {
        button.addEventListener('click', function () {
            setOfficialScope(this.dataset.officialScope || 'session');
        });
    });

    if (sessionOfficialSearch) {
        sessionOfficialSearch.addEventListener('input', refreshOfficialCandidates);
    }

    document.addEventListener('click', function (event) {
        const button = event.target.closest('.js-edit-session-officials');
        if (!button || !sessionOfficialsForm) return;

        sessionOfficialsSessionId.value = button.dataset.sessionId || '';
        sessionOfficialsModalTitle.textContent = button.dataset.sessionTitle || '';
        currentSessionAuthorIds = new Set(
            (button.dataset.sessionAuthorIds || '')
                .split(',')
                .map(value => value.trim().toLowerCase())
                .filter(Boolean)
        );

        if (sessionOfficialSearch) sessionOfficialSearch.value = '';
        setOfficialScope('session');

        const chairKey = button.dataset.chairOfficialKey || '';
        const viceChairKey = button.dataset.viceChairOfficialKey || '';
        rebuildOfficialSelect(sessionChairOfficialKey, chairKey, getFilteredOfficialCandidates());
        rebuildOfficialSelect(sessionViceChairOfficialKey, viceChairKey, getFilteredOfficialCandidates());
    });

    if (sessionOfficialsForm) {
        sessionOfficialsForm.addEventListener('submit', function (event) {
            event.preventDefault();

            const sessionId = sessionOfficialsSessionId?.value || '';
            const chairOfficialKey = sessionChairOfficialKey?.value || '';
            const viceChairOfficialKey = sessionViceChairOfficialKey?.value || '';

            if (!sessionId) {
                showError('Oturum bilgisi bulunamadı.');
                return;
            }

            if (chairOfficialKey && viceChairOfficialKey && chairOfficialKey === viceChairOfficialKey) {
                showError(root.dataset.sameOfficialError || 'Oturum başkanı ile başkan yardımcısı aynı kişi olamaz.');
                return;
            }

            const chair = parseOfficialKey(chairOfficialKey);
            const viceChair = parseOfficialKey(viceChairOfficialKey);

            rememberActiveDay();
            saveSessionOfficialsButton.disabled = true;

            postJson(root.dataset.sessionOfficialsUrl, {
                congressId: congressId,
                sessionId: sessionId,
                chairAuthorId: chair.authorId,
                chairBoardMemberId: chair.boardMemberId,
                viceChairAuthorId: viceChair.authorId,
                viceChairBoardMemberId: viceChair.boardMemberId
            })
                .then(() => window.location.reload())
                .catch(error => {
                    saveSessionOfficialsButton.disabled = false;
                    showError(error.message);
                });
        });
    }

    function rememberActiveDay(targetSelector) {
        const selector = targetSelector
            || document.querySelector('.program-day-tabs .nav-link.active')?.getAttribute('data-bs-target');
        if (selector) sessionStorage.setItem(activeDayStorageKey, selector);
    }

    function restoreActiveDay() {
        const selector = sessionStorage.getItem(activeDayStorageKey);
        if (!selector || !window.bootstrap?.Tab) return;

        const button = document.querySelector(`.program-day-tabs .nav-link[data-bs-target="${selector}"]`);
        if (button) bootstrap.Tab.getOrCreateInstance(button).show();
    }

    document.querySelectorAll('.program-day-tabs .nav-link').forEach(button => {
        button.addEventListener('shown.bs.tab', function () {
            rememberActiveDay(this.getAttribute('data-bs-target'));
        });
    });

    let dragInProgress = false;
    let allowDaySwitch = false;
    let daySwitchTimer = null;

    function clearDaySwitchTimer() {
        if (daySwitchTimer) {
            window.clearTimeout(daySwitchTimer);
            daySwitchTimer = null;
        }

        document.querySelectorAll('.program-day-tabs .nav-link.is-drag-hover')
            .forEach(button => {
                button.classList.remove('is-drag-hover', 'border', 'border-primary-600', 'bg-primary-50', 'text-primary-600');
            });
    }

    function refreshSortablePositions() {
        if (!$.fn.sortable) return;
        window.setTimeout(() => {
            $('.program-item-list').sortable('refresh');
            $('.program-item-list').sortable('refreshPositions');
            $('.program-room-timeline').sortable('refresh');
            $('.program-room-timeline').sortable('refreshPositions');
        }, 0);
    }

    function initializeDayTabDragSwitch() {
        document.querySelectorAll('.program-day-tabs .nav-link').forEach(button => {
            button.addEventListener('mouseenter', function () {
                if (!dragInProgress || !allowDaySwitch || this.classList.contains('active')) return;

                clearDaySwitchTimer();
                this.classList.add('is-drag-hover', 'border', 'border-primary-600', 'bg-primary-50', 'text-primary-600');
                daySwitchTimer = window.setTimeout(() => {
                    const targetSelector = this.getAttribute('data-bs-target');
                    if (!targetSelector || !window.bootstrap?.Tab) return;

                    rememberActiveDay(targetSelector);
                    bootstrap.Tab.getOrCreateInstance(this).show();
                    clearDaySwitchTimer();
                }, 450);
            });

            button.addEventListener('mouseleave', clearDaySwitchTimer);
            button.addEventListener('shown.bs.tab', function () {
                if (dragInProgress) refreshSortablePositions();
            });
        });
    }

    function initializeSortable() {
        if (!$.fn.sortable) return;

        $('.program-item-list').sortable({
            connectWith: '.program-item-list',
            items: '> .program-item:not(.is-locked)',
            cancel: 'input, button, a, select, textarea',
            handle: '.program-drag-handle',
            placeholder: 'program-item-placeholder border border-primary-300 border-dashed radius-8 bg-primary-50 h-72-px',
            tolerance: 'pointer',
            helper: 'clone',
            appendTo: document.body,
            forcePlaceholderSize: true,
            zIndex: 2000,
            start: function (event, ui) {
                dragInProgress = true;
                allowDaySwitch = true;
                document.body.classList.add('program-dragging');
                ui.item.addClass('is-dragging');
                ui.item.data('source-list', this);
                ui.item.data('source-index', ui.item.index());
                ui.helper.css('width', `${ui.item.outerWidth()}px`);
            },
            over: function () {
                $(this).closest('.program-session-card').addClass('is-drop-target border-primary-600 shadow-sm');
            },
            out: function () {
                $(this).closest('.program-session-card').removeClass('is-drop-target border-primary-600 shadow-sm');
            },
            stop: function (event, ui) {
                dragInProgress = false;
                allowDaySwitch = false;
                clearDaySwitchTimer();
                document.body.classList.remove('program-dragging');
                $('.program-session-card').removeClass('is-drop-target border-primary-600 shadow-sm');
                ui.item.removeClass('is-dragging');

                const $finalList = ui.item.closest('.program-item-list');
                if (!$finalList.length) {
                    window.location.reload();
                    return;
                }

                const sourceList = ui.item.data('source-list');
                const sourceIndex = Number(ui.item.data('source-index'));
                const finalIndex = ui.item.index();
                const listChanged = sourceList !== $finalList.get(0);
                const orderChanged = sourceIndex !== finalIndex;

                if (!listChanged && !orderChanged) return;

                const paneId = $finalList.closest('.tab-pane').attr('id');
                if (paneId) rememberActiveDay(`#${paneId}`);
                persistOrder($finalList, ui.item);
            }
        }).disableSelection();
    }

    function initializeBreakSortable() {
        if (!$.fn.sortable) return;

        $('.program-room-timeline').sortable({
            items: '> .program-timeline-persisted-block',
            handle: '.program-fixed-drag-handle',
            cancel: 'input, button, a, select, textarea',
            placeholder: 'program-break-placeholder border border-warning-300 border-dashed radius-8 bg-warning-50 h-56-px',
            tolerance: 'pointer',
            forcePlaceholderSize: true,
            zIndex: 1900,
            start: function (event, ui) {
                if (!ui.item.hasClass('program-fixed-break') || !ui.item.hasClass('is-movable')) {
                    $(this).sortable('cancel');
                    return;
                }

                dragInProgress = true;
                allowDaySwitch = false;
                document.body.classList.add('program-break-dragging');
                ui.item.addClass('is-dragging');
                ui.item.data('source-index', ui.item.index());
            },
            stop: function (event, ui) {
                dragInProgress = false;
                allowDaySwitch = false;
                clearDaySwitchTimer();
                document.body.classList.remove('program-break-dragging');
                ui.item.removeClass('is-dragging');

                const sourceIndex = Number(ui.item.data('source-index'));
                const finalIndex = ui.item.index();
                if (sourceIndex === finalIndex) return;

                const $timeline = ui.item.closest('.program-room-timeline');
                const paneId = $timeline.closest('.tab-pane').attr('id');
                if (paneId) rememberActiveDay(`#${paneId}`);
                persistBreakOrder($timeline, ui.item);
            }
        }).disableSelection();
    }

    let breakInsertRequestInProgress = false;

    function initializeBreakInsertionDragDrop() {
        if (!$.fn.draggable || !$.fn.droppable) return;

        $('.program-break-drop-zone').droppable({
            tolerance: 'pointer',
            accept: function (candidate) {
                if (breakInsertRequestInProgress) return false;

                const $candidate = $(candidate);
                return $candidate.hasClass('js-break-insert-source');
            },
            over: function () {
                $(this).addClass('is-break-drop-hover border-primary-600 bg-primary-50');
            },
            out: function () {
                $(this).removeClass('is-break-drop-hover border-primary-600 bg-primary-50');
            },
            drop: function (event, ui) {
                if (breakInsertRequestInProgress) return;

                const $zone = $(this);
                const $list = $zone.closest('.program-item-list');
                const $source = $(ui.draggable);
                breakInsertRequestInProgress = true;
                rememberActiveDay();

                postJson(root.dataset.breakReorderUrl, {
                    congressId: congressId,
                    programDayId: $list.data('program-day-id'),
                    eventRoomId: $list.data('room-id'),
                    breakId: $source.data('break-id'),
                    targetSessionId: $zone.data('target-session-id'),
                    targetItemIndex: Number.parseInt($zone.attr('data-target-item-index'), 10),
                    orderedBlockKeys: []
                })
                    .then(() => window.location.reload())
                    .catch(error => {
                        breakInsertRequestInProgress = false;
                        reloadAfterError(error.message);
                    });
            }
        });

        $('.js-break-insert-source').draggable({
            handle: '.program-break-insert-handle',
            helper: 'clone',
            appendTo: document.body,
            revert: 'invalid',
            revertDuration: 150,
            scroll: true,
            zIndex: 3000,
            start: function () {
                dragInProgress = true;
                allowDaySwitch = true;
                document.body.classList.add('program-break-insert-dragging');
                $('.program-break-drop-zone')
                    .removeClass('d-none')
                    .addClass('is-break-drop-enabled d-block p-12 my-1 border border-primary-300 border-dashed radius-8 bg-neutral-50');
            },
            stop: function () {
                dragInProgress = false;
                allowDaySwitch = false;
                clearDaySwitchTimer();
                document.body.classList.remove('program-break-insert-dragging');
                $('.program-break-drop-zone')
                    .addClass('d-none')
                    .removeClass('is-break-drop-enabled is-break-drop-hover d-block p-12 my-1 border border-primary-300 border-primary-600 border-dashed radius-8 bg-neutral-50 bg-primary-50');
            }
        });
    }

    let breakOrderRequestInProgress = false;

    function persistBreakOrder($timeline, $break) {
        if (breakOrderRequestInProgress || !$timeline?.length || !$break?.length) return;
        breakOrderRequestInProgress = true;

        const payload = {
            congressId: congressId,
            programDayId: $timeline.data('program-day-id'),
            eventRoomId: $timeline.data('room-id'),
            breakId: $break.data('break-id'),
            orderedBlockKeys: $timeline
                .children('.program-timeline-persisted-block[data-block-key]')
                .map(function () { return $(this).attr('data-block-key'); })
                .get()
        };

        postJson(root.dataset.breakReorderUrl, payload)
            .then(() => window.location.reload())
            .catch(error => {
                breakOrderRequestInProgress = false;
                reloadAfterError(error.message);
            });
    }

    let orderRequestInProgress = false;

    function persistOrder($list, $item) {
        if (orderRequestInProgress || !$item?.length) return;
        orderRequestInProgress = true;
        rememberActiveDay();

        const payload = {
            congressId: congressId,
            movedItemId: $item.data('item-id'),
            targetSessionId: $list.data('session-id'),
            orderedItemIds: $list.children('.program-item').map(function () {
                return $(this).data('item-id');
            }).get()
        };

        postJson(root.dataset.reorderUrl, payload)
            .then(() => window.location.reload())
            .catch(error => {
                orderRequestInProgress = false;
                reloadAfterError(error.message);
            });
    }

    let breakDeleteRequestInProgress = false;

    $(document).on('click', '.js-delete-break', function (event) {
        event.preventDefault();
        event.stopPropagation();
        if (breakDeleteRequestInProgress) return;

        const button = this;
        const breakId = button.dataset.breakId;
        if (!breakId) return;

        const removeBreak = function () {
            breakDeleteRequestInProgress = true;
            button.disabled = true;
            rememberActiveDay();

            postJson(root.dataset.breakDeleteUrl, {
                congressId: congressId,
                breakId: breakId
            })
                .then(() => window.location.reload())
                .catch(error => {
                    breakDeleteRequestInProgress = false;
                    button.disabled = false;
                    showError(error.message);
                });
        };

        if (!window.Swal) {
            if (window.confirm(root.dataset.removeBreakText || 'Bu ara programdan kaldırılacaktır.'))
                removeBreak();
            return;
        }

        Swal.fire({
            icon: 'warning',
            title: root.dataset.removeBreakTitle || 'Ara kaldırılsın mı?',
            text: root.dataset.removeBreakText || 'Bu ara programdan tamamen kaldırılacaktır.',
            showCancelButton: true,
            confirmButtonText: root.dataset.removeBreakConfirm || 'Kaldır',
            cancelButtonText: root.dataset.cancelText || 'Vazgeç',
            confirmButtonColor: '#dc3545'
        }).then(result => {
            if (result.isConfirmed) removeBreak();
        });
    });

    $(document).on('change', '.js-duration-input', function () {
        const input = this;
        const item = input.closest('.program-item');
        const duration = Number.parseInt(input.value, 10);
        if (!Number.isInteger(duration) || duration < 5 || duration > 120) {
            showError('Bildiri süresi 5 ile 120 dakika arasında olmalıdır.')
                .then(() => window.location.reload());
            return;
        }

        input.disabled = true;
        rememberActiveDay();
        postJson(root.dataset.durationUrl, {
            congressId: congressId,
            itemId: item.dataset.itemId,
            durationMinutes: duration
        })
            .then(() => window.location.reload())
            .catch(error => {
                input.disabled = false;
                showError(error.message);
            });
    });

    $(document).on('click', '.js-lock-item', function () {
        const button = this;
        const item = button.closest('.program-item');
        button.disabled = true;
        rememberActiveDay();

        postJson(root.dataset.lockUrl, {
            congressId: congressId,
            itemId: item.dataset.itemId
        })
            .then(() => window.location.reload())
            .catch(error => {
                button.disabled = false;
                showError(error.message);
            });
    });

    $(document).on('submit', '.js-reset-program-form', function (event) {
        if (!window.Swal) return;
        event.preventDefault();
        const form = this;
        Swal.fire({
            icon: 'warning',
            title: 'Program taslağı sıfırlansın mı?',
            text: 'Otomatik ve manuel tüm program yerleşimleri kaldırılacaktır.',
            showCancelButton: true,
            confirmButtonText: 'Sıfırla',
            cancelButtonText: 'Vazgeç'
        }).then(result => {
            if (result.isConfirmed) form.submit();
        });
    });

    function parseTime(value) {
        if (!/^\d{2}:\d{2}$/.test(value || '')) return null;
        const parts = value.split(':').map(Number);
        if (parts[0] > 23 || parts[1] > 59) return null;
        return parts[0] * 60 + parts[1];
    }

    function formatTime(value) {
        const normalized = ((value % 1440) + 1440) % 1440;
        return `${String(Math.floor(normalized / 60)).padStart(2, '0')}:${String(normalized % 60).padStart(2, '0')}`;
    }

    function subtractBlocks(start, end, blocks) {
        let windows = [{ start, end }];
        blocks.sort((a, b) => a.start - b.start || a.end - b.end).forEach(block => {
            const next = [];
            windows.forEach(window => {
                if (block.end <= window.start || block.start >= window.end) {
                    next.push(window);
                    return;
                }
                if (block.start > window.start) next.push({ start: window.start, end: block.start });
                if (block.end < window.end) next.push({ start: block.end, end: window.end });
            });
            windows = next;
        });
        return windows;
    }

    function simulateWindow(length, settings) {
        let remaining = length;
        let sessions = 0;
        let capacity = 0;
        const minimumSession = settings.presentation + settings.question + settings.sessionBreak;

        while (remaining > 0) {
            if (remaining < minimumSession) break;

            let sessionMinutes = Math.min(settings.session, remaining);
            sessions += 1;
            capacity += Math.max(0, Math.floor((sessionMinutes - settings.question - settings.sessionBreak) / settings.presentation));
            remaining -= sessionMinutes;
            if (remaining <= 0) break;

            if (settings.breakMinutes <= 0) {
                if (remaining < minimumSession) {
                    capacity += Math.floor(remaining / settings.presentation);
                    remaining = 0;
                }
                continue;
            }

            let breakMinutes = Math.min(settings.breakMinutes, remaining);
            const afterBreak = remaining - breakMinutes;
            if (afterBreak > 0 && afterBreak < minimumSession) breakMinutes = remaining;
            remaining -= breakMinutes;
        }

        return { sessions, capacity };
    }

    function calculateRoomSummary(dayStart, dayEnd, blocks, settings) {
        return subtractBlocks(dayStart, dayEnd, blocks)
            .reduce((summary, window) => {
                const result = simulateWindow(window.end - window.start, settings);
                summary.sessions += result.sessions;
                summary.capacity += result.capacity;
                return summary;
            }, { sessions: 0, capacity: 0 });
    }

    function updateSchedulePreview() {
        const form = document.getElementById('programGenerateForm');
        const preview = document.getElementById('programSchedulePreview');
        const body = preview?.querySelector('.js-program-schedule-preview-body');
        if (!form || !preview || !body) return [];

        const value = name => form.querySelector(`[name="${name}"]`)?.value;
        const checked = name => Boolean(form.querySelector(`[name="${name}"]`)?.checked);
        const integer = name => Number.parseInt(value(name), 10);

        const dayStart = parseTime(value('Generate.DayStartTime'));
        const dayEnd = parseTime(value('Generate.DayEndTime'));
        const lunchStart = parseTime(value('Generate.LunchStartTime'));
        const includeQuestionAnswer = checked('Generate.IncludeQuestionAnswer');
        const includeSessionBreak = checked('Generate.IncludeSessionBreaks');
        const settings = {
            session: integer('Generate.SessionDurationMinutes'),
            presentation: integer('Generate.PresentationDurationMinutes'),
            question: includeQuestionAnswer
                ? integer('Generate.QuestionAnswerDurationMinutes')
                : 0,
            breakMinutes: integer('Generate.BreakDurationMinutes'),
            sessionBreak: includeSessionBreak
                ? integer('Generate.SessionBreakDurationMinutes')
                : 0
        };
        const openingDuration = integer('Generate.OpeningDurationMinutes');
        const lunchDuration = integer('Generate.LunchDurationMinutes');
        const includeOpening = checked('Generate.IncludeOpening');
        const includeLunch = checked('Generate.IncludeLunch');
        const openingRoomId = value('Generate.OpeningRoomId') || '';
        const selectedRooms = Array.from(form.querySelectorAll('input[name="Generate.RoomIds"]:checked'))
            .map(input => input.value);
        const errors = [];

        if (selectedRooms.length === 0) errors.push('En az bir salon seçilmelidir.');
        if (dayStart === null || dayEnd === null || dayEnd <= dayStart) {
            errors.push('Gün bitiş saati başlangıç saatinden sonra olmalıdır.');
        }
        if (!Number.isInteger(settings.session) || settings.session < 30 || settings.session > 360) {
            errors.push('Oturum süresi 30 ile 360 dakika arasında olmalıdır.');
        }
        if (!Number.isInteger(settings.presentation) || settings.presentation < 5 || settings.presentation > 120) {
            errors.push('Bildiri süresi 5 ile 120 dakika arasında olmalıdır.');
        }
        if (includeQuestionAnswer
            && (!Number.isInteger(settings.question) || settings.question < 1 || settings.question > 180)) {
            errors.push('Soru-cevap süresi 1 ile 180 dakika arasında olmalıdır.');
        }
        if (!Number.isInteger(settings.breakMinutes) || settings.breakMinutes < 0 || settings.breakMinutes > 180) {
            errors.push('Oturum arası 0 ile 180 dakika arasında olmalıdır.');
        }
        if (includeSessionBreak
            && (!Number.isInteger(settings.sessionBreak) || settings.sessionBreak < 1 || settings.sessionBreak > 60)) {
            errors.push('Oturum içi ara süresi 1 ile 60 dakika arasında olmalıdır.');
        }
        if (Number.isInteger(settings.session)
            && Number.isInteger(settings.presentation)
            && Number.isInteger(settings.question)
            && Number.isInteger(settings.sessionBreak)
            && settings.presentation + settings.question + settings.sessionBreak > settings.session) {
            errors.push('Oturum süresi en az bir bildiri, varsa oturum içi ara ve soru-cevap süresini karşılamalıdır.');
        }

        let openingEnd = null;
        if (includeOpening) {
            if (!Number.isInteger(openingDuration) || openingDuration <= 0) {
                errors.push('Açılış süresi sıfırdan büyük olmalıdır.');
            } else if (dayStart !== null) {
                openingEnd = dayStart + openingDuration;
                if (dayEnd !== null && openingEnd > dayEnd) errors.push('Açılış bloğu çalışma saatlerinin dışına taşıyor.');
            }
            if (openingRoomId && !selectedRooms.includes(openingRoomId)) {
                errors.push('Açılış salonu seçili salonlardan biri olmalıdır.');
            }
        }

        let lunchEnd = null;
        if (includeLunch) {
            if (lunchStart === null || !Number.isInteger(lunchDuration) || lunchDuration <= 0) {
                errors.push('Öğle arası başlangıç saati ve süresi geçerli olmalıdır.');
            } else {
                lunchEnd = lunchStart + lunchDuration;
                if (dayStart !== null && dayEnd !== null && (lunchStart < dayStart || lunchEnd > dayEnd)) {
                    errors.push('Öğle arası çalışma saatlerinin içinde olmalıdır.');
                }
            }
        }

        if (openingEnd !== null && lunchStart !== null && lunchEnd !== null
            && dayStart < lunchEnd && lunchStart < openingEnd) {
            errors.push('Açılış bloğu ile öğle arası çakışamaz.');
        }

        const uniqueErrors = Array.from(new Set(errors));
        const hasErrors = uniqueErrors.length > 0;
        preview.classList.toggle('is-invalid', hasErrors);
        preview.classList.toggle('is-valid', !hasErrors);
        preview.classList.toggle('border-danger-300', hasErrors);
        preview.classList.toggle('bg-danger-50', hasErrors);
        preview.classList.toggle('border-success-300', !hasErrors);
        preview.classList.toggle('bg-success-50', !hasErrors);

        if (uniqueErrors.length > 0 || dayStart === null || dayEnd === null) {
            body.innerHTML = `<strong>${preview.dataset.invalid || 'Ayarlar geçersiz'}</strong><ul>${uniqueErrors.map(error => `<li>${error}</li>`).join('')}</ul>`;
            return uniqueErrors;
        }

        let firstDaySessions = 0;
        let firstDayCapacity = 0;
        let otherDaySessions = 0;
        let otherDayCapacity = 0;

        selectedRooms.forEach(roomId => {
            const baseBlocks = includeLunch ? [{ start: lunchStart, end: lunchEnd }] : [];
            const firstDayBlocks = baseBlocks.slice();
            if (includeOpening && (!openingRoomId || openingRoomId === roomId)) {
                firstDayBlocks.push({ start: dayStart, end: openingEnd });
            }

            const first = calculateRoomSummary(dayStart, dayEnd, firstDayBlocks, settings);
            const other = calculateRoomSummary(dayStart, dayEnd, baseBlocks, settings);
            firstDaySessions += first.sessions;
            firstDayCapacity += first.capacity;
            otherDaySessions += other.sessions;
            otherDayCapacity += other.capacity;
        });

        const dayCount = Math.max(1, Number.parseInt(root.dataset.programDayCount || '1', 10));
        const firstDayLabel = preview.dataset.firstDay || 'İlk gün';
        const otherDaysLabel = preview.dataset.otherDays || 'Diğer günler';
        const sessionLabel = preview.dataset.sessionLabel || 'Tahmini oturum';
        const capacityLabel = preview.dataset.capacityLabel || 'Tahmini kapasite';
        const validLabel = preview.dataset.valid || 'Zaman planı tutarlı';

        body.innerHTML = `
            <div class="program-preview-valid"><i class="ri-checkbox-circle-line"></i>${validLabel}</div>
            <div class="program-preview-grid">
                <div><span>${firstDayLabel}</span><strong>${sessionLabel}: ${firstDaySessions}</strong><small>${capacityLabel}: ${firstDayCapacity}</small></div>
                ${dayCount > 1 ? `<div><span>${otherDaysLabel} (${dayCount - 1})</span><strong>${sessionLabel}: ${otherDaySessions}</strong><small>${capacityLabel}: ${otherDayCapacity}</small></div>` : ''}
                <div><span>${formatTime(dayStart)}–${formatTime(dayEnd)}</span><strong>${selectedRooms.length} salon</strong><small>${settings.presentation} dk / bildiri</small></div>
            </div>`;

        return [];
    }

    const generateForm = document.getElementById('programGenerateForm');
    if (generateForm) {
        generateForm.addEventListener('input', updateSchedulePreview);
        generateForm.addEventListener('change', updateSchedulePreview);
        generateForm.addEventListener('submit', function (event) {
            const candidateCount = updateSubmissionFilterPreview();
            if (candidateCount <= 0) {
                event.preventDefault();
                showError(noFilterCandidatesElement?.textContent?.trim() || 'Seçilen filtrelere uygun bildiri bulunamadı.');
                return;
            }

            const mode = event.submitter?.value;
            if (mode === '2') return;

            const errors = updateSchedulePreview();
            if (errors.length > 0) {
                event.preventDefault();
                showError(errors[0]);
            }
        });
    }

    $(document).on('click', '.js-generate-program', function (event) {
        if (!document.querySelectorAll('input[name="Generate.RoomIds"]:checked').length) {
            event.preventDefault();
            showError('En az bir salon seçmelisiniz.');
        }
    });

    restoreActiveDay();
    initializeDayTabDragSwitch();
    initializeSortable();
    initializeBreakSortable();
    initializeBreakInsertionDragDrop();
    updateSubmissionFilterPreview();
    updateSchedulePreview();
})(jQuery);
