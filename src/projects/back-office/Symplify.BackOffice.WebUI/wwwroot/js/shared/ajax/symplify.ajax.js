window.Symplify = window.Symplify || {};

window.Symplify.Ajax = (function ($) {
    'use strict';

    const defaultOptions = {
        successToastPosition: 'top-end',
        successToastTimer: 1800,
        infoToastTimer: 2200,
        confirmButtonColor: '#487FFF',
        dangerButtonColor: '#EF4A00',
        cancelButtonColor: '#6B7280'
    };

    function getAntiForgeryToken($container) {
        const $scope = $container && $container.length ? $container : $(document);
        let $token = $scope.find('input[name="__RequestVerificationToken"]').first();

        if (!$token.length && $scope.is('form')) {
            $token = $scope.find('input[name="__RequestVerificationToken"]').first();
        }

        if (!$token.length) {
            $token = $('input[name="__RequestVerificationToken"]').first();
        }

        return $token.length ? $token.val() : null;
    }

    function buildTokenHeader($container) {
        const token = getAntiForgeryToken($container);
        return token ? { RequestVerificationToken: token } : {};
    }

    function buildAjaxHeaders($container) {
        return $.extend({
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json',
            'X-Culture': getCurrentCulture()
        }, buildTokenHeader($container));
    }

    function postForm($form, options) {
        const settings = $.extend({ multipart: false, showLoading: false }, options || {});
        const ajaxOptions = {
            url: $form.attr('action'),
            type: $form.attr('method') || 'POST',
            headers: buildAjaxHeaders($form)
        };

        if (settings.multipart) {
            ajaxOptions.data = new FormData($form[0]);
            ajaxOptions.processData = false;
            ajaxOptions.contentType = false;
        } else {
            ajaxOptions.data = $form.serialize();
        }

        if (settings.showLoading === true) {
            ajaxOptions.beforeSend = function () {
                showLoading(settings.loadingTitle, settings.loadingText, settings.loadingOptions);
            };
            ajaxOptions.complete = function () {
                closeLoading();
            };
        }

        return $.ajax(ajaxOptions);
    }

    function postJson(url, payload, $container, options) {
        const settings = $.extend({ showLoading: false }, options || {});
        const ajaxOptions = {
            url: url,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify(payload || {}),
            headers: buildAjaxHeaders($container || $(document))
        };

        if (settings.showLoading === true) {
            ajaxOptions.beforeSend = function () {
                showLoading(settings.loadingTitle, settings.loadingText, settings.loadingOptions);
            };
            ajaxOptions.complete = function () {
                closeLoading();
            };
        }

        return $.ajax(ajaxOptions);
    }

    function showSuccess(message, options) {
        const resolvedMessage = normalizeMessage(message) || getText('successTitle', 'Başarılı');
        showToast($.extend({
            icon: 'success',
            title: resolvedMessage,
            timer: defaultOptions.successToastTimer
        }, options || {}));
    }

    function showInfo(message, options) {
        const resolvedMessage = normalizeMessage(message) || getText('infoTitle', 'Bilgi');
        showToast($.extend({
            icon: 'info',
            title: resolvedMessage,
            timer: defaultOptions.infoToastTimer
        }, options || {}));
    }

    function showWarning(message, options) {
        const resolvedMessage = normalizeMessage(message) || getText('warningTitle', 'Uyarı');
        showModal($.extend({
            icon: 'warning',
            title: getText('warningTitle', 'Uyarı'),
            text: resolvedMessage,
            confirmButtonText: getText('ok', 'Tamam'),
            confirmButtonColor: defaultOptions.confirmButtonColor
        }, options || {}));
    }

    function showError(response, options) {
        const message = extractMessage(response) || getText('genericError', 'İşlem sırasında bir hata oluştu.');
        const validationHtml = buildValidationErrorHtml(response);

        showModal($.extend({
            icon: 'error',
            title: getText('errorTitle', 'Hata'),
            html: validationHtml || escapeHtml(message),
            confirmButtonText: getText('ok', 'Tamam'),
            confirmButtonColor: defaultOptions.confirmButtonColor
        }, options || {}));
    }

    function confirm(options) {
        const settings = options || {};

        const title = resolveOptionText(settings.title, getText('deleteConfirmTitle', 'Emin misiniz?'));
        const text = resolveOptionText(settings.text, getText('deleteConfirmText', 'Bu işlem geri alınamayabilir.'));
        const confirmButtonText = resolveOptionText(settings.confirmButtonText, getText('deleteConfirmButton', 'Sil'));
        const cancelButtonText = resolveOptionText(settings.cancelButtonText, getText('cancel', 'Vazgeç'));

        if (isSweetAlertAvailable()) {
            return Swal.fire({
                icon: settings.icon || 'warning',
                title: title,
                text: text,
                html: settings.html,
                showCancelButton: true,
                confirmButtonText: confirmButtonText,
                cancelButtonText: cancelButtonText,
                confirmButtonColor: settings.confirmButtonColor || defaultOptions.dangerButtonColor,
                cancelButtonColor: settings.cancelButtonColor || defaultOptions.cancelButtonColor,
                reverseButtons: settings.reverseButtons !== false,
                focusCancel: settings.focusCancel !== false
            });
        }

        const isConfirmed = window.confirm(stripHtml(settings.html) || text || title);
        return $.Deferred().resolve({ isConfirmed: isConfirmed }).promise();
    }


    function showLoading(title, text, options) {
        const settings = options || {};
        const resolvedTitle = normalizeMessage(title) || getText('processingTitle', 'İşlem yapılıyor');
        const resolvedText = normalizeMessage(text) || getText('processingText', 'Lütfen bekleyin, işlem tamamlanıyor.');

        document.body.classList.add('symplify-is-processing');
        document.body.style.cursor = 'progress';

        if (isSweetAlertAvailable()) {
            Swal.fire($.extend({
                title: resolvedTitle,
                text: resolvedText,
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: function () {
                    Swal.showLoading();
                }
            }, settings));
        }
    }

    function closeLoading() {
        document.body.classList.remove('symplify-is-processing');
        document.body.style.cursor = '';

        if (isSweetAlertAvailable() && Swal.isVisible && Swal.isVisible()) {
            Swal.close();
        }
    }

    function handleJsonResult(response, options) {
        const settings = $.extend({ successMessage: null, showSuccess: true }, options || {});

        if (!response || response.success !== true) {
            showError(response);
            return false;
        }

        if (settings.showSuccess !== false) {
            showSuccess(response.message || settings.successMessage);
        }

        return true;
    }

    function reloadDataTable(tableSelector, resetPaging) {
        const $table = $(tableSelector);

        if ($.fn.DataTable && $table.length && $.fn.DataTable.isDataTable($table)) {
            $table.DataTable().ajax.reload(null, resetPaging === true);
            return true;
        }

        return false;
    }

    function extractMessage(response) {
        if (!response) return null;
        if (typeof response === 'string') return normalizeMessage(response);

        const json = response.responseJSON || response;
        const candidates = [
            json.message,
            json.detail,
            json.title,
            json.error,
            response.responseText,
            response.statusText
        ];

        for (let i = 0; i < candidates.length; i++) {
            const value = normalizeMessage(candidates[i]);
            if (value) return value;
        }

        return null;
    }

    function normalizeMessage(value) {
        if (value === null || value === undefined) return null;

        if (Array.isArray(value)) {
            const messages = value.map(normalizeMessage).filter(Boolean);
            return messages.length ? messages.join('\n') : null;
        }

        if (typeof value === 'object') {
            return normalizeMessage(value.message || value.title || value.detail || value.error);
        }

        const text = String(value).trim();
        if (!text) return null;

        const knownMessage = resolveKnownMessageKey(text);
        if (knownMessage) return knownMessage;

        if (isResourceKey(text)) {
            return getResourceText(text) || text;
        }

        return text;
    }

    function resolveKnownMessageKey(key) {
        const fallbackByKey = {
            'Common.Created': getText('created', 'Kayıt oluşturuldu.'),
            'Common.Updated': getText('updated', 'Kayıt güncellendi.'),
            'Common.Deleted': getText('deleted', 'Kayıt silindi.'),
            'Common.InvalidRequest': getText('invalidRequest', 'Geçersiz istek.'),
            'Common.Error': getText('genericError', 'İşlem sırasında bir hata oluştu.'),
            'Common.GenericError': getText('genericError', 'İşlem sırasında bir hata oluştu.'),
            'Common.ReorderSuccess': getText('reorderSuccess', 'Sıralama güncellendi.'),
            'Common.Saved': getText('saved', 'Kayıt kaydedildi.')
        };

        return Object.prototype.hasOwnProperty.call(fallbackByKey, key)
            ? fallbackByKey[key]
            : null;
    }

    function getText(key, fallback) {
        const sources = getTextSources();

        for (let i = 0; i < sources.length; i++) {
            const source = sources[i];
            if (!source || !Object.prototype.hasOwnProperty.call(source, key)) continue;

            const value = source[key];
            if (value === null || value === undefined) continue;

            const text = String(value).trim();
            if (!text) continue;

            return isResourceKey(text) ? (getResourceText(text) || fallback) : text;
        }

        return fallback;
    }

    function getTextSources() {
        return [
            window.Symplify.Texts,
            window.Symplify.texts,
            window.Symplify.Lookup && window.Symplify.Lookup.texts
        ];
    }

    function getResourceText(key) {
        if (!key) return null;

        const resources = window.Symplify.resources || {};
        const value = resources[key];

        if (value !== null && value !== undefined) {
            const text = String(value).trim();
            if (text && text !== key) return text;
        }

        if (typeof window.Symplify.t === 'function') {
            const translated = window.Symplify.t(key, null);
            if (translated !== null && translated !== undefined) {
                const text = String(translated).trim();
                if (text && text !== key) return text;
            }
        }

        return null;
    }

    function resolveOptionText(value, fallback) {
        return normalizeMessage(value) || fallback || '';
    }

    function isResourceKey(value) {
        return !!value && /^[A-Za-z0-9]+(\.[A-Za-z0-9]+)+$/.test(String(value).trim());
    }

    function showToast(options) {
        const settings = options || {};

        if (isSweetAlertAvailable()) {
            Swal.fire({
                toast: true,
                position: settings.position || defaultOptions.successToastPosition,
                icon: settings.icon || 'success',
                title: settings.title || settings.text || '',
                timer: settings.timer || defaultOptions.successToastTimer,
                timerProgressBar: settings.timerProgressBar !== false,
                showConfirmButton: false,
                showCloseButton: settings.showCloseButton === true
            });
            return;
        }

        console.info(settings.title || settings.text || '');
    }

    function showModal(options) {
        if (isSweetAlertAvailable()) {
            Swal.fire(options);
            return;
        }

        window.alert(options.text || stripHtml(options.html) || options.title || '');
    }

    function isSweetAlertAvailable() {
        return typeof Swal !== 'undefined' && typeof Swal.fire === 'function';
    }

    function buildValidationErrorHtml(response) {
        const errors = response?.responseJSON?.errors || response?.errors;
        if (!errors) return null;

        const messages = [];

        Object.keys(errors).forEach(function (key) {
            const value = errors[key];

            if (Array.isArray(value)) {
                value.forEach(function (message) {
                    const normalizedMessage = normalizeMessage(message);
                    if (normalizedMessage) messages.push(normalizedMessage);
                });
                return;
            }

            const normalizedValue = normalizeMessage(value);
            if (normalizedValue) messages.push(normalizedValue);
        });

        if (!messages.length) return null;

        const items = messages.map(function (message) {
            return '<li>' + escapeHtml(message) + '</li>';
        }).join('');

        return '<div class="text-start"><ul class="mb-0 ps-20">' + items + '</ul></div>';
    }

    function getCurrentCulture() {
        return location.pathname.split('/').filter(Boolean)[0] || 'tr-TR';
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

    function stripHtml(value) {
        return value ? String(value).replace(/<[^>]*>/g, '') : '';
    }

    return {
        getAntiForgeryToken: getAntiForgeryToken,
        buildTokenHeader: buildTokenHeader,
        buildAjaxHeaders: buildAjaxHeaders,
        postForm: postForm,
        postJson: postJson,
        handleJsonResult: handleJsonResult,
        reloadDataTable: reloadDataTable,
        showLoading: showLoading,
        closeLoading: closeLoading,
        showError: showError,
        showWarning: showWarning,
        showInfo: showInfo,
        showSuccess: showSuccess,
        confirm: confirm,
        extractMessage: extractMessage,
        normalizeMessage: normalizeMessage,
        getText: getText,
        escapeHtml: escapeHtml
    };
})(jQuery);
