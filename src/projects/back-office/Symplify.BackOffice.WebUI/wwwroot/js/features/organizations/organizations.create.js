window.Symplify = window.Symplify || {};
window.Symplify.Organizations = window.Symplify.Organizations || {};

(function () {
    'use strict';

    $(function () {
        if (window.Symplify.Organizations.Form && typeof window.Symplify.Organizations.Form.init === 'function') {
            window.Symplify.Organizations.Form.init();
        }
    });
})();
