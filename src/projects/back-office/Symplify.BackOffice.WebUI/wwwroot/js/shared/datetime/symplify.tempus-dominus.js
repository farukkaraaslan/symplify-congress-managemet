window.Symplify = window.Symplify || {};

window.Symplify.TempusDominus = (function ($) {
    'use strict';

    const defaults = {
        selector: '[data-symplify-datetime]',
        format: 'dd.MM.yyyy HH:mm',
        locale: 'tr-TR',
        useCurrent: false,
        stepping: 5,
        displayComponents: {
            calendar: true,
            date: true,
            month: true,
            year: true,
            decades: true,
            clock: true,
            hours: true,
            minutes: true,
            seconds: false
        }
    };

    function initAll(container, options) {
        const $container = normalizeContainer(container);
        const settings = $.extend(true, {}, defaults, options || {});
        const $inputs = $container.find(settings.selector).addBack(settings.selector);

        $inputs.each(function () {
            init(this, settings);
        });
    }

    function init(element, options) {
        const input = element instanceof HTMLElement ? element : $(element).get(0);

        if (!input) {
            return null;
        }

        const $input = $(input);
        const settings = buildSettings($input, options);

        destroy(input);

        if (window.tempusDominus && typeof window.tempusDominus.TempusDominus === 'function') {
            return initTempusDominus(input, settings);
        }

        return initNativeFallback(input, settings);
    }

    function destroy(container) {
        const $container = normalizeContainer(container);
        const $inputs = $container.find(defaults.selector).addBack(defaults.selector);

        $inputs.each(function () {
            const $input = $(this);
            const instance = $input.data('symplify-tempus-dominus-instance');
            const $native = $input.data('symplify-tempus-dominus-native');

            if (instance && typeof instance.dispose === 'function') {
                instance.dispose();
            }

            if ($native && $native.jquery) {
                $native.remove();
            }

            $input.off('.symplifyTempusDominus');
            $input.removeData('symplify-tempus-dominus-instance');
            $input.removeData('symplify-tempus-dominus-native');
        });
    }

    function syncAll(container) {
        const $container = normalizeContainer(container);
        $container.find(defaults.selector).addBack(defaults.selector).trigger('change');
    }

    function initTempusDominus(input, settings) {
        const options = {
            localization: {
                locale: settings.locale,
                format: toTempusFormat(settings.format)
            },
            useCurrent: settings.useCurrent,
            stepping: settings.stepping,
            display: {
                components: settings.displayComponents
            }
        };

        const instance = new window.tempusDominus.TempusDominus(input, options);
        $(input).data('symplify-tempus-dominus-instance', instance);

        input.addEventListener('change.td', function () {
            triggerValidation(input);
        });

        return instance;
    }

    function initNativeFallback(input, settings) {
        const $input = $(input);
        const initialValue = $input.val();
        const hiddenName = $input.attr('name');

        if (!hiddenName) {
            return null;
        }

        let $native = $input.siblings('input[data-symplify-datetime-native-for="' + escapeAttribute(hiddenName) + '"]').first();

        if (!$native.length) {
            $native = $('<input type="datetime-local" class="form-control radius-8 mt-2" />');
            $native.attr('data-symplify-datetime-native-for', hiddenName);
            $native.insertAfter($input);
        }

        $input.attr('type', 'hidden');
        $native.val(toNativeValue(initialValue));

        $native.off('.symplifyTempusDominus').on('change.symplifyTempusDominus input.symplifyTempusDominus', function () {
            $input.val(fromNativeValue($native.val()));
            $input.trigger('change');
            triggerValidation(input);
        });

        $input.data('symplify-tempus-dominus-native', $native);

        return $native[0];
    }

    function buildSettings($input, options) {
        const settings = $.extend(true, {}, defaults, options || {});
        const data = $input.data() || {};

        settings.format = data.datetimeFormat || settings.format;
        settings.locale = data.datetimeLocale || document.documentElement.getAttribute('lang') || settings.locale;
        settings.useCurrent = data.datetimeUseCurrent !== undefined ? parseBoolean(data.datetimeUseCurrent) : settings.useCurrent;
        settings.stepping = data.datetimeStepping !== undefined ? parsePositiveInt(data.datetimeStepping) || settings.stepping : settings.stepping;

        return settings;
    }

    function toTempusFormat(format) {
        return String(format || defaults.format)
            .replace(/yyyy/g, 'yyyy')
            .replace(/dd/g, 'dd')
            .replace(/MM/g, 'MM')
            .replace(/HH/g, 'HH')
            .replace(/mm/g, 'mm');
    }

    function toNativeValue(value) {
        const parsed = parseDisplayValue(value);

        if (!parsed) {
            return '';
        }

        return pad(parsed.year, 4) + '-' + pad(parsed.month, 2) + '-' + pad(parsed.day, 2) + 'T' + pad(parsed.hour, 2) + ':' + pad(parsed.minute, 2);
    }

    function fromNativeValue(value) {
        if (!value) {
            return '';
        }

        const parts = String(value).split('T');
        const dateParts = (parts[0] || '').split('-');
        const timeParts = (parts[1] || '00:00').split(':');

        if (dateParts.length < 3) {
            return '';
        }

        return pad(dateParts[2], 2) + '.' + pad(dateParts[1], 2) + '.' + pad(dateParts[0], 4) + ' ' + pad(timeParts[0] || 0, 2) + ':' + pad(timeParts[1] || 0, 2);
    }

    function parseDisplayValue(value) {
        if (!value) {
            return null;
        }

        const text = String(value).trim();
        let match = text.match(/^(\d{1,2})\.(\d{1,2})\.(\d{4})(?:\s+(\d{1,2}):(\d{1,2}))?$/);

        if (match) {
            return {
                day: Number(match[1]),
                month: Number(match[2]),
                year: Number(match[3]),
                hour: Number(match[4] || 0),
                minute: Number(match[5] || 0)
            };
        }

        match = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})(?:[T\s](\d{1,2}):(\d{1,2}))?/);

        if (match) {
            return {
                year: Number(match[1]),
                month: Number(match[2]),
                day: Number(match[3]),
                hour: Number(match[4] || 0),
                minute: Number(match[5] || 0)
            };
        }

        return null;
    }

    function triggerValidation(input) {
        const $input = $(input);
        const $form = $input.closest('form');

        if ($form.length && $form.data('validator')) {
            $input.valid();
        }
    }

    function normalizeContainer(container) {
        if (!container) {
            return $(document);
        }

        return container.jquery ? container : $(container);
    }

    function parsePositiveInt(value) {
        const parsed = parseInt(value, 10);
        return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
    }

    function parseBoolean(value) {
        if (typeof value === 'boolean') {
            return value;
        }

        return String(value).toLowerCase() === 'true';
    }

    function pad(value, length) {
        return String(value).padStart(length, '0');
    }

    function escapeAttribute(value) {
        return String(value || '').replace(/"/g, '\\"');
    }

    return {
        init: init,
        initAll: initAll,
        destroy: destroy,
        syncAll: syncAll
    };
})(jQuery);

$(function () {
    if (window.Symplify.TempusDominus) {
        window.Symplify.TempusDominus.initAll(document);
    }
});
