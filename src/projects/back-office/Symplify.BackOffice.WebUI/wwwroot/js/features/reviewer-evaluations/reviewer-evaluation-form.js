(function () {
    'use strict';

    function t(key, fallback) {
        if (window.Symplify && typeof window.Symplify.t === 'function') {
            return window.Symplify.t(key, fallback || key);
        }

        return fallback || key;
    }

    function getScoreSelects() {
        return Array.prototype.slice.call(document.querySelectorAll('.js-score-select'));
    }

    function getRecommendationSelect() {
        return document.querySelector('.js-recommendation-select');
    }

    function setText(id, value) {
        var element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }

    function updateScoreState() {
        var total = 0;
        var completed = 0;
        var selects = getScoreSelects();

        selects.forEach(function (select) {
            if (select.value !== '') {
                completed += 1;
            }

            var value = parseFloat(select.value || '0');
            if (!Number.isNaN(value)) {
                total += value;
            }
        });

        ['scoreTotal', 'scoreTotalInline', 'scoreTotalSidebar'].forEach(function (id) {
            setText(id, total.toString());
        });

        setText('scoredCriteriaCount', completed.toString());
        setText('criteriaTotal', selects.length.toString());

        updateSubmitAvailability();
    }

    function updateSubmitAvailability() {
        var button = document.getElementById('submitEvaluationButton');
        if (!button) {
            return;
        }

        var selects = getScoreSelects();
        var recommendation = getRecommendationSelect();
        var allCriteriaScored = selects.every(function (select) { return select.value !== ''; });
        var hasRecommendation = recommendation ? recommendation.value !== '' : true;
        var isValid = allCriteriaScored && hasRecommendation;

        button.disabled = !isValid;
    }

    document.addEventListener('change', function (event) {
        if (event.target && event.target.classList.contains('js-score-select')) {
            updateScoreState();
        }

        if (event.target && event.target.classList.contains('js-recommendation-select')) {
            updateSubmitAvailability();
        }
    });

    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-submit-mode]');
        if (!button) {
            return;
        }

        var hidden = document.getElementById('SubmitEvaluation');
        if (!hidden) {
            return;
        }

        hidden.value = 'true';
        event.preventDefault();

        if (button.disabled) {
            return;
        }

        var form = button.closest('form');
        if (!form) {
            return;
        }

        if (window.Swal) {
            Swal.fire({
                title: t('BackOffice.ReviewerEvaluations.Confirm.Submit.Title'),
                text: t('BackOffice.ReviewerEvaluations.Confirm.Submit.Text'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: t('BackOffice.ReviewerEvaluations.Confirm.Submit.Button'),
                cancelButtonText: t('Common.Cancel')
            }).then(function (result) {
                if (result.isConfirmed) {
                    hidden.value = 'true';
                    button.disabled = true;
                    form.submit();
                }
            });
            return;
        }

        if (window.confirm(t('BackOffice.ReviewerEvaluations.Confirm.Submit.Fallback'))) {
            hidden.value = 'true';
            button.disabled = true;
            form.submit();
        }
    });

    updateScoreState();
})();
