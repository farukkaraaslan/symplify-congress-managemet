window.Symplify = window.Symplify || {};

window.Symplify.Ajax = (function ($) {
    'use strict';

    function getAntiForgeryToken($container) {
        const $token = ($container || $(document))
            .find('input[name="__RequestVerificationToken"]')
            .first();

        return $token.length ? $token.val() : null;
    }

    function buildTokenHeader($container) {
        const token = getAntiForgeryToken($container);

        if (!token) {
            return {};
        }

        return {
            RequestVerificationToken: token
        };
    }

    function postForm($form) {
        return $.ajax({
            url: $form.attr('action'),
            type: $form.attr('method') || 'POST',
            data: $form.serialize(),
            headers: buildTokenHeader($form)
        });
    }

    function showError(response) {
        const message = extractMessage(response) || getText('genericError', 'İşlem sırasında bir hata oluştu.');
        const validationHtml = buildValidationErrorHtml(response);

        showSwal({
            icon: 'error',
            title: getText('errorTitle', 'Hata'),
            html: validationHtml || escapeHtml(message),
            confirmButtonText: getText('ok', 'Tamam'),
            confirmButtonColor: '#487FFF'
        });
    }

    function showSuccess(message) {
        const resolvedMessage = normalizeMessage(message);

        showSwal({
            icon: 'success',
            title: getText('successTitle', 'Başarılı'),
            text: resolvedMessage || '',
            confirmButtonText: getText('ok', 'Tamam'),
            confirmButtonColor: '#487FFF'
        });
    }

    function confirm(options) {
        const settings = options || {};

        const title = resolveOptionText(
            settings.title,
            getText('deleteConfirmTitle', 'Emin misiniz?')
        );

        const text = resolveOptionText(
            settings.text,
            getText('deleteConfirmText', 'Bu işlem geri alınamayabilir.')
        );

        const confirmButtonText = resolveOptionText(
            settings.confirmButtonText,
            getText('deleteConfirmButton', 'Sil')
        );

        const cancelButtonText = resolveOptionText(
            settings.cancelButtonText,
            getText('cancel', 'Vazgeç')
        );

        if (isSweetAlertAvailable()) {
            return Swal.fire({
                icon: settings.icon || 'warning',
                title: title,
                text: text,
                showCancelButton: true,
                confirmButtonText: confirmButtonText,
                cancelButtonText: cancelButtonText,
                confirmButtonColor: settings.confirmButtonColor || '#EF4A00',
                cancelButtonColor: settings.cancelButtonColor || '#6B7280',
                reverseButtons: true
            });
        }

        const isConfirmed = window.confirm(text || title);
        return $.Deferred().resolve({ isConfirmed: isConfirmed }).promise();
    }

    function extractMessage(response) {
        if (!response) {
            return null;
        }

        if (typeof response === 'string') {
            return normalizeMessage(response);
        }

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

            if (value) {
                return value;
            }
        }

        return null;
    }

    function normalizeMessage(value) {
        if (value === null || value === undefined) {
            return null;
        }

        if (Array.isArray(value)) {
            const messages = value
                .map(function (item) {
                    return normalizeMessage(item);
                })
                .filter(function (item) {
                    return !!item;
                });

            return messages.length ? messages.join('\n') : null;
        }

        if (typeof value === 'object') {
            if (value.message) {
                return normalizeMessage(value.message);
            }

            if (value.title) {
                return normalizeMessage(value.title);
            }

            if (value.detail) {
                return normalizeMessage(value.detail);
            }

            if (value.error) {
                return normalizeMessage(value.error);
            }

            return null;
        }

        const text = String(value).trim();

        if (!text) {
            return null;
        }

        const knownMessage = resolveKnownMessageKey(text);

        if (knownMessage) {
            return knownMessage;
        }

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
            'Common.ReorderSuccess': getText('reorderSuccess', 'Sıralama güncellendi.')
        };

        if (Object.prototype.hasOwnProperty.call(fallbackByKey, key)) {
            return fallbackByKey[key];
        }

        return null;
    }

    function getText(key, fallback) {
        const sources = getTextSources();

        for (let i = 0; i < sources.length; i++) {
            const source = sources[i];

            if (!source || !Object.prototype.hasOwnProperty.call(source, key)) {
                continue;
            }

            const value = source[key];

            if (value === null || value === undefined) {
                continue;
            }

            const text = String(value).trim();

            if (!text) {
                continue;
            }

            if (isResourceKey(text)) {
                return getResourceText(text) || fallback;
            }

            return text;
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
        if (!key) {
            return null;
        }

        const resources = window.Symplify.resources || {};
        const value = resources[key];

        if (value !== null && value !== undefined) {
            const text = String(value).trim();

            if (text && text !== key) {
                return text;
            }
        }

        if (typeof window.Symplify.t === 'function') {
            const translated = window.Symplify.t(key, null);

            if (translated !== null && translated !== undefined) {
                const text = String(translated).trim();

                if (text && text !== key) {
                    return text;
                }
            }
        }

        return null;
    }

    function resolveOptionText(value, fallback) {
        const resolved = normalizeMessage(value);

        if (resolved) {
            return resolved;
        }

        return fallback || '';
    }

    function isResourceKey(value) {
        if (!value) {
            return false;
        }

        return /^[A-Za-z0-9]+(\.[A-Za-z0-9]+)+$/.test(String(value).trim());
    }

    function showSwal(options) {
        if (isSweetAlertAvailable()) {
            Swal.fire(options);
            return;
        }

        alert(options.text || stripHtml(options.html) || options.title || '');
    }

    function isSweetAlertAvailable() {
        return typeof Swal !== 'undefined' && typeof Swal.fire === 'function';
    }

    function buildValidationErrorHtml(response) {
        const errors = response?.responseJSON?.errors || response?.errors;

        if (!errors) {
            return null;
        }

        const messages = [];

        Object.keys(errors).forEach(function (key) {
            const value = errors[key];

            if (Array.isArray(value)) {
                value.forEach(function (message) {
                    const normalizedMessage = normalizeMessage(message);

                    if (normalizedMessage) {
                        messages.push(normalizedMessage);
                    }
                });

                return;
            }

            const normalizedValue = normalizeMessage(value);

            if (normalizedValue) {
                messages.push(normalizedValue);
            }
        });

        if (!messages.length) {
            return null;
        }

        const items = messages.map(function (message) {
            return '<li>' + escapeHtml(message) + '</li>';
        }).join('');

        return '<div class="text-start"><ul class="mb-0 ps-20">' + items + '</ul></div>';
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return '';
        }

        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function stripHtml(value) {
        if (!value) {
            return '';
        }

        return String(value).replace(/<[^>]*>/g, '');
    }

    return {
        getAntiForgeryToken: getAntiForgeryToken,
        buildTokenHeader: buildTokenHeader,
        postForm: postForm,
        showError: showError,
        showSuccess: showSuccess,
        confirm: confirm,
        extractMessage: extractMessage,
        normalizeMessage: normalizeMessage
    };
})(jQuery);