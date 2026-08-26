window.Symplify = window.Symplify || {};

window.Symplify.Dropzone = (function ($) {
    'use strict';

    const defaults = {
        selector: '[data-symplify-dropzone]',
        fileNameTarget: '[data-dropzone-file-name], .js-dropzone-file-name, .js-slider-image-file-name',
        previewTarget: '[data-dropzone-preview], .js-dropzone-preview',
        errorTarget: '[data-dropzone-error], .js-dropzone-error',
        dragActiveClass: 'border-primary-600 bg-primary-50',
        invalidClass: 'border-danger',
        get invalidFileText() { return (window.Symplify && window.Symplify.t) ? window.Symplify.t('BackOffice.Common.File.InvalidFile', 'Seçilen dosya bu alan için uygun değil.') : 'Seçilen dosya bu alan için uygun değil.'; },
        get fileTooLargeText() { return (window.Symplify && window.Symplify.t) ? window.Symplify.t('BackOffice.Common.File.FileTooLarge', 'Seçilen dosya izin verilen boyuttan büyük.') : 'Seçilen dosya izin verilen boyuttan büyük.'; },
        maxSizeBytes: null,
        maxSizeMb: null,
        inputSelector: null,
        previewUrl: null,
        currentText: null,
        emptyText: null,
        accept: null,
        multiple: null,
        showImagePreview: true
    };

    function initAll(container, options) {
        const $container = normalizeContainer(container);
        const settings = $.extend({}, defaults, options || {});

        $container.find(settings.selector).each(function () {
            init(this, settings);
        });

        if ($container.is(settings.selector)) {
            init($container[0], settings);
        }
    }

    function init(element, options) {
        const $zone = $(element).first();
        if (!$zone.length) return null;

        const settings = buildSettings($zone, options);
        const $input = findInput($zone, settings);
        if (!$input.length) return null;

        const oldState = $zone.data('symplify-dropzone-state');
        if (oldState && oldState.previewUrl) revokePreviewUrl(oldState.previewUrl);

        const state = {
            $zone: $zone,
            $input: $input,
            settings: settings,
            defaultText: resolveDefaultText($zone, settings),
            previewUrl: null
        };

        $zone.data('symplify-dropzone-state', state);
        $input.data('symplify-dropzone-state', state);

        bindEvents(state);
        updateUi(state);

        return state;
    }

    function destroy(container) {
        const $container = normalizeContainer(container);

        $container.find(defaults.selector + ', .js-slider-image-dropzone').addBack(defaults.selector + ', .js-slider-image-dropzone').each(function () {
            const state = $(this).data('symplify-dropzone-state');
            if (!state) return;

            state.$zone.off('.symplifyDropzone');
            state.$input.off('.symplifyDropzone');
            state.$input.closest('form').off('reset.symplifyDropzone');
            revokePreviewUrl(state.previewUrl);
            state.$zone.removeData('symplify-dropzone-state');
            state.$input.removeData('symplify-dropzone-state');
        });
    }

    function reset(container) {
        const $container = normalizeContainer(container);

        $container.find(defaults.selector + ', .js-slider-image-dropzone').addBack(defaults.selector + ', .js-slider-image-dropzone').each(function () {
            const state = $(this).data('symplify-dropzone-state');
            if (!state) return;

            clearInput(state.$input[0]);
            clearError(state);
            updateUi(state);
        });
    }


    function resolveDefaultText($zone, settings) {
        if (settings.previewUrl && settings.currentText) return settings.currentText;
        if (!settings.previewUrl && settings.emptyText) return settings.emptyText;
        return settings.defaultText || getTargetText($zone, settings.fileNameTarget);
    }

    function bindEvents(state) {
        const $zone = state.$zone;
        const $input = state.$input;

        $zone.off('.symplifyDropzone');
        $input.off('.symplifyDropzone');

        $zone
            .on('click.symplifyDropzone', function (event) {
                if (shouldIgnoreClick(event, state)) return;
                event.preventDefault();
                $input.trigger('click');
            })
            .on('dragenter.symplifyDropzone dragover.symplifyDropzone', function (event) {
                if (isDisabled(state)) return;
                event.preventDefault();
                event.stopPropagation();
                $zone.addClass(state.settings.dragActiveClass);
            })
            .on('dragleave.symplifyDropzone dragend.symplifyDropzone', function (event) {
                event.preventDefault();
                event.stopPropagation();
                if (event.relatedTarget && $.contains($zone[0], event.relatedTarget)) return;
                $zone.removeClass(state.settings.dragActiveClass);
            })
            .on('drop.symplifyDropzone', function (event) {
                if (isDisabled(state)) return;
                event.preventDefault();
                event.stopPropagation();
                $zone.removeClass(state.settings.dragActiveClass);

                const originalEvent = event.originalEvent || event;
                const files = originalEvent.dataTransfer ? originalEvent.dataTransfer.files : null;
                if (!files || !files.length) return;

                assignFiles(state, files);
            });

        $input.on('change.symplifyDropzone', function () {
            clearError(state);
            if (!validateFiles(state, this.files)) {
                clearInput(this);
            }
            updateUi(state);
        });

        const $form = $input.closest('form');
        if ($form.length) {
            $form.off('reset.symplifyDropzone').on('reset.symplifyDropzone', function () {
                window.setTimeout(function () {
                    clearError(state);
                    updateUi(state);
                }, 0);
            });
        }
    }

    function buildSettings($zone, options) {
        const settings = $.extend({}, defaults, options || {});
        const data = $zone.data() || {};

        settings.inputSelector = data.dropzoneInput || settings.inputSelector;
        settings.fileNameTarget = data.dropzoneFileNameTarget || settings.fileNameTarget;
        settings.previewTarget = data.dropzonePreviewTarget || settings.previewTarget;
        settings.errorTarget = data.dropzoneErrorTarget || settings.errorTarget;
        settings.defaultText = data.dropzoneDefaultText || settings.defaultText;
        settings.previewUrl = data.dropzonePreviewUrl || settings.previewUrl;
        settings.currentText = data.dropzoneCurrentText || settings.currentText;
        settings.emptyText = data.dropzoneEmptyText || settings.emptyText;
        settings.invalidFileText = data.dropzoneInvalidFileText || settings.invalidFileText;
        settings.fileTooLargeText = data.dropzoneFileTooLargeText || settings.fileTooLargeText;
        settings.dragActiveClass = data.dropzoneDragActiveClass || settings.dragActiveClass;
        settings.invalidClass = data.dropzoneInvalidClass || settings.invalidClass;
        settings.accept = data.dropzoneAccept || settings.accept;

        if (data.dropzoneMaxSizeBytes !== undefined) settings.maxSizeBytes = parsePositiveInt(data.dropzoneMaxSizeBytes);
        if (data.dropzoneMaxSize !== undefined) settings.maxSizeBytes = parsePositiveInt(data.dropzoneMaxSize);
        if (data.dropzoneMaxSizeMb !== undefined) settings.maxSizeMb = Number(data.dropzoneMaxSizeMb);
        if (settings.maxSizeMb && settings.maxSizeMb > 0) settings.maxSizeBytes = Math.floor(settings.maxSizeMb * 1024 * 1024);
        if (data.dropzoneMultiple !== undefined) settings.multiple = parseBoolean(data.dropzoneMultiple);
        if (data.dropzoneShowImagePreview !== undefined) settings.showImagePreview = parseBoolean(data.dropzoneShowImagePreview);

        return settings;
    }

    function findInput($zone, settings) {
        if (settings.inputSelector) {
            const $bySelector = $(settings.inputSelector).first();
            if ($bySelector.length) return $bySelector;
        }

        const forId = $zone.attr('for');
        if (forId) {
            const $byFor = $('#' + escapeSelector(forId)).first();
            if ($byFor.length) return $byFor;
        }

        const $inside = $zone.find('input[type="file"]').first();
        if ($inside.length) return $inside;

        return $zone.closest('form').find('input[type="file"]').first();
    }

    function assignFiles(state, files) {
        const fileArray = normalizeFiles(state, files);
        clearError(state);

        if (!validateFiles(state, fileArray)) {
            clearInput(state.$input[0]);
            updateUi(state);
            return;
        }

        try {
            const dataTransfer = new DataTransfer();
            fileArray.forEach(function (file) { dataTransfer.items.add(file); });
            state.$input[0].files = dataTransfer.files;
            state.$input.trigger('change');
        } catch (error) {
            showError(state, state.settings.invalidFileText);
        }
    }

    function normalizeFiles(state, files) {
        const allFiles = Array.prototype.slice.call(files || []);
        const multiple = state.settings.multiple === null ? state.$input.prop('multiple') === true : state.settings.multiple === true;
        return multiple ? allFiles : allFiles.slice(0, 1);
    }

    function validateFiles(state, files) {
        const fileArray = Array.prototype.slice.call(files || []);
        const accept = state.settings.accept || state.$input.attr('accept') || '';
        const maxSizeBytes = state.settings.maxSizeBytes;

        for (let i = 0; i < fileArray.length; i++) {
            const file = fileArray[i];

            if (maxSizeBytes && file.size > maxSizeBytes) {
                showError(state, state.settings.fileTooLargeText);
                markInvalid(state, true);
                return false;
            }

            if (accept && !isAccepted(file, accept)) {
                showError(state, state.settings.invalidFileText);
                markInvalid(state, true);
                return false;
            }
        }

        markInvalid(state, false);
        return true;
    }

    function updateUi(state) {
        const files = Array.prototype.slice.call(state.$input[0].files || []);
        const text = files.length ? files.map(function (file) { return file.name; }).join(', ') : state.defaultText;

        findInZone(state.$zone, state.settings.fileNameTarget).text(text || '');
        updatePreview(state, files[0] || null);
    }

    function updatePreview(state, file) {
        const $preview = findInZone(state.$zone, state.settings.previewTarget).first();
        if (!$preview.length) return;

        revokePreviewUrl(state.previewUrl);
        state.previewUrl = null;

        if (!file) {
            if (state.settings.previewUrl && state.settings.showImagePreview) {
                $preview.attr('src', state.settings.previewUrl).removeClass('d-none');
            } else {
                $preview.addClass('d-none').removeAttr('src');
            }
            return;
        }

        if (!state.settings.showImagePreview || String(file.type || '').indexOf('image/') !== 0) {
            $preview.addClass('d-none').removeAttr('src');
            return;
        }

        state.previewUrl = URL.createObjectURL(file);
        $preview.attr('src', state.previewUrl).removeClass('d-none');
    }

    function isAccepted(file, accept) {
        const rules = String(accept || '').split(',').map(function (rule) { return rule.trim().toLowerCase(); }).filter(Boolean);
        if (!rules.length) return true;

        const fileName = String(file.name || '').toLowerCase();
        const fileType = String(file.type || '').toLowerCase();

        return rules.some(function (rule) {
            if (rule.charAt(0) === '.') return fileName.endsWith(rule);
            if (rule.endsWith('/*')) return fileType.indexOf(rule.slice(0, -1)) === 0;
            return fileType === rule;
        });
    }

    function shouldIgnoreClick(event, state) {
        if (isDisabled(state)) return true;
        const $target = $(event.target);
        if ($target.is('input[type="file"]')) return true;
        if ($target.closest('button, a, input:not([type="file"]), textarea, select').length) return true;
        if (state.$zone.is('label') && state.$zone.attr('for')) return true;
        return false;
    }

    function showError(state, message) {
        const $target = findInZone(state.$zone, state.settings.errorTarget).first();
        const text = String(message || '').trim();

        if ($target.length) {
            $target.text(text).toggleClass('d-none', !text);
            return;
        }

        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError({ message: text });
            return;
        }

        if (text) window.alert(text);
    }

    function clearError(state) {
        const $target = findInZone(state.$zone, state.settings.errorTarget).first();
        if ($target.length) $target.text('').addClass('d-none');
        markInvalid(state, false);
    }

    function markInvalid(state, invalid) {
        state.$zone.toggleClass(state.settings.invalidClass, invalid === true);
        state.$input.toggleClass('is-invalid input-validation-error', invalid === true);
    }

    function findInZone($zone, selector) {
        if (!selector) return $();
        const $items = $zone.find(selector);
        return $zone.is(selector) ? $items.add($zone) : $items;
    }

    function getTargetText($zone, selector) {
        const $target = findInZone($zone, selector).first();
        return $target.length ? $target.text().trim() : '';
    }

    function normalizeContainer(container) {
        if (!container) return $(document);
        return container.jquery ? container : $(container);
    }

    function isDisabled(state) {
        return state.$input.prop('disabled') === true || state.$input.prop('readonly') === true;
    }

    function clearInput(input) {
        try { input.value = ''; } catch (error) { $(input).val(''); }
    }

    function revokePreviewUrl(url) {
        if (url && window.URL && typeof window.URL.revokeObjectURL === 'function') {
            window.URL.revokeObjectURL(url);
        }
    }

    function parseBoolean(value) {
        if (typeof value === 'boolean') return value;
        return String(value).toLowerCase() === 'true';
    }

    function parsePositiveInt(value) {
        const number = parseInt(value, 10);
        return Number.isFinite(number) && number > 0 ? number : null;
    }

    function escapeSelector(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|/@])/g, '\\$1');
    }

    return {
        init: init,
        initAll: initAll,
        reset: reset,
        destroy: destroy
    };
})(jQuery);

$(function () {
    if (window.Symplify.Dropzone) {
        window.Symplify.Dropzone.initAll(document);
    }
});
