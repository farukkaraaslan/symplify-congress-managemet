window.Symplify = window.Symplify || {};
window.Symplify.TransactionStatusTransitions = window.Symplify.TransactionStatusTransitions || {};

$(function () {
    'use strict';

    if (window.Symplify.TransactionStatusTransitions.Table) {
        window.Symplify.TransactionStatusTransitions.Table.init();
    }

    if (window.Symplify.TransactionStatusTransitions.Create) {
        window.Symplify.TransactionStatusTransitions.Create.init();
    }

    if (window.Symplify.TransactionStatusTransitions.Update) {
        window.Symplify.TransactionStatusTransitions.Update.init();
    }

    if (window.Symplify.TransactionStatusTransitions.Delete) {
        window.Symplify.TransactionStatusTransitions.Delete.init();
    }
});
