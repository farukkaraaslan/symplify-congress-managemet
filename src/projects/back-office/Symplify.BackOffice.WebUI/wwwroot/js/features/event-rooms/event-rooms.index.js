window.Symplify = window.Symplify || {};
window.Symplify.EventRooms = window.Symplify.EventRooms || {};

$(function () {
    'use strict';

    if (window.Symplify.EventRooms.Table) {
        window.Symplify.EventRooms.Table.init();
    }

    if (window.Symplify.EventRooms.Create) {
        window.Symplify.EventRooms.Create.init();
    }

    if (window.Symplify.EventRooms.Update) {
        window.Symplify.EventRooms.Update.init();
    }

    if (window.Symplify.EventRooms.Delete) {
        window.Symplify.EventRooms.Delete.init();
    }
});
