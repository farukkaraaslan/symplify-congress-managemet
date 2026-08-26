window.Symplify = window.Symplify || {};
window.Symplify.Titles = window.Symplify.Titles || {};

$(function () {
    'use strict';

    if (window.Symplify.Titles.Table) {
        window.Symplify.Titles.Table.init();
    }

    if (window.Symplify.Titles.Create) {
        window.Symplify.Titles.Create.init();
    }

    if (window.Symplify.Titles.Update) {
        window.Symplify.Titles.Update.init();
    }

    if (window.Symplify.Titles.Delete) {
        window.Symplify.Titles.Delete.init();
    }
});
