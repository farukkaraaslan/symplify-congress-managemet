window.Symplify = window.Symplify || {};
window.Symplify.TransactionStatuses = window.Symplify.TransactionStatuses || {};

$(function () {
    'use strict';

    if (window.Symplify.TransactionStatuses.Table) window.Symplify.TransactionStatuses.Table.init();
    if (window.Symplify.TransactionStatuses.Create) window.Symplify.TransactionStatuses.Create.init();
    if (window.Symplify.TransactionStatuses.Update) window.Symplify.TransactionStatuses.Update.init();
    if (window.Symplify.TransactionStatuses.Delete) window.Symplify.TransactionStatuses.Delete.init();
});
