(function () {
    'use strict';

    const tr = 'tr-TR';
    const en = 'en-US';

    const institutionReplacements = new Map(Object.entries({
        'jsga': 'JSGA',
        'meb': 'MEB',
        'sbü': 'SBÜ',
        'sbu': 'SBÜ',
        's.b.ü': 'SBÜ',
        'tc': 'T.C.',
        't.c': 'T.C.',
        'suam': 'SUAM',
        'eah': 'EAH',
        'myo': 'MYO',
        'ktü': 'KTÜ',
        'omu': 'OMÜ',
        'kto': 'KTO',
        'makü': 'MAKÜ',
        'ybu': 'YBU',
        'adpu': 'ADPU',
        'uaem': 'UAEM',
        'mcbu': 'MCBU',
        'rudn': 'RUDN',
        'toaurılc': 'TOAURILC',
        'toaurilc': 'TOAURILC',
        'ad': 'AD',
        'abd': 'ABD',
        'a.d': 'A.D.',
        'a.b.d': 'A.B.D.',
        's.y.k': 'S.Y.K.',
        'r&d': 'R&D',
        'ar-ge': 'AR-GE'
    }));

    function normalizeSpaces(value, trim) {
        const normalized = String(value || '').replace(/\s+/g, ' ');
        return trim === false ? normalized : normalized.trim();
    }

    function replaceTurkishCharsForEnglish(value) {
        return String(value || '')
            .replaceAll('ç', 'c')
            .replaceAll('Ç', 'C')
            .replaceAll('ğ', 'g')
            .replaceAll('Ğ', 'G')
            .replaceAll('ı', 'i')
            .replaceAll('İ', 'I')
            .replaceAll('ö', 'o')
            .replaceAll('Ö', 'O')
            .replaceAll('ş', 's')
            .replaceAll('Ş', 'S')
            .replaceAll('ü', 'u')
            .replaceAll('Ü', 'U');
    }

    function upperTr(value, trim) {
        return normalizeSpaces(value, trim).toLocaleUpperCase(tr);
    }

    function upperEn(value, trim) {
        return normalizeSpaces(replaceTurkishCharsForEnglish(value), trim).toLocaleUpperCase(en);
    }

    function titleCase(value) {
        const lower = normalizeSpaces(value).toLocaleLowerCase(tr);
        let result = '';
        let shouldUpper = true;

        for (const ch of lower) {
            if (/\p{L}/u.test(ch) && shouldUpper) {
                result += ch.toLocaleUpperCase(tr);
                shouldUpper = false;
                continue;
            }

            result += ch;
            shouldUpper = [' ', '-', '\'', '.', '/', ',', ';', ':', '(', '[', '{'].includes(ch);
        }

        return result.trim();
    }

    function normalizeTitle(value, culture, trim) {
        let normalized = normalizeSpaces(value, trim)
            .replace(/\s+([,.;:!?])/g, '$1')
            .replace(/([,;:!?])([^\s])/g, '$1 $2');

        return culture === 'en' ? upperEn(normalized, trim) : upperTr(normalized, trim);
    }

    function normalizeEnglishText(value, trim) {
        return normalizeSpaces(replaceTurkishCharsForEnglish(value), trim)
            .replace(/\s+([,.;:!?])/g, '$1')
            .replace(/([,;:!?])([^\s])/g, '$1 $2');
    }

    function normalizeInstitution(value) {
        let normalized = normalizeSpaces(value)
            .replace(/\s+([,.;:])/g, '$1')
            .replace(/([,;:])([^\s])/g, '$1 $2')
            .replace(/\s*\/\s*/g, '/')
            .replace(/\s*-\s*/g, '-')
            .replace(/([\p{L}])\.([\p{L}])/gu, '$1. $2');

        normalized = titleCase(normalized)
            .replace(/\bVe\b/g, 've')
            .replace(/\bİle\b/g, 'ile')
            .replace(/\bAnd\b/gi, 'and')
            .replace(/\bOf\b/gi, 'of')
            .replace(/\bThe\b/gi, 'the')
            .replace(/\bFor\b/gi, 'for')
            .replace(/\bIn\b/gi, 'in');

        normalized = normalized.replace(/[\p{L}\p{N}&.]+/gu, function (token) {
            const key = token.toLocaleLowerCase(tr).replace(/\.$/, '');
            return institutionReplacements.get(key) || token;
        });

        return normalized
            .replace(/S\.\s*B\.\s*Ü\.?/gi, 'SBÜ')
            .replace(/A\.\s*B\.\s*D\.?/gi, 'A.B.D.')
            .replace(/A\.\s*D\.?/gi, 'A.D.')
            .replace(/S\.\s*Y\.\s*K\.?/gi, 'S.Y.K.')
            .replace(/Dr\.\s*Lütfi/gi, 'Dr. Lütfi')
            .replace(/Dr\.\s*Sadi/gi, 'Dr. Sadi')
            .trim();
    }

    function setValue(input, value) {
        if (!input || input.value === value) return;

        const start = input.selectionStart;
        const end = input.selectionEnd;
        input.value = value;

        if (document.activeElement === input && typeof start === 'number' && typeof end === 'number') {
            const delta = value.length - String(input.value || '').length;
            const position = Math.min(value.length, Math.max(0, start + delta));
            input.setSelectionRange(position, position);
        }
    }

    function normalizeInput(input, immediate) {
        if (!input) return;

        const trim = !immediate;

        if (input.hasAttribute('data-normalize-surname')) {
            setValue(input, upperTr(input.value, trim));
            return;
        }

        if (input.hasAttribute('data-normalize-title-tr')) {
            setValue(input, normalizeTitle(input.value, 'tr', trim));
            return;
        }

        if (input.hasAttribute('data-normalize-title-en')) {
            setValue(input, normalizeTitle(input.value, 'en', trim));
            return;
        }

        if (input.hasAttribute('data-normalize-english-text')) {
            setValue(input, normalizeEnglishText(input.value, trim));
            return;
        }

        if (!immediate && input.hasAttribute('data-normalize-person-name')) {
            setValue(input, titleCase(input.value));
            return;
        }

        if (!immediate && input.hasAttribute('data-normalize-institution')) {
            setValue(input, normalizeInstitution(input.value));
        }
    }

    document.addEventListener('input', function (event) {
        normalizeInput(event.target, true);
    }, true);

    document.addEventListener('blur', function (event) {
        normalizeInput(event.target, false);
    }, true);

    document.addEventListener('submit', function (event) {
        if (!event.target || typeof event.target.querySelectorAll !== 'function') return;

        event.target
            .querySelectorAll('[data-normalize-surname],[data-normalize-title-tr],[data-normalize-title-en],[data-normalize-english-text],[data-normalize-person-name],[data-normalize-institution]')
            .forEach(input => normalizeInput(input, false));
    }, true);

    window.Symplify = window.Symplify || {};
    window.Symplify.TextNormalizer = {
        upperTr,
        upperEn,
        titleCase,
        normalizeInstitution,
        normalizeTitleTr: value => normalizeTitle(value, 'tr'),
        normalizeTitleEn: value => normalizeTitle(value, 'en'),
        normalizeEnglishText,
        replaceTurkishCharsForEnglish
    };
}());
