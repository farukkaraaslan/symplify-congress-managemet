(function () {
    'use strict';

    const input = document.querySelector('#PhoneNumberDisplay');
    const hidden = document.querySelector('#PhoneNumber');
    const form = document.querySelector('#complete-phone-form');

    if (!input || !hidden || !form || typeof window.intlTelInput !== 'function') {
        return;
    }

    const iti = window.intlTelInput(input, {
        initialCountry: 'tr',
        separateDialCode: true,
        nationalMode: true,
        autoPlaceholder: 'aggressive',
        formatAsYouType: true
    });

    function stripDialCodeFromVisibleInput() {
        const country = iti.getSelectedCountryData();
        const dialCode = country && country.dialCode ? country.dialCode : '';
        let value = input.value || '';

        value = value.trim();
        if (dialCode && value.startsWith('+' + dialCode)) {
            value = value.substring(('+' + dialCode).length).trim();
        }

        input.value = value;
    }

    function buildFallbackE164() {
        const country = iti.getSelectedCountryData();
        const dialCode = country && country.dialCode ? country.dialCode : '';
        const national = (input.value || '').replace(/\D/g, '');

        if (!dialCode || !national) {
            return '';
        }

        return '+' + dialCode + national;
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

    input.addEventListener('blur', syncHiddenPhone);
    input.addEventListener('countrychange', syncHiddenPhone);
    input.addEventListener('input', function () {
        stripDialCodeFromVisibleInput();
        hidden.value = '';
    });

    form.addEventListener('submit', function (event) {
        const value = syncHiddenPhone();

        if (!value || !/^\+[1-9]\d{7,14}$/.test(value)) {
            event.preventDefault();
            event.stopPropagation();
            input.classList.add('is-invalid');
            input.focus();
            return false;
        }

        input.classList.remove('is-invalid');
        return true;
    });
}());
