(function () {
    'use strict';

    const form = document.getElementById('submissionCreateForm');
    if (!form) return;

    const congressSelect = document.getElementById('submissionCongressSelect');
    if (congressSelect) {
        congressSelect.addEventListener('change', function () {
            const selectedCongressId = this.value;
            const url = new URL(window.location.href);
            if (selectedCongressId) {
                url.searchParams.set('congressId', selectedCongressId);
            } else {
                url.searchParams.delete('congressId');
            }
            window.location.href = url.toString();
        });
    }

    const authors = [];
    const list = document.getElementById('authorsList');
    const hidden = document.getElementById('authorsHiddenContainer');
    const badge = document.getElementById('authorCountBadge');
    const addButton = document.getElementById('addAuthorButton');

    if (!list || !hidden || !badge || !addButton) return;

    const titleOptions = readTitleOptions();

    try {
        const existing = JSON.parse(list.dataset.existingAuthors || '[]');
        existing.forEach(item => authors.push({
            titleId: item.TitleId || item.titleId || '',
            titleName: item.TitleName || item.titleName || resolveTitleName(item.TitleId || item.titleId || '', titleOptions),
            fullName: item.FullName || item.fullName || '',
            email: item.Email || item.email || '',
            institution: item.Institution || item.institution || '',
            orcid: item.Orcid || item.orcid || '',
            isCorrespondingAuthor: Boolean(item.IsCorrespondingAuthor ?? item.isCorrespondingAuthor)
        }));
    } catch {
        // ignored intentionally; empty author list is safer than blocking the form
    }

    addButton.addEventListener('click', function () {
        const titleInput = document.getElementById('authorTitleId');
        const fullNameInput = document.getElementById('authorFullName');
        const emailInput = document.getElementById('authorEmail');
        const titleId = titleInput.value.trim();
        const titleName = titleInput.selectedOptions?.[0]?.textContent?.trim() || '';
        const fullName = fullNameInput.value.trim();
        const orcid = document.getElementById('authorOrcid').value.trim();
        const institution = document.getElementById('authorInstitution').value.trim();
        const email = emailInput.value.trim();
        const isCorrespondingAuthor = document.getElementById('authorRole').value === 'true';

        if (!titleId) {
            titleInput.focus();
            return;
        }

        if (!fullName) {
            fullNameInput.focus();
            return;
        }

        if (!email || !emailInput.checkValidity()) {
            emailInput.reportValidity();
            emailInput.focus();
            return;
        }

        authors.push({ titleId, titleName, fullName, email, institution, orcid, isCorrespondingAuthor });
        clearAuthorInputs();
        renderAuthors();
    });

    function clearAuthorInputs() {
        document.getElementById('authorTitleId').value = '';
        document.getElementById('authorFullName').value = '';
        document.getElementById('authorOrcid').value = '';
        document.getElementById('authorInstitution').value = '';
        document.getElementById('authorEmail').value = '';
        document.getElementById('authorRole').value = 'false';
    }

    function removeAuthor(index) {
        authors.splice(index, 1);
        renderAuthors();
    }

    function renderAuthors() {
        hidden.innerHTML = '';
        list.innerHTML = '';
        badge.textContent = `${authors.length} Yazar`;

        if (authors.length === 0) {
            list.innerHTML = '<div class="text-neutral-500 text-sm">Henüz yazar eklenmedi.</div>';
            return;
        }

        authors.forEach((author, index) => {
            appendHidden(index, 'TitleId', author.titleId);
            appendHidden(index, 'TitleName', author.titleName || resolveTitleName(author.titleId, titleOptions));
            appendHidden(index, 'FullName', author.fullName);
            appendHidden(index, 'Email', author.email);
            appendHidden(index, 'Institution', author.institution);
            appendHidden(index, 'Orcid', author.orcid);
            appendHidden(index, 'IsCorrespondingAuthor', author.isCorrespondingAuthor ? 'true' : 'false');

            const wrapper = document.createElement('div');
            wrapper.className = 'border rounded-3 p-16 mb-3';
            wrapper.innerHTML = `
                <div class="d-flex align-items-start justify-content-between gap-3">
                    <div>
                        <div class="d-flex align-items-center gap-2 mb-1">
                            <i class="ri-circle-fill ${author.isCorrespondingAuthor ? 'text-success' : 'text-neutral-400'} text-xs"></i>
                            <span class="fw-semibold text-primary-light"></span>
                        </div>
                        <span class="badge ${author.isCorrespondingAuthor ? 'bg-success-100 text-success-600' : 'bg-neutral-200 text-neutral-700'} rounded-pill mb-2">
                            ${author.isCorrespondingAuthor ? 'Sorumlu Yazar' : 'Yazar'}
                        </span>
                        <p class="text-sm text-neutral-500 mb-0 title"></p>
                        <p class="text-sm text-neutral-500 mb-0 institution"></p>
                        <p class="text-sm text-neutral-500 mb-0 orcid"></p>
                        <p class="text-sm text-neutral-500 mb-0 email"></p>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <button type="button" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center" data-remove-author="${index}">
                            <i class="ri-delete-bin-line"></i>
                        </button>
                    </div>
                </div>`;

            wrapper.querySelector('.fw-semibold').textContent = author.fullName;
            wrapper.querySelector('.title').textContent = author.titleName || resolveTitleName(author.titleId, titleOptions) || '-';
            wrapper.querySelector('.institution').textContent = author.institution || '-';
            wrapper.querySelector('.orcid').textContent = author.orcid || '-';
            wrapper.querySelector('.email').textContent = author.email || '-';
            list.appendChild(wrapper);
        });

        list.querySelectorAll('[data-remove-author]').forEach(button => {
            button.addEventListener('click', function () {
                removeAuthor(Number(this.dataset.removeAuthor));
            });
        });
    }


    function readTitleOptions() {
        try {
            return JSON.parse(list.dataset.titleOptions || '[]').map(item => ({
                id: item.Id || item.id || '',
                text: item.Text || item.text || ''
            }));
        } catch {
            return [];
        }
    }

    function resolveTitleName(titleId, titleOptions) {
        if (!titleId) return '';
        const option = (titleOptions || []).find(item => String(item.id).toLowerCase() === String(titleId).toLowerCase());
        return option ? option.text : '';
    }

    function appendHidden(index, property, value) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = `Authors[${index}].${property}`;
        input.value = value || '';
        hidden.appendChild(input);
    }

    renderAuthors();
}());
