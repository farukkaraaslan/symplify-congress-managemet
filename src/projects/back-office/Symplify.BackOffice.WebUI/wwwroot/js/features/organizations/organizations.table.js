window.Symplify = window.Symplify || {};
window.Symplify.Organizations = window.Symplify.Organizations || {};

(function () {
    'use strict';

    $(function () {
        if (window.Symplify.Organizations.Index && typeof window.Symplify.Organizations.Index.init === 'function') {
            window.Symplify.Organizations.Index.init();
        }
    });
})();
