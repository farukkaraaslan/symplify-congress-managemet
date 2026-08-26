window.Symplify = window.Symplify || {};
window.Symplify.EvaluationCriteria = window.Symplify.EvaluationCriteria || {};

$(function () {
    'use strict';

    if (window.Symplify.EvaluationCriteria.Table) {
        window.Symplify.EvaluationCriteria.Table.init();
    }

    if (window.Symplify.EvaluationCriteria.Create) {
        window.Symplify.EvaluationCriteria.Create.init();
    }

    if (window.Symplify.EvaluationCriteria.Update) {
        window.Symplify.EvaluationCriteria.Update.init();
    }

    if (window.Symplify.EvaluationCriteria.Delete) {
        window.Symplify.EvaluationCriteria.Delete.init();
    }
});
