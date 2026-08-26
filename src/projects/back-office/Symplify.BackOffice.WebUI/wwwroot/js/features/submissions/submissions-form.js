(function () {
    'use strict';

    const form = document.getElementById('submissionCreateForm') || document.getElementById('submissionEditForm');
    if (!form) return;

    const validationSummary = form.querySelector('[data-submission-validation-summary]');

    function t(key, fallback) {
        if (window.Symplify && typeof window.Symplify.t === 'function') {
            return window.Symplify.t(key, fallback);
        }

        return fallback || key;
    }

    function localizedFallback(trValue, enValue) {
        const htmlLang = document.documentElement.getAttribute('lang') || '';
        const pathCultureMatch = window.location.pathname.match(/\/(en-US|tr-TR)(?=\/|$)/i);
        const culture = (htmlLang || (pathCultureMatch ? pathCultureMatch[1] : '') || '').toLowerCase();
        return culture.startsWith('en') ? enValue : trValue;
    }

    function tx(key, trValue, enValue) {
        return t(key, localizedFallback(trValue, enValue));
    }

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
    const authorCollectionValidation = document.querySelector('[data-author-collection-validation]');
    const titleOptions = list ? readTitleOptions() : [];
    const allowAuthorEdit = list ? String(list.dataset.allowAuthorEdit || '').toLowerCase() === 'true' : false;
    let editingAuthorIndex = null;
    let cancelEditButton = null;
    let authorTitleCombobox = null;
    let authorEditModalElement = null;
    let authorEditModalInstance = null;
    let authorEditModalIndex = null;

    if (list && hidden && badge && addButton) {
        normalizeTransientAuthorInputs();
        authorTitleCombobox = initializeAuthorTitleCombobox();
        initializeAuthorValidationEvents();
        initializeAuthors();
    }

    initializeEditors();
    initializeRichTextValidation();
    initializeValidationSummary();
    initializeSubmitFlow();

    function initializeEditors() {
        if (window.Symplify && window.Symplify.TinyMce && typeof window.Symplify.TinyMce.initAll === 'function') {
            window.Symplify.TinyMce.initAll($(form));
        }

        form.addEventListener('shown.bs.tab', function (event) {
            const targetSelector = event.target && event.target.getAttribute('data-bs-target');
            if (!targetSelector || !window.Symplify || !window.Symplify.TinyMce || typeof window.Symplify.TinyMce.initAll !== 'function') {
                return;
            }

            const pane = form.querySelector(targetSelector);
            if (pane) {
                window.Symplify.TinyMce.initAll($(pane));
            }
        });
    }

    function normalizeTransientAuthorInputs() {
        const transientInputs = [
            document.getElementById('authorTitleId'),
            document.getElementById('authorFirstName'),
            document.getElementById('authorLastName'),
            document.getElementById('authorFullName'),
            document.getElementById('authorEmail'),
            document.getElementById('authorOrcid'),
            document.getElementById('authorInstitution'),
            document.getElementById('authorRole')
        ];

        transientInputs.forEach(input => {
            if (!input) return;

            input.required = false;
            input.removeAttribute('required');
            input.removeAttribute('pattern');
            input.removeAttribute('data-val');
            input.removeAttribute('data-val-required');
            input.removeAttribute('aria-required');
        });

        const emailInput = document.getElementById('authorEmail');
        if (emailInput) {
            emailInput.type = 'text';
            emailInput.setAttribute('inputmode', 'email');
            emailInput.setAttribute('autocomplete', 'email');
        }
    }

    function initializeAuthors() {
        try {
            const existing = JSON.parse(list.dataset.existingAuthors || '[]');
            existing.forEach(item => authors.push({
                id: item.Id || item.id || '',
                titleId: item.TitleId || item.titleId || '',
                titleName: item.TitleName || item.titleName || resolveTitleName(item.TitleId || item.titleId || '', titleOptions),
                firstName: normalizePersonName(item.FirstName || item.firstName || splitFullName(item.FullName || item.fullName || '').firstName),
                lastName: normalizeSurname(item.LastName || item.lastName || splitFullName(item.FullName || item.fullName || '').lastName),
                fullName: normalizeFullName(item.FirstName || item.firstName, item.LastName || item.lastName, item.FullName || item.fullName || ''),
                email: item.Email || item.email || '',
                institution: normalizeInstitution(item.Institution || item.institution || ''),
                orcid: item.Orcid || item.orcid || '',
                isCorrespondingAuthor: Boolean(item.IsCorrespondingAuthor ?? item.isCorrespondingAuthor)
            }));
        } catch {
            // ignored intentionally; empty author list is safer than blocking the form
        }

        initializeAuthorEditControls();

        addButton.addEventListener('click', function () {
            const validation = validateAuthorForm({ showErrors: true });
            if (!validation.isValid || !validation.author) return;

            const duplicateAuthorIndex = findAuthorIndexByEmail(validation.author.email, editingAuthorIndex);
            if (duplicateAuthorIndex !== -1) {
                setAuthorFieldError('email', tx('BackOffice.Submissions.AuthorForm.Validation.DuplicateEmail', 'Bu e-posta adresi ile bir yazar zaten eklendi. Aynı e-posta adresiyle ikinci bir yazar eklenemez.', 'An author with this email address has already been added. You cannot add another author with the same email address.'));
                focusAuthorField('email');
                return;
            }

            if (allowAuthorEdit && editingAuthorIndex !== null && authors[editingAuthorIndex]) {
                authors[editingAuthorIndex] = {
                    ...authors[editingAuthorIndex],
                    ...validation.author
                };
                clearAuthorEditState();
            } else {
                authors.push({ id: '', ...validation.author });
                clearAuthorInputs();
            }

            clearAuthorCollectionError();
            renderAuthors();
        });

        renderAuthors();
    }

    function initializeAuthorEditControls() {
        if (!allowAuthorEdit || !addButton || cancelEditButton) return;

        cancelEditButton = document.createElement('button');
        cancelEditButton.type = 'button';
        cancelEditButton.className = 'btn btn-outline-neutral-900 radius-8 px-20 py-11 d-none align-items-center justify-content-center gap-2 w-100 mt-2';
        cancelEditButton.innerHTML = '<i class="ri-close-line"></i><span>' + tx('BackOffice.Submissions.AuthorForm.CancelEditButton', 'Düzenlemeyi İptal Et', 'Cancel Editing') + '</span>';
        addButton.insertAdjacentElement('afterend', cancelEditButton);

        cancelEditButton.addEventListener('click', function () {
            clearAuthorEditState();
            clearAuthorInputs();
            clearAllAuthorFieldErrors();
        });
    }

    function initializeAuthorValidationEvents() {
        const fields = ['title', 'firstName', 'lastName', 'email', 'institution', 'role'];
        fields.forEach(field => {
            const input = getAuthorInput(field);
            if (!input) return;

            input.addEventListener('input', function () {
                clearAuthorFieldError(field);
                clearAuthorCollectionError();
            });
            input.addEventListener('change', function () {
                clearAuthorFieldError(field);
                clearAuthorCollectionError();
            });
        });
    }

    function initializeAuthorTitleCombobox() {
        const select = document.getElementById('authorTitleId');
        if (!select || select.dataset.authorCombobox !== 'true') return null;

        const options = Array.from(select.options)
            .filter(option => option.value)
            .map(option => ({ value: option.value, text: option.textContent.trim() }));

        const wrapper = document.createElement('div');
        wrapper.className = 'author-combobox position-relative';

        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control author-combobox-input';
        input.placeholder = select.dataset.authorPlaceholder || tx('BackOffice.Submissions.AuthorForm.TitlePlaceholder', 'Unvan seçiniz', 'Select title');
        input.autocomplete = 'off';
        input.setAttribute('aria-autocomplete', 'list');

        const menu = document.createElement('div');
        menu.className = 'author-combobox-menu position-absolute top-100 start-0 end-0 z-3 max-h-258-px overflow-auto p-8 mt-4 bg-base border radius-12 shadow-sm d-none';
        menu.setAttribute('role', 'listbox');

        wrapper.appendChild(input);
        wrapper.appendChild(menu);
        select.classList.add('d-none');
        select.setAttribute('tabindex', '-1');
        select.insertAdjacentElement('afterend', wrapper);

        function render(query) {
            const normalizedQuery = normalizeForSearch(query);
            const filtered = normalizedQuery
                ? options.filter(option => normalizeForSearch(option.text).includes(normalizedQuery))
                : options;

            menu.innerHTML = '';

            if (filtered.length === 0) {
                const empty = document.createElement('div');
                empty.className = 'author-combobox-empty px-12 py-8 text-neutral-500 text-sm';
                empty.textContent = select.dataset.authorNoResults || tx('BackOffice.Submissions.AuthorForm.TitleNoResults', 'Eşleşen unvan bulunamadı.', 'No matching title was found.');
                menu.appendChild(empty);
            } else {
                filtered.forEach(option => {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.className = 'author-combobox-option btn w-100 text-start bg-transparent border-0 radius-8 px-12 py-8 text-primary-light bg-hover-primary-50 text-hover-primary-600';
                    button.textContent = option.text;
                    button.setAttribute('role', 'option');
                    button.addEventListener('mousedown', function (event) {
                        event.preventDefault();
                        select.value = option.value;
                        input.value = option.text;
                        hide();
                        clearAuthorFieldError('title');
                        clearAuthorCollectionError();
                        select.dispatchEvent(new Event('change', { bubbles: true }));
                    });
                    menu.appendChild(button);
                });
            }

            show();
        }

        function show() {
            menu.classList.remove('d-none');
        }

        function hide() {
            menu.classList.add('d-none');
        }

        function syncFromSelect() {
            const selected = options.find(option => option.value === select.value);
            input.value = selected ? selected.text : '';
        }

        input.addEventListener('focus', function () {
            render(input.value);
        });

        input.addEventListener('input', function () {
            select.value = '';
            render(input.value);
            clearAuthorFieldError('title');
            clearAuthorCollectionError();
        });

        input.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                hide();
                input.blur();
            }
        });

        input.addEventListener('blur', function () {
            window.setTimeout(function () {
                const selected = options.find(option => option.value === select.value);
                input.value = selected ? selected.text : '';
                hide();
            }, 120);
        });

        select.addEventListener('change', syncFromSelect);
        syncFromSelect();

        return {
            input,
            syncFromSelect,
            focus: function () {
                input.focus();
                render(input.value);
            }
        };
    }

    function validateAuthorForm(options) {
        const showErrors = options?.showErrors !== false;
        const titleInput = document.getElementById('authorTitleId');
        const firstNameInput = document.getElementById('authorFirstName');
        const lastNameInput = document.getElementById('authorLastName');
        const fullNameInput = document.getElementById('authorFullName');
        const emailInput = document.getElementById('authorEmail');
        const institutionInput = document.getElementById('authorInstitution');
        const roleInput = document.getElementById('authorRole');
        const titleId = (titleInput?.value || '').trim();
        const titleName = titleInput?.selectedOptions?.[0]?.textContent?.trim() || resolveTitleName(titleId, titleOptions);
        const legacyName = (fullNameInput?.value || '').trim();
        const nameParts = splitFullName(legacyName);
        const firstName = normalizePersonName((firstNameInput?.value || '').trim() || nameParts.firstName);
        const lastName = normalizeSurname((lastNameInput?.value || '').trim() || nameParts.lastName);
        const fullName = buildFullName(firstName, lastName);
        const orcid = (document.getElementById('authorOrcid')?.value || '').trim();
        const institution = normalizeInstitution((institutionInput?.value || '').trim());
        const email = (emailInput?.value || '').trim();
        const roleValue = (roleInput?.value || '').trim();
        let isValid = true;
        let firstInvalidField = null;

        if (firstNameInput) firstNameInput.value = firstName;
        if (lastNameInput) lastNameInput.value = lastName;
        if (fullNameInput) fullNameInput.value = fullName;
        if (institutionInput) institutionInput.value = institution;

        clearAllAuthorFieldErrors();

        function markInvalid(field, message) {
            isValid = false;
            if (!firstInvalidField) firstInvalidField = field;
            if (showErrors) setAuthorFieldError(field, message);
        }

        if (!titleId) {
            markInvalid('title', tx('BackOffice.Submissions.AuthorForm.Validation.TitleRequired', 'Yazar unvanı zorunludur.', 'Author title is required.'));
        }

        if (!firstName) {
            markInvalid('firstName', tx('BackOffice.Submissions.AuthorForm.Validation.FirstNameRequired', 'Yazar adı zorunludur.', 'Author first name is required.'));
        }

        if (!lastName) {
            markInvalid('lastName', tx('BackOffice.Submissions.AuthorForm.Validation.LastNameRequired', 'Yazar soyadı zorunludur.', 'Author last name is required.'));
        }

        if (!email) {
            markInvalid('email', tx('BackOffice.Submissions.AuthorForm.Validation.EmailRequired', 'Yazar e-posta adresi zorunludur.', 'Author email address is required.'));
        } else if (!isValidEmail(email)) {
            markInvalid('email', tx('BackOffice.Submissions.AuthorForm.Validation.EmailInvalid', 'Geçerli bir yazar e-posta adresi giriniz.', 'Enter a valid author email address.'));
        }

        if (!institution) {
            markInvalid('institution', tx('BackOffice.Submissions.AuthorForm.Validation.InstitutionRequired', 'Yazar kurum bilgisi zorunludur.', 'Author institution is required.'));
        }

        if (roleValue !== 'true' && roleValue !== 'false') {
            markInvalid('role', tx('BackOffice.Submissions.AuthorForm.Validation.RoleRequired', 'Yazar rolü zorunludur.', 'Author role is required.'));
        }

        if (!isValid) {
            if (firstInvalidField) focusAuthorField(firstInvalidField);
            return { isValid: false };
        }

        return {
            isValid: true,
            author: {
                titleId,
                titleName,
                firstName,
                lastName,
                fullName,
                email,
                institution,
                orcid,
                isCorrespondingAuthor: roleValue === 'true'
            }
        };
    }

    function clearAuthorInputs() {
        const title = document.getElementById('authorTitleId');
        const firstName = document.getElementById('authorFirstName');
        const lastName = document.getElementById('authorLastName');
        const fullName = document.getElementById('authorFullName');
        const orcid = document.getElementById('authorOrcid');
        const institution = document.getElementById('authorInstitution');
        const email = document.getElementById('authorEmail');
        const role = document.getElementById('authorRole');

        if (title) title.value = '';
        if (firstName) firstName.value = '';
        if (lastName) lastName.value = '';
        if (fullName) fullName.value = '';
        if (orcid) orcid.value = '';
        if (institution) institution.value = '';
        if (email) email.value = '';
        if (role) role.value = '';
        if (authorTitleCombobox) authorTitleCombobox.syncFromSelect();
        clearAllAuthorFieldErrors();
    }

    function clearAuthorEditState() {
        editingAuthorIndex = null;
        addButton.innerHTML = '<i class="ri-add-line"></i><span>' + tx('BackOffice.Submissions.AuthorForm.AddButton', 'Yazarı Listeye Ekle', 'Add Author to List') + '</span>';
        addButton.classList.remove('btn-primary-600');
        addButton.classList.add('btn-success-600');

        if (cancelEditButton) {
            cancelEditButton.classList.add('d-none');
            cancelEditButton.classList.remove('d-flex');
        }
    }

    function startEditAuthor(index) {
        if (!allowAuthorEdit || !authors[index]) return;

        clearAuthorEditState();
        clearAuthorInputs();
        clearAllAuthorFieldErrors();
        clearAuthorCollectionError();
        openAuthorEditModal(index);
    }

    function ensureAuthorEditModal() {
        if (authorEditModalElement) return authorEditModalElement;

        const titleOptionsHtml = titleOptions
            .map(option => `<option value="${escapeHtml(option.id)}">${escapeHtml(option.text)}</option>`)
            .join('');

        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'authorEditModal';
        modal.tabIndex = -1;
        modal.setAttribute('aria-hidden', 'true');
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content radius-16">
                    <div class="modal-header border-bottom">
                        <div>
                            <h5 class="modal-title mb-1">${escapeHtml(tx('BackOffice.Submissions.AuthorEditModal.Title', 'Yazar Güncelle', 'Update Author'))}</h5>
                            <p class="text-neutral-500 text-sm mb-0">${escapeHtml(tx('BackOffice.Submissions.AuthorEditModal.Subtitle', 'Yazar bilgilerini kontrol edip güncelleyin.', 'Review and update the author information.'))}</p>
                        </div>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="${escapeHtml(tx('Common.Close', 'Kapat', 'Close'))}"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row gy-3">
                            <div class="col-md-6">
                                <label class="form-label" for="editAuthorTitleId">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.TitleLabel', 'Unvan', 'Title'))} <span class="text-danger">*</span></label>
                                <select class="form-control form-select" id="editAuthorTitleId" data-edit-author-field="title">
                                    <option value="">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.TitlePlaceholder', 'Unvan seçiniz', 'Select title'))}</option>
                                    ${titleOptionsHtml}
                                </select>
                                <span class="text-danger text-sm d-block mt-1" data-edit-author-validation-for="title"></span>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label" for="editAuthorFirstName">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.FirstNameLabel', 'Ad', 'First Name'))} <span class="text-danger">*</span></label>
                                <input class="form-control" id="editAuthorFirstName" type="text" data-edit-author-field="firstName" autocomplete="given-name" data-normalize-person-name="true" />
                                <span class="text-danger text-sm d-block mt-1" data-edit-author-validation-for="firstName"></span>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label" for="editAuthorLastName">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.LastNameLabel', 'Soyad', 'Last Name'))} <span class="text-danger">*</span></label>
                                <input class="form-control" id="editAuthorLastName" type="text" data-edit-author-field="lastName" autocomplete="family-name" data-normalize-surname="true" />
                                <span class="text-danger text-sm d-block mt-1" data-edit-author-validation-for="lastName"></span>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label" for="editAuthorEmail">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.EmailLabel', 'E-posta', 'Email'))} <span class="text-danger">*</span></label>
                                <input class="form-control" id="editAuthorEmail" type="text" inputmode="email" data-edit-author-field="email" autocomplete="email" />
                                <span class="text-danger text-sm d-block mt-1" data-edit-author-validation-for="email"></span>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label" for="editAuthorInstitution">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.InstitutionLabel', 'Kurum', 'Institution'))} <span class="text-danger">*</span></label>
                                <input class="form-control" id="editAuthorInstitution" type="text" data-edit-author-field="institution" autocomplete="organization" data-normalize-institution="true" />
                                <span class="text-danger text-sm d-block mt-1" data-edit-author-validation-for="institution"></span>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label" for="editAuthorOrcid">${escapeHtml(tx('BackOffice.Submissions.Create.OrcidLabel', 'ORCID', 'ORCID'))}</label>
                                <input class="form-control" id="editAuthorOrcid" type="text" inputmode="numeric" autocomplete="off" />
                            </div>
                            <div class="col-md-6">
                                <label class="form-label" for="editAuthorRole">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.RoleLabel', 'Yazar Rolü', 'Author Role'))} <span class="text-danger">*</span></label>
                                <select class="form-control form-select" id="editAuthorRole" data-edit-author-field="role">
                                    <option value="">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.RolePlaceholder', 'Rol seçiniz', 'Select role'))}</option>
                                    <option value="true">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.RoleCorresponding', 'Sorumlu Yazar', 'Corresponding Author'))}</option>
                                    <option value="false">${escapeHtml(tx('BackOffice.Submissions.AuthorForm.RoleAuthor', 'Yazar', 'Author'))}</option>
                                </select>
                                <span class="text-danger text-sm d-block mt-1" data-edit-author-validation-for="role"></span>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer border-top">
                        <button type="button" class="btn btn-outline-neutral-900 radius-8" data-bs-dismiss="modal">${escapeHtml(tx('Common.Cancel', 'Vazgeç', 'Cancel'))}</button>
                        <button type="button" class="btn btn-primary-600 radius-8" id="saveAuthorEditButton">
                            <i class="ri-save-line"></i>
                            <span>${escapeHtml(tx('BackOffice.Submissions.AuthorEditModal.SaveButton', 'Yazarı Güncelle', 'Update Author'))}</span>
                        </button>
                    </div>
                </div>
            </div>`;

        document.body.appendChild(modal);
        authorEditModalElement = modal;

        modal.querySelectorAll('[data-edit-author-field]').forEach(input => {
            input.addEventListener('input', function () {
                clearAuthorModalFieldError(input.dataset.editAuthorField);
            });
            input.addEventListener('change', function () {
                clearAuthorModalFieldError(input.dataset.editAuthorField);
            });
        });

        modal.querySelector('#saveAuthorEditButton')?.addEventListener('click', saveAuthorFromModal);
        modal.addEventListener('hidden.bs.modal', function () {
            authorEditModalIndex = null;
            clearAllAuthorModalFieldErrors();
        });

        return modal;
    }

    function openAuthorEditModal(index) {
        const author = authors[index];
        const modal = ensureAuthorEditModal();

        authorEditModalIndex = index;
        modal.querySelector('#editAuthorTitleId').value = author.titleId || '';
        modal.querySelector('#editAuthorFirstName').value = author.firstName || splitFullName(author.fullName || '').firstName || '';
        modal.querySelector('#editAuthorLastName').value = author.lastName || splitFullName(author.fullName || '').lastName || '';
        modal.querySelector('#editAuthorEmail').value = author.email || '';
        modal.querySelector('#editAuthorInstitution').value = author.institution || '';
        modal.querySelector('#editAuthorOrcid').value = author.orcid || '';
        modal.querySelector('#editAuthorRole').value = author.isCorrespondingAuthor ? 'true' : 'false';
        clearAllAuthorModalFieldErrors();

        if (window.bootstrap && window.bootstrap.Modal) {
            authorEditModalInstance = window.bootstrap.Modal.getOrCreateInstance(modal);
            authorEditModalInstance.show();
            return;
        }

        modal.classList.add('show');
        modal.style.display = 'block';
        modal.removeAttribute('aria-hidden');
    }

    function saveAuthorFromModal() {
        if (authorEditModalIndex === null || !authors[authorEditModalIndex]) return;

        const validation = validateAuthorModalForm({ showErrors: true });
        if (!validation.isValid || !validation.author) return;

        const duplicateAuthorIndex = findAuthorIndexByEmail(validation.author.email, authorEditModalIndex);
        if (duplicateAuthorIndex !== -1) {
            setAuthorModalFieldError('email', tx('BackOffice.Submissions.AuthorForm.Validation.DuplicateEmail', 'Bu e-posta adresi ile bir yazar zaten eklendi. Aynı e-posta adresiyle ikinci bir yazar eklenemez.', 'An author with this email address has already been added. You cannot add another author with the same email address.'));
            document.getElementById('editAuthorEmail')?.focus();
            return;
        }

        authors[authorEditModalIndex] = {
            ...authors[authorEditModalIndex],
            ...validation.author
        };

        renderAuthors();
        clearAuthorCollectionError();

        if (authorEditModalInstance) {
            authorEditModalInstance.hide();
        } else if (authorEditModalElement) {
            authorEditModalElement.classList.remove('show');
            authorEditModalElement.style.display = 'none';
            authorEditModalElement.setAttribute('aria-hidden', 'true');
        }
    }

    function validateAuthorModalForm(options) {
        const showErrors = options?.showErrors !== false;
        const titleInput = document.getElementById('editAuthorTitleId');
        const firstNameInput = document.getElementById('editAuthorFirstName');
        const lastNameInput = document.getElementById('editAuthorLastName');
        const emailInput = document.getElementById('editAuthorEmail');
        const institutionInput = document.getElementById('editAuthorInstitution');
        const roleInput = document.getElementById('editAuthorRole');
        const titleId = (titleInput?.value || '').trim();
        const titleName = titleInput?.selectedOptions?.[0]?.textContent?.trim() || resolveTitleName(titleId, titleOptions);
        const firstName = normalizePersonName(firstNameInput?.value || '');
        const lastName = normalizeSurname(lastNameInput?.value || '');
        const fullName = buildFullName(firstName, lastName);
        const email = (emailInput?.value || '').trim();
        const institution = normalizeInstitution(institutionInput?.value || '');
        const orcid = (document.getElementById('editAuthorOrcid')?.value || '').trim();
        const roleValue = (roleInput?.value || '').trim();
        let isValid = true;
        let firstInvalidField = null;

        if (firstNameInput) firstNameInput.value = firstName;
        if (lastNameInput) lastNameInput.value = lastName;
        if (institutionInput) institutionInput.value = institution;

        clearAllAuthorModalFieldErrors();

        function markInvalid(field, message) {
            isValid = false;
            if (!firstInvalidField) firstInvalidField = field;
            if (showErrors) setAuthorModalFieldError(field, message);
        }

        if (!titleId) markInvalid('title', tx('BackOffice.Submissions.AuthorForm.Validation.TitleRequired', 'Yazar unvanı zorunludur.', 'Author title is required.'));
        if (!firstName) markInvalid('firstName', tx('BackOffice.Submissions.AuthorForm.Validation.FirstNameRequired', 'Yazar adı zorunludur.', 'Author first name is required.'));
        if (!lastName) markInvalid('lastName', tx('BackOffice.Submissions.AuthorForm.Validation.LastNameRequired', 'Yazar soyadı zorunludur.', 'Author last name is required.'));
        if (!email) {
            markInvalid('email', tx('BackOffice.Submissions.AuthorForm.Validation.EmailRequired', 'Yazar e-posta adresi zorunludur.', 'Author email address is required.'));
        } else if (!isValidEmail(email)) {
            markInvalid('email', tx('BackOffice.Submissions.AuthorForm.Validation.EmailInvalid', 'Geçerli bir yazar e-posta adresi giriniz.', 'Enter a valid author email address.'));
        }
        if (!institution) markInvalid('institution', tx('BackOffice.Submissions.AuthorForm.Validation.InstitutionRequired', 'Yazar kurum bilgisi zorunludur.', 'Author institution is required.'));
        if (roleValue !== 'true' && roleValue !== 'false') markInvalid('role', tx('BackOffice.Submissions.AuthorForm.Validation.RoleRequired', 'Yazar rolü zorunludur.', 'Author role is required.'));

        if (!isValid) {
            if (firstInvalidField) document.querySelector(`[data-edit-author-field="${firstInvalidField}"]`)?.focus();
            return { isValid: false };
        }

        return {
            isValid: true,
            author: {
                titleId,
                titleName,
                firstName,
                lastName,
                fullName,
                email,
                institution,
                orcid,
                isCorrespondingAuthor: roleValue === 'true'
            }
        };
    }

    function setAuthorModalFieldError(field, message) {
        const target = document.querySelector(`[data-edit-author-validation-for="${field}"]`);
        if (target) target.textContent = message;

        const input = document.querySelector(`[data-edit-author-field="${field}"]`);
        if (input) input.classList.add('is-invalid');
    }

    function clearAuthorModalFieldError(field) {
        const target = document.querySelector(`[data-edit-author-validation-for="${field}"]`);
        if (target) target.textContent = '';

        const input = document.querySelector(`[data-edit-author-field="${field}"]`);
        if (input) input.classList.remove('is-invalid');
    }

    function clearAllAuthorModalFieldErrors() {
        ['title', 'firstName', 'lastName', 'email', 'institution', 'role'].forEach(clearAuthorModalFieldError);
    }

    function removeAuthor(index) {
        authors.splice(index, 1);

        if (editingAuthorIndex !== null) {
            if (editingAuthorIndex === index) {
                clearAuthorEditState();
                clearAuthorInputs();
            } else if (editingAuthorIndex > index) {
                editingAuthorIndex -= 1;
            }
        }

        renderAuthors();
    }

    function renderAuthors() {
        hidden.innerHTML = '';
        list.innerHTML = '';
        badge.textContent = `${authors.length} ${tx('BackOffice.Submissions.AuthorList.CountSuffix', 'Yazar', 'Author(s)')}`;

        if (authors.length === 0) {
            list.innerHTML = `
                <div class="empty-state py-24">
                    <span class="empty-state__icon"><i class="ri-user-add-line text-3xl"></i></span>
                    <h6>${escapeHtml(tx('BackOffice.Submissions.AuthorList.EmptyTitle', 'Henüz yazar eklenmedi', 'No authors added yet'))}</h6>
                    <p class="text-sm mb-0">${escapeHtml(tx('BackOffice.Submissions.AuthorList.Empty', 'Yazar eklediğinde bilgiler bu alanda listelenecek.', 'When you add an author, the information will be listed here.'))}</p>
                </div>`;
            return;
        }

        authors.forEach((author, index) => {
            const resolvedTitleName = author.titleName || resolveTitleName(author.titleId, titleOptions);

            appendHidden(index, 'Id', author.id);
            appendHidden(index, 'TitleId', author.titleId);
            appendHidden(index, 'TitleName', resolvedTitleName);
            appendHidden(index, 'FirstName', author.firstName);
            appendHidden(index, 'LastName', author.lastName);
            appendHidden(index, 'FullName', author.fullName);
            appendHidden(index, 'Email', author.email);
            appendHidden(index, 'Institution', author.institution);
            appendHidden(index, 'Orcid', author.orcid);
            appendHidden(index, 'IsCorrespondingAuthor', author.isCorrespondingAuthor ? 'true' : 'false');

            const wrapper = document.createElement('div');
            wrapper.className = `submission-author-card card border radius-12 p-16 mb-12 shadow-none ${author.isCorrespondingAuthor ? 'submission-author-card-corresponding border-success-300 bg-success-50' : 'bg-base'}`;
            wrapper.innerHTML = `
                <div class="submission-author-card-body d-flex flex-wrap flex-sm-nowrap align-items-start align-items-sm-center justify-content-between gap-3">
                    <div class="submission-author-card-main flex-grow-1 min-w-0">
                        <div class="submission-author-card-heading d-flex flex-wrap align-items-center gap-2 mb-8">
                            <span class="author-status-dot w-10-px h-10-px rounded-circle flex-shrink-0 ${author.isCorrespondingAuthor ? 'author-status-dot-success bg-success-600' : 'author-status-dot-muted bg-neutral-400'}"></span>
                            <span class="fw-semibold text-primary-light author-display-name text-break"></span>
                            <span class="badge ${author.isCorrespondingAuthor ? 'bg-success-100 text-success-600' : 'bg-neutral-200 text-neutral-700'} rounded-pill author-role-badge">
                                ${author.isCorrespondingAuthor ? tx('BackOffice.Submissions.AuthorForm.RoleCorresponding', 'Sorumlu Yazar', 'Corresponding Author') : tx('BackOffice.Submissions.AuthorForm.RoleAuthor', 'Yazar', 'Author')}
                            </span>
                        </div>
                        <div class="submission-author-card-meta d-flex flex-wrap align-items-center gap-2 text-sm text-neutral-700">
                            <span class="submission-author-card-meta-item d-inline-flex align-items-center gap-1">
                                <span class="submission-author-card-meta-label text-neutral-500 fw-semibold text-nowrap">${tx('BackOffice.Submissions.AuthorCard.EmailLabel', 'E-posta', 'Email')}:</span>
                                <span class="email text-break"></span>
                            </span>
                            <span class="submission-author-card-meta-item d-inline-flex align-items-center gap-1">
                                <span class="submission-author-card-meta-label text-neutral-500 fw-semibold text-nowrap">${tx('BackOffice.Submissions.AuthorCard.InstitutionLabel', 'Kurum / Üniversite', 'Institution / University')}:</span>
                                <span class="institution text-break"></span>
                            </span>
                            <span class="submission-author-card-meta-item d-inline-flex align-items-center gap-1">
                                <span class="submission-author-card-meta-label text-neutral-500 fw-semibold text-nowrap">${tx('BackOffice.Submissions.AuthorCard.OrcidLabel', 'ORCID', 'ORCID')}:</span>
                                <span class="orcid text-break"></span>
                            </span>
                        </div>
                    </div>
                    <div class="author-actions d-flex align-items-center justify-content-end gap-2 flex-shrink-0"></div>
                </div>`;

            wrapper.querySelector('.author-display-name').textContent = buildAuthorDisplayName(resolvedTitleName, author.fullName);
            wrapper.querySelector('.institution').textContent = author.institution || '-';
            wrapper.querySelector('.orcid').textContent = author.orcid || '-';
            wrapper.querySelector('.email').textContent = author.email || '-';

            const actions = wrapper.querySelector('.author-actions');

            if (allowAuthorEdit) {
                const editButton = document.createElement('button');
                editButton.type = 'button';
                editButton.className = 'btn btn-primary-100 text-primary-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center';
                editButton.setAttribute('data-edit-author', String(index));
                editButton.setAttribute('aria-label', tx('BackOffice.Submissions.AuthorList.EditAriaLabel', 'Yazarı düzenle', 'Edit author'));
                editButton.innerHTML = '<i class="ri-edit-2-line"></i>';
                actions.appendChild(editButton);
            }

            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 w-40-px h-40-px d-flex align-items-center justify-content-center';
            removeButton.setAttribute('data-remove-author', String(index));
            removeButton.setAttribute('aria-label', tx('BackOffice.Submissions.AuthorList.DeleteAriaLabel', 'Yazarı sil', 'Delete author'));
            removeButton.innerHTML = '<i class="ri-delete-bin-line"></i>';
            actions.appendChild(removeButton);

            list.appendChild(wrapper);
        });

        list.querySelectorAll('[data-edit-author]').forEach(button => {
            button.addEventListener('click', function () {
                startEditAuthor(Number(this.dataset.editAuthor));
            });
        });

        list.querySelectorAll('[data-remove-author]').forEach(button => {
            button.addEventListener('click', function () {
                removeAuthor(Number(this.dataset.removeAuthor));
            });
        });
    }

    function buildAuthorDisplayName(titleName, fullName) {
        const title = (titleName || '').trim();
        const name = (fullName || '').trim();

        if (!title) return name || '-';
        if (!name) return title;

        return `${title} ${name}`;
    }


    function buildFullName(firstName, lastName) {
        return [normalizePersonName(firstName), normalizeSurname(lastName)]
            .filter(value => !isBlank(value))
            .join(' ')
            .trim();
    }

    function normalizePersonName(value) {
        if (window.Symplify?.TextNormalizer?.titleCase) {
            return window.Symplify.TextNormalizer.titleCase(value);
        }

        return String(value || '').replace(/\s+/g, ' ').trim();
    }

    function normalizeSurname(value) {
        if (window.Symplify?.TextNormalizer?.upperTr) {
            return window.Symplify.TextNormalizer.upperTr(value);
        }

        return String(value || '').replace(/\s+/g, ' ').trim().toLocaleUpperCase('tr-TR');
    }

    function normalizeInstitution(value) {
        if (window.Symplify?.TextNormalizer?.normalizeInstitution) {
            return window.Symplify.TextNormalizer.normalizeInstitution(value);
        }

        return String(value || '').replace(/\s+/g, ' ').trim();
    }

    function normalizeFullName(firstName, lastName, fullName) {
        const normalizedFirstName = normalizePersonName(firstName);
        const normalizedLastName = normalizeSurname(lastName);
        if (normalizedFirstName || normalizedLastName) {
            return buildFullName(normalizedFirstName, normalizedLastName);
        }

        const parts = splitFullName(fullName);
        return buildFullName(parts.firstName, parts.lastName);
    }

    function splitFullName(fullName) {
        const value = String(fullName || '').replace(/\s+/g, ' ').trim();
        if (!value) return { firstName: '', lastName: '' };

        const parts = value.split(' ').filter(Boolean);
        if (parts.length === 1) return { firstName: normalizePersonName(parts[0]), lastName: '' };

        return {
            firstName: normalizePersonName(parts.slice(0, -1).join(' ')),
            lastName: normalizeSurname(parts.slice(-1).join(' '))
        };
    }

    function findAuthorIndexByEmail(email, exceptIndex) {
        const normalizedEmail = normalizeEmail(email);
        if (!normalizedEmail) return -1;

        return authors.findIndex((author, index) =>
            index !== exceptIndex && normalizeEmail(author.email) === normalizedEmail
        );
    }

    function findDuplicateAuthorEmail() {
        const seen = new Set();

        for (const author of authors) {
            const normalizedEmail = normalizeEmail(author.email);
            if (!normalizedEmail) continue;
            if (seen.has(normalizedEmail)) return author.email;
            seen.add(normalizedEmail);
        }

        return '';
    }

    function normalizeEmail(value) {
        return String(value || '').trim().toLocaleLowerCase('en-US');
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
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

    function initializeValidationSummary() {
        if (!validationSummary) return;

        syncValidationSummary();

        if (window.jQuery && $.validator) {
            $(form)
                .off('invalid-form.validate.submissionValidationSummary')
                .on('invalid-form.validate.submissionValidationSummary', function () {
                    window.requestAnimationFrame(syncValidationSummary);
                });
        }
    }

    function syncValidationSummary() {
        if (!validationSummary) return;

        const hasListErrors = Array.from(
            validationSummary.querySelectorAll('li')
        ).some(item => String(item.textContent || '').trim().length > 0);

        const hasSummaryErrors =
            validationSummary.classList.contains('validation-summary-errors') &&
            hasListErrors;

        validationSummary.classList.toggle('d-none', !hasSummaryErrors);
    }

    function initializeSubmitFlow() {
        let confirmedSubmitForReview = false;
        let isSubmitting = false;

        form.addEventListener('submit', function (event) {
            const submitter = event.submitter;
            const actionValue = submitter && submitter.name === 'SubmitAction'
                ? submitter.value
                : (form.querySelector('input[name="SubmitAction"]') || {}).value;

            ensureSubmitAction(actionValue === 'submit' ? 'submit' : 'draft');

            if (isSubmitting) {
                event.preventDefault();
                return;
            }

            prepareEditorsForSubmit();

            if (!validateAuthorStateBeforeSubmit() || !isFormValid()) {
                event.preventDefault();
                closeProcessing();
                return;
            }

            if (actionValue === 'submit' && confirmedSubmitForReview !== true) {
                event.preventDefault();
                askSubmitForReviewConfirmation(submitter);
                return;
            }

            isSubmitting = true;
            disableSubmitButtons();
            showProcessing(actionValue);
        });

        function askSubmitForReviewConfirmation(submitter) {
            const title = tx('BackOffice.Submissions.Create.ConfirmSubmit.Title', 'Bildiri onaya gönderilsin mi?', 'Send submission for approval?');
            const text = tx('BackOffice.Submissions.Create.ConfirmSubmit.Text', 'Onaya gönderildikten sonra bildiriyi güncelleyemeyebilirsiniz. Devam etmek istediğinize emin misiniz?', 'After sending it for approval, you may not be able to update the submission. Are you sure you want to continue?');

            if (!window.Swal || typeof window.Swal.fire !== 'function') {
                if (window.confirm(`${title}\n\n${text}`)) {
                    confirmedSubmitForReview = true;
                    if (!submitter) ensureSubmitAction('submit');
                    form.requestSubmit(submitter || undefined);
                }
                return;
            }

            window.Swal.fire({
                icon: 'warning',
                title: title,
                text: text,
                showCancelButton: true,
                confirmButtonText: tx('BackOffice.Submissions.Create.ConfirmSubmit.ConfirmButton', 'Evet, onaya gönder', 'Yes, send for approval'),
                cancelButtonText: tx('Common.Cancel', 'Vazgeç', 'Cancel'),
                reverseButtons: true,
                focusCancel: true
            }).then(result => {
                if (!result.isConfirmed) return;

                confirmedSubmitForReview = true;
                if (!submitter) ensureSubmitAction('submit');
                form.requestSubmit(submitter || undefined);
            });
        }
    }

    function validateAuthorStateBeforeSubmit() {
        if (!list || !hidden || !badge || !addButton) return true;

        clearAuthorCollectionError();

        // Yazar ekleme alanı transient bir formdur; asıl submit edilen veri authors hidden collection'dır.
        // Ancak henüz hiç yazar eklenmemişse submit sırasında kullanıcıyı direkt ilgili inputların altında yönlendirelim.
        if (authors.length === 0) {
            const validation = validateAuthorForm({ showErrors: true });

            if (!validation.isValid) {
                setAuthorCollectionError(tx('BackOffice.Submissions.AuthorList.Validation.AtLeastOneCorrespondingAuthor', 'En az bir sorumlu yazar eklenmelidir.', 'At least one corresponding author must be added.'));
                return false;
            }

            setAuthorCollectionError(tx('BackOffice.Submissions.AuthorForm.Validation.PendingAuthorNotAdded', 'Yazar bilgilerini listeye eklemek için önce “Yazarı Listeye Ekle” butonuna basınız.', 'Click “Add Author to List” before submitting the author information.'));
            addButton.focus();
            return false;
        }

        // Kullanıcı ikinci/ek yazar girmeye başlamış ama listeye eklememişse bu taslak inputları da validate edilir.
        if (hasPendingAuthorFormValues()) {
            const validation = validateAuthorForm({ showErrors: true });
            if (!validation.isValid) return false;

            setAuthorCollectionError(tx('BackOffice.Submissions.AuthorForm.Validation.PendingAuthorNotAdded', 'Yazar bilgilerini listeye eklemek için önce “Yazarı Listeye Ekle” butonuna basınız.', 'Click “Add Author to List” before submitting the author information.'));
            addButton.focus();
            return false;
        }

        return validateAuthorCollection();
    }

    function hasPendingAuthorFormValues() {
        return [
            document.getElementById('authorTitleId')?.value,
            document.getElementById('authorFirstName')?.value,
            document.getElementById('authorLastName')?.value,
            document.getElementById('authorFullName')?.value,
            document.getElementById('authorEmail')?.value,
            document.getElementById('authorOrcid')?.value,
            document.getElementById('authorInstitution')?.value,
            document.getElementById('authorRole')?.value
        ].some(value => !isBlank(value));
    }

    function validateAuthorCollection() {
        if (authors.length === 0 || !authors.some(author => author.isCorrespondingAuthor)) {
            setAuthorCollectionError(tx('BackOffice.Submissions.AuthorList.Validation.AtLeastOneCorrespondingAuthor', 'En az bir sorumlu yazar eklenmelidir.', 'At least one corresponding author must be added.'));
            list.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return false;
        }

        const duplicateEmail = findDuplicateAuthorEmail();
        if (duplicateEmail) {
            setAuthorCollectionError(tx('BackOffice.Submissions.AuthorList.Validation.DuplicateEmail', 'Aynı e-posta adresiyle birden fazla yazar eklenemez.', 'Multiple authors cannot be added with the same email address.') + ' (' + duplicateEmail + ')');
            list.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return false;
        }

        const invalidTitle = authors.find(author => isBlank(author.titleId));
        if (invalidTitle) {
            setAuthorCollectionError(tx('BackOffice.Submissions.AuthorList.Validation.TitleRequired', 'Listedeki her yazar için unvan seçilmelidir.', 'A title must be selected for every listed author.'));
            list.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return false;
        }

        const invalidEmail = authors.find(author => isBlank(author.email) || !isValidEmail(author.email));
        if (invalidEmail) {
            setAuthorCollectionError(tx('BackOffice.Submissions.AuthorList.Validation.EmailRequired', 'Listedeki her yazar için geçerli e-posta girilmelidir.', 'A valid email address must be entered for every listed author.'));
            list.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return false;
        }

        const invalidInstitution = authors.find(author => isBlank(author.institution));
        if (invalidInstitution) {
            setAuthorCollectionError(tx('BackOffice.Submissions.AuthorList.Validation.InstitutionRequired', 'Listedeki her yazar için kurum bilgisi girilmelidir.', 'Institution information must be entered for every listed author.'));
            list.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return false;
        }

        return true;
    }

    function prepareEditorsForSubmit() {
        if (window.tinymce && typeof window.tinymce.triggerSave === 'function') {
            window.tinymce.triggerSave();
        }

        if (window.Symplify.Forms && typeof window.Symplify.Forms.syncEditors === 'function') {
            window.Symplify.Forms.syncEditors($(form));
        }

        if (window.Symplify.TinyMce && typeof window.Symplify.TinyMce.syncAll === 'function') {
            window.Symplify.TinyMce.syncAll($(form));
        }
    }

    function isFormValid() {
        const isRichTextValid = validateRichTextRequiredFields({ showErrors: true, scrollToFirst: true });

        if (window.jQuery && $.validator && $(form).data('validator')) {
            const isJQueryValid = $(form).valid();
            syncValidationSummary();

            return isJQueryValid && isRichTextValid;
        }

        const isNativeValid = typeof form.checkValidity === 'function' ? form.checkValidity() : true;
        return isNativeValid && isRichTextValid;
    }

    function initializeRichTextValidation() {
        const textareas = Array.from(form.querySelectorAll('textarea[data-symplify-editor][data-val-required]'));
        if (textareas.length === 0) return;

        textareas.forEach(textarea => {
            textarea.addEventListener('input', function () {
                clearRichTextError(textarea);
            });

            textarea.addEventListener('change', function () {
                validateRichTextField(textarea, { showErrors: true });
            });

            textarea.addEventListener('blur', function () {
                validateRichTextField(textarea, { showErrors: true });
            });
        });

        bindTinyMceRequiredValidation(textareas, 0);
    }

    function bindTinyMceRequiredValidation(textareas, attempt) {
        const pending = [];

        textareas.forEach(textarea => {
            const editor = getTinyMceEditor(textarea);
            if (!editor) {
                pending.push(textarea);
                return;
            }

            if (editor.__symplifyRequiredValidationBound) return;

            editor.__symplifyRequiredValidationBound = true;

            editor.on('input keyup change setcontent undo redo', function () {
                const text = getRichTextPlainText(textarea);
                if (!isBlank(text)) {
                    clearRichTextError(textarea);
                }
            });

            editor.on('blur', function () {
                validateRichTextField(textarea, { showErrors: true });
            });
        });

        if (pending.length > 0 && attempt < 20) {
            window.setTimeout(function () {
                bindTinyMceRequiredValidation(pending, attempt + 1);
            }, 250);
        }
    }

    function validateRichTextRequiredFields(options) {
        const scrollToFirst = options?.scrollToFirst === true;
        const textareas = Array.from(form.querySelectorAll('textarea[data-symplify-editor][data-val-required]'));
        let firstInvalid = null;

        textareas.forEach(textarea => {
            const isValid = validateRichTextField(textarea, options);
            if (!isValid && !firstInvalid) firstInvalid = textarea;
        });

        if (firstInvalid && scrollToFirst) {
            activateContainingTab(firstInvalid);
            window.setTimeout(function () {
                scrollToRichText(firstInvalid);
                focusRichText(firstInvalid);
            }, 120);
        }

        return !firstInvalid;
    }

    function validateRichTextField(textarea, options) {
        const showErrors = options?.showErrors !== false;
        const text = getRichTextPlainText(textarea);

        if (isBlank(text)) {
            if (showErrors) {
                const message = textarea.getAttribute('data-val-required') || tx('BackOffice.Submissions.Create.Validation.AbstractRequired', 'Özet zorunludur.', 'Abstract is required.');
                setRichTextError(textarea, message);
            }

            return false;
        }

        clearRichTextError(textarea);
        return true;
    }

    function getTinyMceEditor(textarea) {
        if (!window.tinymce || !textarea || !textarea.id || typeof window.tinymce.get !== 'function') return null;
        return window.tinymce.get(textarea.id);
    }

    function getRichTextPlainText(textarea) {
        const editor = getTinyMceEditor(textarea);

        if (editor && typeof editor.getContent === 'function') {
            return normalizeEditorText(editor.getContent({ format: 'text' }));
        }

        return normalizeEditorText(stripHtml(textarea.value || ''));
    }

    function stripHtml(value) {
        const div = document.createElement('div');
        div.innerHTML = value || '';
        return div.textContent || div.innerText || '';
    }

    function normalizeEditorText(value) {
        return String(value || '')
            .replace(/ /g, ' ')
            .replace(/​/g, '')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function setRichTextError(textarea, message) {
        const target = getValidationTarget(textarea);
        if (target) {
            target.textContent = message;
            target.classList.remove('field-validation-valid');
            target.classList.add('field-validation-error');
        }

        textarea.classList.add('is-invalid');
        getTinyMceEditor(textarea)?.getContainer()?.classList.add('is-invalid');
    }

    function clearRichTextError(textarea) {
        const target = getValidationTarget(textarea);
        if (target) {
            target.textContent = '';
            target.classList.remove('field-validation-error');
            target.classList.add('field-validation-valid');
        }

        textarea.classList.remove('is-invalid');
        getTinyMceEditor(textarea)?.getContainer()?.classList.remove('is-invalid');
    }

    function getValidationTarget(textarea) {
        const name = textarea.name || textarea.id;
        if (!name) return null;

        return Array.from(form.querySelectorAll('[data-richtext-validation-for], [data-valmsg-for]'))
            .find(element =>
                element.getAttribute('data-richtext-validation-for') === name ||
                element.getAttribute('data-valmsg-for') === name
            ) || null;
    }

    function activateContainingTab(element) {
        const pane = element.closest('.tab-pane');
        if (!pane || pane.classList.contains('active')) return;

        const trigger = form.querySelector(`[data-bs-target="#${pane.id}"], [href="#${pane.id}"]`);
        if (!trigger) return;

        if (window.bootstrap && window.bootstrap.Tab) {
            window.bootstrap.Tab.getOrCreateInstance(trigger).show();
            return;
        }

        trigger.click();
    }

    function scrollToRichText(textarea) {
        const container = getTinyMceEditor(textarea)?.getContainer() || textarea;
        container.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function focusRichText(textarea) {
        const editor = getTinyMceEditor(textarea);
        if (editor && typeof editor.focus === 'function') {
            editor.focus();
            return;
        }

        textarea.focus();
    }

    function showProcessing(actionValue) {
        const isSubmitForReview = actionValue === 'submit';
        const title = isSubmitForReview
            ? tx('BackOffice.Submissions.Create.Processing.SubmitTitle', 'Bildiri onaya gönderiliyor', 'Sending submission for approval')
            : tx('BackOffice.Submissions.Create.Processing.DraftTitle', 'Bildiri kaydediliyor', 'Saving submission');
        const text = tx('BackOffice.Submissions.Create.Processing.Text', 'Özet metni ve dosyalar işleniyor. Lütfen sayfayı kapatmayın.', 'The abstract text and files are being processed. Please do not close the page.');

        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showLoading === 'function') {
            window.Symplify.Ajax.showLoading(title, text);
            return;
        }

        if (window.Swal && typeof window.Swal.fire === 'function') {
            window.Swal.fire({
                title: title,
                text: text,
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: function () {
                    window.Swal.showLoading();
                }
            });
        }
    }

    function closeProcessing() {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.closeLoading === 'function') {
            window.Symplify.Ajax.closeLoading();
            return;
        }

        if (window.Swal && typeof window.Swal.close === 'function') {
            window.Swal.close();
        }
    }

    function disableSubmitButtons() {
        form.querySelectorAll('button[type="submit"]').forEach(button => {
            button.disabled = true;
            button.classList.add('disabled');
        });
    }

    function ensureSubmitAction(value) {
        let hidden = form.querySelector('input[type="hidden"][name="SubmitAction"]');
        if (!hidden) {
            hidden = document.createElement('input');
            hidden.type = 'hidden';
            hidden.name = 'SubmitAction';
            form.appendChild(hidden);
        }

        hidden.value = value;
    }

    function getAuthorInput(field) {
        if (field === 'title') {
            return authorTitleCombobox?.input || document.getElementById('authorTitleId');
        }

        return document.querySelector(`[data-author-field="${field}"]`);
    }

    function focusAuthorField(field) {
        if (field === 'title' && authorTitleCombobox) {
            authorTitleCombobox.focus();
            return;
        }

        getAuthorInput(field)?.focus();
    }

    function setAuthorFieldError(field, message) {
        const target = document.querySelector(`[data-author-validation-for="${field}"]`);
        if (target) target.textContent = message;

        const input = getAuthorInput(field);
        if (input) input.classList.add('is-invalid');

        if (field === 'title') {
            document.getElementById('authorTitleId')?.classList.add('is-invalid');
        }
    }

    function clearAuthorFieldError(field) {
        const target = document.querySelector(`[data-author-validation-for="${field}"]`);
        if (target) target.textContent = '';

        const input = getAuthorInput(field);
        if (input) input.classList.remove('is-invalid');

        if (field === 'title') {
            document.getElementById('authorTitleId')?.classList.remove('is-invalid');
        }
    }

    function clearAllAuthorFieldErrors() {
        ['title', 'firstName', 'lastName', 'email', 'institution', 'role'].forEach(clearAuthorFieldError);
    }

    function setAuthorCollectionError(message) {
        if (authorCollectionValidation) {
            authorCollectionValidation.textContent = message;
            authorCollectionValidation.classList.remove('field-validation-valid');
            authorCollectionValidation.classList.add('field-validation-error');
        }
    }

    function clearAuthorCollectionError() {
        if (authorCollectionValidation) {
            authorCollectionValidation.textContent = '';
            authorCollectionValidation.classList.remove('field-validation-error');
            authorCollectionValidation.classList.add('field-validation-valid');
        }
    }

    function isValidEmail(value) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value || '');
    }

    function isBlank(value) {
        return !String(value || '').trim();
    }

    function normalizeForSearch(value) {
        return String(value || '')
            .toLocaleLowerCase('tr-TR')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '');
    }
}());
