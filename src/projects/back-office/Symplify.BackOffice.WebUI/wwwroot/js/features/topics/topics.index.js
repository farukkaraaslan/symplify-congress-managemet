window.Symplify = window.Symplify || {};
window.Symplify.Topics = window.Symplify.Topics || {};

$(function () {
    'use strict';

    if (window.Symplify.Topics.Table) {
        window.Symplify.Topics.Table.init();
    }

    if (window.Symplify.Topics.Create) {
        window.Symplify.Topics.Create.init();
    }

    if (window.Symplify.Topics.Update) {
        window.Symplify.Topics.Update.init();
    }

    if (window.Symplify.Topics.Delete) {
        window.Symplify.Topics.Delete.init();
    }
});
