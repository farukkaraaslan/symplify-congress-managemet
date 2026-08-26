window.Symplify = window.Symplify || {};
window.Symplify.CongressSubmissionTypes = window.Symplify.CongressSubmissionTypes || {};

window.Symplify.CongressSubmissionTypes.Index = (function ($) {
    'use strict';

    const selectors = {
        panel: '#congressSubmissionTypePanel',
        modal: '#congressSubmissionTypeSelectionModal',
        badges: '#congressSubmissionTypeSelectedBadges',
        emptyState: '#congressSubmissionTypeEmptyState',
        loading: '.congress-submission-type-selection-loading',
        list: '.congress-submission-type-selection-list',
        optionsEmpty: '.congress-submission-type-selection-empty',
        option: '.js-congress-submission-type-option',
        saveButton: '#saveCongressSubmissionTypeSelectionsButton'
    };

    function init() {
        const $panel = $(selectors.panel);

        if (!$panel.length) {
            return;
        }

        ensureModalAttachedToBody();
        bindModalCleanup();
        loadSelected();

        $(document)
            .off('shown.bs.modal.congressSubmissionTypes', selectors.modal)
            .on('shown.bs.modal.congressSubmissionTypes', selectors.modal, loadOptions);

        $(document)
            .off('change.congressSubmissionTypes', selectors.option)
            .on('change.congressSubmissionTypes', selectors.option, updateOptionVisualState);

        $(document)
            .off('click.congressSubmissionTypes', selectors.saveButton)
            .on('click.congressSubmissionTypes', selectors.saveButton, saveSelections);
    }

    function ensureModalAttachedToBody() {
        const $modal = $(selectors.modal);

        if (!$modal.length) {
            return;
        }

        if (!$modal.parent().is('body')) {
            $modal.appendTo(document.body);
        }
    }

    function bindModalCleanup() {
        $(document)
            .off('hidden.bs.modal.congressSubmissionTypesCleanup', selectors.modal)
            .on('hidden.bs.modal.congressSubmissionTypesCleanup', selectors.modal, cleanupModalArtifacts);
    }

    function cleanupModalArtifacts() {
        if ($('.modal.show').length) {
            return;
        }

        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css({ overflow: '', paddingRight: '' });
    }

    function loadSelected() {
        const $panel = $(selectors.panel);
        const url = $panel.data('selected-url');

        if (!url) {
            return;
        }

        $.ajax({
            url: url,
            type: 'GET',
            headers: getAjaxHeaders($panel)
        })
            .done(function (response) {
                if (!response || response.success === false) {
                    showError(response);
                    return;
                }

                renderSelected(response.items || []);
            })
            .fail(showError);
    }

    function loadOptions() {
        const $panel = $(selectors.panel);
        const $modal = $(selectors.modal);
        const url = $panel.data('options-url');

        if (!url || !$modal.length) {
            return;
        }

        $modal.find(selectors.loading).removeClass('d-none');
        $modal.find(selectors.list).addClass('d-none').empty();
        $modal.find(selectors.optionsEmpty).addClass('d-none');

        $.ajax({
            url: url,
            type: 'GET',
            headers: getAjaxHeaders($panel)
        })
            .done(function (response) {
                if (!response || response.success === false) {
                    showError(response);
                    return;
                }

                renderOptions(response.items || []);
            })
            .fail(showError)
            .always(function () {
                $modal.find(selectors.loading).addClass('d-none');
            });
    }

    function saveSelections() {
        const $panel = $(selectors.panel);
        const $modal = $(selectors.modal);
        const url = $panel.data('save-url');
        const congressId = $panel.data('congress-id');

        if (!url || !congressId) {
            return;
        }

        const selectedIds = $modal.find(selectors.option + ':checked')
            .map(function () { return $(this).val(); })
            .get()
            .filter(Boolean);

        const $button = $(selectors.saveButton);
        const originalHtml = $button.html();

        $button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>' + getText('saving', 'Kaydediliyor...'));

        $.ajax({
            url: url,
            type: 'POST',
            traditional: true,
            data: {
                congressId: congressId,
                selectedSubmissionTypeIds: selectedIds
            },
            headers: getAjaxHeaders($panel)
        })
            .done(function (response) {
                if (!response || response.success !== true) {
                    showError(response);
                    return;
                }

                hideModal($modal);
                loadSelected();
                showSuccess(response.message || getText('saved', 'Seçimler kaydedildi.'));
            })
            .fail(showError)
            .always(function () {
                $button.prop('disabled', false).html(originalHtml);
            });
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
            const text = item.name || item.code || '-';
            const badgeClass = item.submissionTypeIsActive === false
                ? 'bg-warning-light text-warning'
                : 'bg-success-light text-success';

            $('<span/>', {
                class: 'badge ' + badgeClass + ' px-12 py-8 rounded-pill',
                text: text
            }).appendTo($container);
        });
    }

    function renderOptions(items) {
        const $modal = $(selectors.modal);
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

        items.forEach(function (item) {
            $list.append(buildOption(item));
        });

        $list.find(selectors.option).each(function () {
            updateOptionVisualState.call(this);
        });
    }

    function buildOption(item) {
        const id = item.submissionTypeId || '';
        const name = item.name || item.code || '-';
        const description = item.description || item.code || '';
        const checked = item.isSelected === true ? ' checked' : '';
        const disabled = item.isActive === false && item.isSelected !== true ? ' disabled' : '';

        return `
            <div class="col-md-6">
                <div class="py-8 px-12 bg-base border radius-8 h-100">
                    <div class="form-switch switch-success d-flex align-items-start gap-2 min-w-0 mb-0">
                        <input class="form-check-input js-congress-submission-type-option" role="switch" type="checkbox" value="${escapeHtml(id)}"${checked}${disabled} />
                        <label class="form-check-label fw-medium text-truncate mb-0 flex-grow-1">
                            <span class="js-congress-submission-type-option-label d-block text-truncate">${escapeHtml(name)}</span>
                            ${description ? `<small class="text-neutral-500 d-block text-truncate">${escapeHtml(description)}</small>` : ''}
                        </label>
                    </div>
                </div>
            </div>`;
    }

    function updateOptionVisualState() {
        const $input = $(this);
        const $label = $input.closest('.form-switch').find('.js-congress-submission-type-option-label');

        if ($input.is(':checked')) {
            $label.removeClass('text-secondary-light').addClass('text-success');
            return;
        }

        $label.removeClass('text-success').addClass('text-secondary-light');
    }

    function getAjaxHeaders($context) {
        const headers = { 'X-Culture': getCurrentCulture() };
        const token = $context.find('input[name="__RequestVerificationToken"]').first().val()
            || $('input[name="__RequestVerificationToken"]').first().val();

        if (token) {
            headers.RequestVerificationToken = token;
        }

        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }

    function hideModal($modal) {
        if (!$modal.length) return;

        ensureModalAttachedToBody();

        if (window.bootstrap && window.bootstrap.Modal) {
            const instance = window.bootstrap.Modal.getInstance($modal[0]);
            if (instance) {
                instance.hide();
                return;
            }
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

    if (window.Symplify.CongressSubmissionTypes && window.Symplify.CongressSubmissionTypes.Index) {
        window.Symplify.CongressSubmissionTypes.Index.init();
    }
});
