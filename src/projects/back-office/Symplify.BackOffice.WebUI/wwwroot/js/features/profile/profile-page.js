(function () {
    'use strict';

    const form = document.querySelector('[data-profile-form]');
    if (!form) {
        return;
    }

    const input = form.querySelector('[data-profile-phone-input]');
    const hidden = form.querySelector('[data-profile-phone-hidden]');
    const validation = form.querySelector('[data-profile-phone-validation]');

    if (!input || !hidden || typeof window.intlTelInput !== 'function') {
        return;
    }

    const initialHiddenValue = hidden.value || '';
    const language = (document.documentElement.lang || '').toLowerCase();
    const iti = window.intlTelInput(input, {
        initialCountry: language.indexOf('tr') === 0 ? 'tr' : 'auto',
        separateDialCode: true,
        nationalMode: true,
        autoPlaceholder: 'aggressive',
        formatAsYouType: true,
        loadUtils: function () {
            return import('https://cdn.jsdelivr.net/npm/intl-tel-input@25.3.1/build/js/utils.js');
        }
    });

    input.closest('.iti')?.classList.add('w-100');

    function stripDialCodeFromVisibleInput() {
        const country = iti.getSelectedCountryData ? iti.getSelectedCountryData() : null;
        const dialCode = country && country.dialCode ? country.dialCode : '';
        let value = input.value || '';

        value = value.trim();
        if (dialCode && value.startsWith('+' + dialCode)) {
            value = value.substring(('+' + dialCode).length).trim();
        }

        input.value = value;
    }

    function buildFallbackE164() {
        const country = iti.getSelectedCountryData ? iti.getSelectedCountryData() : null;
        const dialCode = country && country.dialCode ? country.dialCode : '';
        const national = (input.value || '').replace(/\D/g, '');

        if (!dialCode || !national) {
            return '';
        }

        return '+' + dialCode + national;
    }

    function setValidationMessage(message) {
        if (validation) {
            validation.textContent = message || '';
        }

        input.classList.toggle('is-invalid', Boolean(message));
    }

    function syncHiddenPhone() {
        stripDialCodeFromVisibleInput();

        let value = '';
        if (typeof iti.getNumber === 'function') {
            value = iti.getNumber();
        }

        if (!value) {
            value = buildFallbackE164();
        }

        hidden.value = value || '';
        return hidden.value;
    }

    function initializeCurrentValue() {
        if (initialHiddenValue && initialHiddenValue.charAt(0) === '+' && typeof iti.setNumber === 'function') {
            iti.setNumber(initialHiddenValue);
        }

        stripDialCodeFromVisibleInput();
        syncHiddenPhone();
    }

    input.addEventListener('blur', function () {
        syncHiddenPhone();
    });

    input.addEventListener('countrychange', function () {
        syncHiddenPhone();
        setValidationMessage('');
    });

    input.addEventListener('input', function () {
        stripDialCodeFromVisibleInput();
        hidden.value = '';
        setValidationMessage('');
    });

    form.addEventListener('submit', function (event) {
        const value = syncHiddenPhone();
        const requiredMessage = input.getAttribute('data-profile-phone-required-message') || 'Telefon numarası zorunludur.';
        const invalidMessage = input.getAttribute('data-profile-phone-invalid-message') || 'Telefon numarasını geçerli formatta giriniz.';

        if (!value) {
            event.preventDefault();
            event.stopImmediatePropagation();
            setValidationMessage(requiredMessage);
            input.focus();
            return false;
        }

        const isValidByPlugin = typeof iti.isValidNumber === 'function' ? iti.isValidNumber() : true;
        const isValidByPattern = /^\+[1-9]\d{7,14}$/.test(value);

        if (!isValidByPlugin || !isValidByPattern) {
            event.preventDefault();
            event.stopImmediatePropagation();
            setValidationMessage(invalidMessage);
            input.focus();
            return false;
        }

        setValidationMessage('');
        return true;
    }, true);

    initializeCurrentValue();
}());

(function () {
    'use strict';

    document.querySelectorAll('[data-profile-photo-input]').forEach(function (input) {
        var targetSelector = input.getAttribute('data-profile-photo-input');
        var target = targetSelector ? document.querySelector(targetSelector) : null;

        if (!target) {
            return;
        }

        input.addEventListener('change', function () {
            var file = input.files && input.files.length ? input.files[0] : null;
            if (!file) {
                return;
            }

            if (!file.type || file.type.indexOf('image/') !== 0) {
                input.value = '';
                return;
            }

            var reader = new FileReader();
            reader.onload = function (event) {
                var image = target.querySelector('img');
                if (!image) {
                    image = document.createElement('img');
                    image.alt = target.getAttribute('data-avatar-text') || '';
                    image.className = 'w-100 h-100 object-fit-cover d-block';
                    target.insertBefore(image, target.firstChild);
                }

                image.src = event.target && event.target.result ? event.target.result : '';
                target.classList.add('profile-photo-preview--has-photo');
                target.querySelector('span')?.classList.add('d-none');
            };

            reader.readAsDataURL(file);
        });
    });
}());
