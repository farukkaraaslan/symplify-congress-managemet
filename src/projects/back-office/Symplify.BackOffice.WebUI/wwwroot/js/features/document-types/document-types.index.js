window.Symplify = window.Symplify || {};
window.Symplify.DocumentTypes = window.Symplify.DocumentTypes || {};

$(function () {
    'use strict';

    if (window.Symplify.DocumentTypes.Table) {
        window.Symplify.DocumentTypes.Table.init();
    }

    if (window.Symplify.DocumentTypes.Create) {
        window.Symplify.DocumentTypes.Create.init();
    }

    if (window.Symplify.DocumentTypes.Update) {
        window.Symplify.DocumentTypes.Update.init();
    }

    if (window.Symplify.DocumentTypes.Delete) {
        window.Symplify.DocumentTypes.Delete.init();
    }
});
