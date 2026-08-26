(function () {
    'use strict';

    function syncBootstrapTheme() {
        var theme = document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-bs-theme', theme);
    }

    syncBootstrapTheme();

    if (window.MutationObserver) {
        new MutationObserver(syncBootstrapTheme).observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-theme']
        });
    }

    document.querySelectorAll('[data-file-input]').forEach(function (input) {
        input.addEventListener('change', function () {
            var targetSelector = input.getAttribute('data-file-input');
            var target = targetSelector ? document.querySelector(targetSelector) : null;
            if (!target) return;

            target.textContent = input.files && input.files.length
                ? input.files[0].name
                : 'Henüz dosya seçilmedi.';
        });
    });

})();
