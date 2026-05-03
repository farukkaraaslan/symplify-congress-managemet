window.Symplify = window.Symplify || {};
window.Symplify.TransactionStatusPhases = window.Symplify.TransactionStatusPhases || {};

$(function () {
    'use strict';

    if (window.Symplify.TransactionStatusPhases.Table) window.Symplify.TransactionStatusPhases.Table.init();
    if (window.Symplify.TransactionStatusPhases.Create) window.Symplify.TransactionStatusPhases.Create.init();
    if (window.Symplify.TransactionStatusPhases.Update) window.Symplify.TransactionStatusPhases.Update.init();
    if (window.Symplify.TransactionStatusPhases.Delete) window.Symplify.TransactionStatusPhases.Delete.init();
});
