window.Symplify = window.Symplify || {};
window.Symplify.CongressTopics = window.Symplify.CongressTopics || {};

window.Symplify.CongressTopics.Index = (function ($) {
    'use strict';

    const selectors = {
        panel: '#congressTopicPanel',
        selectionModal: '#congressTopicSelectionModal',
        categoryModal: '#congressTopicCategoriesModal',
        badges: '#congressTopicSelectedBadges',
        emptyState: '#congressTopicEmptyState',
        loading: '.congress-topic-selection-loading',
        list: '.congress-topic-selection-list',
        optionsEmpty: '.congress-topic-selection-empty',
        option: '.js-congress-topic-option',
        categorySelect: '.js-congress-topic-category-select',
        saveButton: '#saveCongressTopicSelectionsButton',
        categoryLoading: '.congress-topic-category-loading',
        categoryList: '.congress-topic-category-list',
        categoryEmpty: '.congress-topic-category-empty',
        categoryRow: '.js-congress-topic-category-row',
        addCategoryButton: '#addCongressTopicCategoryButton',
        saveCategoriesButton: '#saveCongressTopicCategoriesButton'
    };

    const state = {
        categories: [],
        languages: []
    };

    function init() {
        const $panel = $(selectors.panel);
        if (!$panel.length) return;

        ensureModalsAttachedToBody();
        bindModalCleanup();
        loadSelected();

        $(document)
            .off('shown.bs.modal.congressTopicsSelection', selectors.selectionModal)
            .on('shown.bs.modal.congressTopicsSelection', selectors.selectionModal, loadOptions);

        $(document)
            .off('shown.bs.modal.congressTopicCategories', selectors.categoryModal)
            .on('shown.bs.modal.congressTopicCategories', selectors.categoryModal, loadCategories);

        $(document)
            .off('change.congressTopics', selectors.option)
            .on('change.congressTopics', selectors.option, updateOptionVisualState);

        $(document)
            .off('click.congressTopics', selectors.saveButton)
            .on('click.congressTopics', selectors.saveButton, saveSelections);

        $(document)
            .off('click.congressTopicCategories', selectors.addCategoryButton)
            .on('click.congressTopicCategories', selectors.addCategoryButton, addCategoryRow);

        $(document)
            .off('click.congressTopicCategories', '.js-remove-congress-topic-category')
            .on('click.congressTopicCategories', '.js-remove-congress-topic-category', function () {
                $(this).closest(selectors.categoryRow).remove();
                normalizeCategoryOrders();
                syncCategoryEmptyState();
            });

        $(document)
            .off('click.congressTopicCategories', selectors.saveCategoriesButton)
            .on('click.congressTopicCategories', selectors.saveCategoriesButton, saveCategories);
    }

    function ensureModalsAttachedToBody() {
        [selectors.selectionModal, selectors.categoryModal].forEach(function (selector) {
            const $modal = $(selector);
            if ($modal.length && !$modal.parent().is('body')) $modal.appendTo(document.body);
        });
    }

    function bindModalCleanup() {
        $(document)
            .off('hidden.bs.modal.congressTopicsCleanup')
            .on('hidden.bs.modal.congressTopicsCleanup', '.modal', cleanupModalArtifacts);
    }

    function cleanupModalArtifacts() {
        if ($('.modal.show').length) return;
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css({ overflow: '', paddingRight: '' });
    }

    function loadSelected() {
        const $panel = $(selectors.panel);
        const url = $panel.data('selected-url');
        if (!url) return;

        $.ajax({ url: url, type: 'GET', headers: getAjaxHeaders($panel) })
            .done(function (response) {
                if (!response || response.success === false) return showError(response);
                renderSelected(response.items || []);
            })
            .fail(showError);
    }

    function loadOptions() {
        const $panel = $(selectors.panel);
        const $modal = $(selectors.selectionModal);
        const url = $panel.data('options-url');
        if (!url || !$modal.length) return;

        $modal.find(selectors.loading).removeClass('d-none');
        $modal.find(selectors.list).addClass('d-none').empty();
        $modal.find(selectors.optionsEmpty).addClass('d-none');

        $.ajax({ url: url, type: 'GET', headers: getAjaxHeaders($panel) })
            .done(function (response) {
                if (!response || response.success === false) return showError(response);
                state.categories = response.categories || [];
                renderOptions(response.items || []);
            })
            .fail(showError)
            .always(function () { $modal.find(selectors.loading).addClass('d-none'); });
    }

    function saveSelections() {
        const $panel = $(selectors.panel);
        const $modal = $(selectors.selectionModal);
        const url = $panel.data('save-url');
        const congressId = $panel.data('congress-id');
        if (!url || !congressId) return;

        const selectedTopicIds = [];
        const selectedCategoryIds = [];

        $modal.find(selectors.option + ':checked').each(function () {
            const $input = $(this);
            const $card = $input.closest('.js-congress-topic-option-card');
            selectedTopicIds.push($input.val());
            selectedCategoryIds.push($card.find(selectors.categorySelect).val() || '');
        });

        const $button = $(selectors.saveButton);
        const originalHtml = $button.html();
        $button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>' + getText('saving', 'Kaydediliyor...'));

        $.ajax({
            url: url,
            type: 'POST',
            traditional: true,
            data: {
                congressId: congressId,
                selectedTopicIds: selectedTopicIds,
                selectedCategoryIds: selectedCategoryIds
            },
            headers: getAjaxHeaders($panel)
        })
            .done(function (response) {
                if (!response || response.success !== true) return showError(response);
                hideModal($modal);
                loadSelected();
                showSuccess(response.message || getText('saved', 'Seçimler kaydedildi.'));
            })
            .fail(showError)
            .always(function () { $button.prop('disabled', false).html(originalHtml); });
    }

    function renderSelected(items) {
        const $container = $(selectors.badges);
        const $emptyState = $(selectors.emptyState);
        $container.empty();

        if (!items.length) {
            $emptyState.removeClass('d-none');
            return;
        }

        $emptyState.addClass('d-none');
        items.forEach(function (item) {
            const topicText = item.name || item.code || '-';
            const text = item.categoryName ? item.categoryName + ' · ' + topicText : topicText;
            const badgeClass = item.topicIsActive === false
                ? 'bg-warning-light text-warning'
                : 'bg-success-light text-success';

            $('<span/>', {
                class: 'badge ' + badgeClass + ' px-12 py-8 rounded-pill',
                text: text
            }).appendTo($container);
        });
    }

    function renderOptions(items) {
        const $modal = $(selectors.selectionModal);
        const $list = $modal.find(selectors.list);
        const $empty = $modal.find(selectors.optionsEmpty);
        $list.empty();

        if (!items.length) {
            $list.addClass('d-none');
            $empty.removeClass('d-none');
            return;
        }

        $empty.addClass('d-none');
        $list.removeClass('d-none');
        items.forEach(function (item) { $list.append(buildOption(item)); });
        $list.find(selectors.option).each(function () { updateOptionVisualState.call(this); });
    }

    function buildOption(item) {
        const $panel = $(selectors.panel);
        const id = item.topicId || '';
        const name = item.name || item.code || '-';
        const description = item.description || item.code || '';
        const checked = item.isSelected === true ? ' checked' : '';
        const disabled = item.isActive === false && item.isSelected !== true ? ' disabled' : '';
        const categoryDisabled = item.isSelected === true ? '' : ' disabled';
        const noneText = getPanelText($panel, 'category-none-text', 'Kategori Yok');
        const categoryLabel = getPanelText($panel, 'category-label-text', 'Kategori');

        let categoryOptions = `<option value="">${escapeHtml(noneText)}</option>`;
        (state.categories || []).forEach(function (category) {
            const isSelectedCategory = item.categoryId && String(item.categoryId) === String(category.id);
            const selected = isSelectedCategory ? ' selected' : '';
            const optionDisabled = category.isActive === false && !isSelectedCategory ? ' disabled' : '';
            const passive = category.isActive === false
                ? ' (' + getPanelText($panel, 'category-passive-text', 'Pasif') + ')'
                : '';
            categoryOptions += `<option value="${escapeHtml(category.id)}"${selected}${optionDisabled}>${escapeHtml(category.name + passive)}</option>`;
        });

        return `
            <div class="col-md-6">
                <div class="py-10 px-12 bg-base border radius-8 h-100 js-congress-topic-option-card">
                    <div class="form-switch switch-success d-flex align-items-start gap-2 min-w-0 mb-8">
                        <input class="form-check-input js-congress-topic-option" role="switch" type="checkbox" value="${escapeHtml(id)}"${checked}${disabled} />
                        <label class="form-check-label fw-medium min-w-0 mb-0 flex-grow-1">
                            <span class="js-congress-topic-option-label d-block text-truncate">${escapeHtml(name)}</span>
                            ${description ? `<small class="text-neutral-500 d-block text-truncate">${escapeHtml(description)}</small>` : ''}
                        </label>
                    </div>
                    <label class="form-label text-xs fw-semibold mb-1">${escapeHtml(categoryLabel)}</label>
                    <select class="form-select form-select-sm radius-8 js-congress-topic-category-select"${categoryDisabled}>
                        ${categoryOptions}
                    </select>
                </div>
            </div>`;
    }

    function updateOptionVisualState() {
        const $input = $(this);
        const $card = $input.closest('.js-congress-topic-option-card');
        const $label = $card.find('.js-congress-topic-option-label');
        const selected = $input.is(':checked');

        $card.find(selectors.categorySelect).prop('disabled', !selected);
        $label.toggleClass('text-success', selected).toggleClass('text-secondary-light', !selected);
    }

    function loadCategories() {
        const $panel = $(selectors.panel);
        const $modal = $(selectors.categoryModal);
        const url = $panel.data('categories-url');
        if (!url || !$modal.length) return;

        $modal.find(selectors.categoryLoading).removeClass('d-none');
        $modal.find(selectors.categoryList).addClass('d-none').empty();
        $modal.find(selectors.categoryEmpty).addClass('d-none');

        $.ajax({ url: url, type: 'GET', headers: getAjaxHeaders($panel) })
            .done(function (response) {
                if (!response || response.success === false) return showError(response);
                state.languages = response.languages || [];
                state.categories = response.categories || [];
                renderCategories(state.categories);
            })
            .fail(showError)
            .always(function () { $modal.find(selectors.categoryLoading).addClass('d-none'); });
    }

    function renderCategories(categories) {
        const $modal = $(selectors.categoryModal);
        const $list = $modal.find(selectors.categoryList);
        $list.empty();
        (categories || []).forEach(function (category) { $list.append(buildCategoryRow(category)); });
        syncCategoryEmptyState();
    }

    function addCategoryRow() {
        const $list = $(selectors.categoryModal).find(selectors.categoryList);
        $list.append(buildCategoryRow({ id: null, order: $list.children().length + 1, isActive: true, translations: [] }));
        syncCategoryEmptyState();
        $list.children().last().find('.js-category-name-input').first().trigger('focus');
    }

    function buildCategoryRow(category) {
        const $panel = $(selectors.panel);
        const translations = category.translations || [];
        const translationByLanguage = {};
        translations.forEach(function (item) { translationByLanguage[String(item.languageId)] = item.name || ''; });

        const languageInputs = (state.languages || []).map(function (language) {
            const value = translationByLanguage[String(language.id)] || '';
            const required = language.isDefault ? ' <span class="text-danger">*</span>' : '';
            const defaultBadge = language.isDefault
                ? ` <span class="badge bg-primary-focus text-primary-600 rounded-pill">${escapeHtml(getPanelText($panel, 'default-text', 'Varsayılan'))}</span>`
                : '';

            return `
                <div class="col-md-6">
                    <label class="form-label text-sm fw-semibold mb-1">${escapeHtml(language.name || language.culture || '')}${required}${defaultBadge}</label>
                    <input type="text"
                           maxlength="200"
                           class="form-control radius-8 js-category-name-input"
                           data-language-id="${escapeHtml(language.id)}"
                           value="${escapeHtml(value)}" />
                </div>`;
        }).join('');

        return `
            <div class="border rounded-3 p-16 js-congress-topic-category-row" data-category-id="${escapeHtml(category.id || '')}">
                <div class="row g-3 align-items-end">
                    <div class="col-md-2">
                        <label class="form-label text-sm fw-semibold mb-1">${escapeHtml(getPanelText($panel, 'category-order-text', 'Sıra'))}</label>
                        <input type="number" min="1" class="form-control radius-8 js-category-order" value="${escapeHtml(category.order || 1)}" />
                    </div>
                    <div class="col-md-8">
                        <div class="row g-3">${languageInputs}</div>
                    </div>
                    <div class="col-md-2">
                        <div class="d-flex align-items-center justify-content-end gap-3 pb-2">
                            <label class="form-check d-inline-flex align-items-center gap-2 mb-0">
                                <input class="form-check-input js-category-active" type="checkbox"${category.isActive !== false ? ' checked' : ''} />
                                <span class="form-check-label text-sm">${escapeHtml(getPanelText($panel, 'category-active-text', 'Aktif'))}</span>
                            </label>
                            <button type="button" class="btn btn-sm btn-outline-danger radius-8 js-remove-congress-topic-category" title="${escapeHtml(getPanelText($panel, 'category-delete-text', 'Sil'))}">
                                <i class="ri-delete-bin-line"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>`;
    }

    function normalizeCategoryOrders() {
        $(selectors.categoryModal).find(selectors.categoryRow).each(function (index) {
            const $order = $(this).find('.js-category-order');
            if (!Number($order.val()) || Number($order.val()) <= 0) $order.val(index + 1);
        });
    }

    function syncCategoryEmptyState() {
        const $modal = $(selectors.categoryModal);
        const hasRows = $modal.find(selectors.categoryRow).length > 0;
        $modal.find(selectors.categoryList).toggleClass('d-none', !hasRows);
        $modal.find(selectors.categoryEmpty).toggleClass('d-none', hasRows);
    }

    function saveCategories() {
        const $panel = $(selectors.panel);
        const $modal = $(selectors.categoryModal);
        const url = $panel.data('save-categories-url');
        const congressId = $panel.data('congress-id');
        if (!url || !congressId) return;

        normalizeCategoryOrders();

        const categories = $modal.find(selectors.categoryRow).map(function () {
            const $row = $(this);
            return {
                id: $row.data('category-id') || null,
                order: Number($row.find('.js-category-order').val()) || 0,
                isActive: $row.find('.js-category-active').is(':checked'),
                translations: $row.find('.js-category-name-input').map(function () {
                    return {
                        languageId: $(this).data('language-id'),
                        name: $(this).val() || null
                    };
                }).get()
            };
        }).get();

        const $button = $(selectors.saveCategoriesButton);
        const originalHtml = $button.html();
        $button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>' + getText('saving', 'Kaydediliyor...'));

        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ congressId: congressId, categories: categories }),
            headers: getAjaxHeaders($panel)
        })
            .done(function (response) {
                if (!response || response.success !== true) return showError(response);
                hideModal($modal);
                loadSelected();
                showSuccess(response.message || getText('saved', 'Kategoriler kaydedildi.'));
            })
            .fail(showError)
            .always(function () { $button.prop('disabled', false).html(originalHtml); });
    }

    function getAjaxHeaders($context) {
        const headers = { 'X-Culture': getCurrentCulture() };
        const token = $context.find('input[name="__RequestVerificationToken"]').first().val()
            || $('input[name="__RequestVerificationToken"]').first().val();
        if (token) headers.RequestVerificationToken = token;
        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }

    function getPanelText($panel, key, fallback) {
        const value = $panel.data(key);
        const normalized = value ? String(value) : '';
        return normalized && !normalized.startsWith('BackOffice.') && !normalized.startsWith('Common.')
            ? normalized
            : fallback;
    }

    function hideModal($modal) {
        if (!$modal.length) return;
        ensureModalsAttachedToBody();
        if (window.bootstrap && window.bootstrap.Modal) {
            const instance = window.bootstrap.Modal.getInstance($modal[0]);
            if (instance) { instance.hide(); return; }
        }
        $modal.modal('hide');
    }

    function showSuccess(message) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function') {
            window.Symplify.Ajax.showSuccess(message);
        }
    }

    function showError(response) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(response);
            return;
        }
        console.error(response && response.responseJSON ? response.responseJSON.message : response);
    }

    function getText(key, fallback) {
        const texts = window.Symplify.Texts || window.Symplify.texts || {};
        return texts[key] || fallback;
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) return '';
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    return { init: init, reload: loadSelected };
})(jQuery);

$(function () {
    'use strict';
    if (window.Symplify.CongressTopics && window.Symplify.CongressTopics.Index) {
        window.Symplify.CongressTopics.Index.init();
    }
});
