(function ($) {
    'use strict';

    function initializePasswordToggle(toggleSelector) {
        $(document).on('click', toggleSelector, function () {
            var $toggle = $(this);
            var $input = $($toggle.attr('data-toggle'));

            if (!$input.length) {
                return;
            }

            $toggle.toggleClass('ri-eye-off-line');
            $input.attr('type', $input.attr('type') === 'password' ? 'text' : 'password');
        });
    }

    function getLocalizedText(key, fallback) {
        return window.Symplify && typeof window.Symplify.t === 'function'
            ? window.Symplify.t(key, fallback)
            : fallback;
    }

    function onlyDigits(value) {
        return (value || '').replace(/\D/g, '');
    }

    function trimLeadingZeros(value) {
        return (value || '').replace(/^0+/, '');
    }

    function buildE164Phone(input, instance) {
        var raw = input && input.value ? input.value.trim() : '';
        if (!raw) {
            return '';
        }

        var selectedCountry = instance && typeof instance.getSelectedCountryData === 'function'
            ? instance.getSelectedCountryData()
            : null;

        var dialCode = selectedCountry && selectedCountry.dialCode
            ? onlyDigits(selectedCountry.dialCode)
            : '';

        var rawDigits = onlyDigits(raw);
        if (!rawDigits) {
            return '';
        }

        if (raw.charAt(0) === '+') {
            return '+' + rawDigits;
        }

        if (dialCode && rawDigits.indexOf(dialCode) === 0) {
            return '+' + rawDigits;
        }

        if (dialCode) {
            return '+' + dialCode + trimLeadingZeros(rawDigits);
        }

        return rawDigits;
    }

    function setPhoneValidationMessage($form, input, message) {
        var $message = $form.find('[data-auth-phone-validation]');
        $message.text(message || '');
        $(input).toggleClass('input-validation-error is-invalid', !!message);
    }

    function toNationalDisplayValue(value, instance) {
        var rawDigits = onlyDigits(value);
        if (!rawDigits) {
            return '';
        }

        var selectedCountry = instance && typeof instance.getSelectedCountryData === 'function'
            ? instance.getSelectedCountryData()
            : null;

        var dialCode = selectedCountry && selectedCountry.dialCode
            ? onlyDigits(selectedCountry.dialCode)
            : '';

        if (dialCode && rawDigits.indexOf(dialCode) === 0) {
            rawDigits = rawDigits.substring(dialCode.length);
        }

        return rawDigits;
    }

    function syncPhoneHiddenFields($form, input, instance) {
        var selectedCountry = instance && typeof instance.getSelectedCountryData === 'function'
            ? instance.getSelectedCountryData()
            : null;

        if (selectedCountry) {
            $form.find('[data-auth-phone-country]').val(selectedCountry.iso2 || '');
            $form.find('[data-auth-phone-dial-code]').val(selectedCountry.dialCode || '');
        }

        var normalizedPhone = buildE164Phone(input, instance);
        $form.find('[data-auth-phone-e164]').val(normalizedPhone);

        return normalizedPhone;
    }

    function normalizeVisiblePhoneInput(input, instance) {
        if (!input || !input.value || input.value.trim().charAt(0) !== '+') {
            return;
        }

        var nationalValue = toNationalDisplayValue(input.value, instance);
        if (nationalValue) {
            input.value = nationalValue;
        }
    }

    function initializeRegisterPhoneInput($form) {
        var input = $form.find('[data-auth-phone-input]')[0];

        if (!input || typeof window.intlTelInput !== 'function') {
            return;
        }

        var language = (document.documentElement.lang || '').toLowerCase();
        var instance = window.intlTelInput(input, {
            initialCountry: language.indexOf('tr') === 0 ? 'tr' : 'auto',
            separateDialCode: true,
            nationalMode: true,
            autoPlaceholder: 'aggressive',
            loadUtils: function () {
                return import('https://cdn.jsdelivr.net/npm/intl-tel-input@25.3.1/build/js/utils.js');
            }
        });

        $(input).closest('.iti').addClass('w-100');

        input.addEventListener('countrychange', function () {
            normalizeVisiblePhoneInput(input, instance);
            syncPhoneHiddenFields($form, input, instance);
            setPhoneValidationMessage($form, input, '');
        });

        input.addEventListener('input', function () {
            normalizeVisiblePhoneInput(input, instance);
            syncPhoneHiddenFields($form, input, instance);
            setPhoneValidationMessage($form, input, '');
        });

        input.addEventListener('blur', function () {
            normalizeVisiblePhoneInput(input, instance);
            syncPhoneHiddenFields($form, input, instance);
        });

        $form[0].addEventListener('submit', function (event) {
            normalizeVisiblePhoneInput(input, instance);
            var normalizedPhone = syncPhoneHiddenFields($form, input, instance);
            var requiredMessage = input.getAttribute('data-auth-phone-required-message') || getLocalizedText('BackOffice.Auth.Validation.PhoneRequired', 'Telefon numarası zorunludur.');
            var invalidMessage = input.getAttribute('data-auth-phone-invalid-message') || getLocalizedText('BackOffice.Auth.Validation.PhoneInvalid', 'Telefon numarasını ülke kodu ile birlikte geçerli formatta giriniz.');

            if (!normalizedPhone) {
                event.preventDefault();
                event.stopImmediatePropagation();
                setPhoneValidationMessage($form, input, requiredMessage);
                input.focus();
                return false;
            }

            if (instance && typeof instance.isValidNumber === 'function' && !instance.isValidNumber()) {
                event.preventDefault();
                event.stopImmediatePropagation();
                setPhoneValidationMessage($form, input, invalidMessage);
                input.focus();
                return false;
            }

            setPhoneValidationMessage($form, input, '');
            return true;
        }, true);

        normalizeVisiblePhoneInput(input, instance);
        syncPhoneHiddenFields($form, input, instance);
    }

    function initializeRegisterStateSelector() {
        var $form = $('[data-auth-register-form]');
        if (!$form.length) {
            return;
        }

        var $country = $form.find('[data-auth-country-select]');
        var $state = $form.find('[data-auth-state-select]');
        var statesUrl = $form.data('states-url');
        var selectStateText = getLocalizedText('BackOffice.Auth.Field.SelectState', 'Eyalet/İl');

        initializeRegisterPhoneInput($form);


        function resetStates() {
            $state.empty();
            $state.append($('<option />').attr('value', '').text(selectStateText));
            $state.prop('disabled', true);
        }

        function fillStates(items) {
            resetStates();

            if (!Array.isArray(items) || items.length === 0) {
                return;
            }

            items.forEach(function (item) {
                $state.append($('<option />').attr('value', item.value || item.Value).text(item.text || item.Text));
            });

            $state.prop('disabled', false);
        }

        if (!$state.find('option[value!=""]').length) {
            $state.prop('disabled', true);
        }

        $country.on('change', function () {
            var countryId = $country.val();

            resetStates();

            if (!countryId || !statesUrl) {
                return;
            }

            $.getJSON(statesUrl, { countryId: countryId })
                .done(fillStates)
                .fail(resetStates);
        });
    }

    $(function () {
        initializePasswordToggle('.toggle-password');
        initializeRegisterStateSelector();
    });
})(jQuery);
