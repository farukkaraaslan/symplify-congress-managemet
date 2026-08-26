(function () {
    'use strict';

    showPendingToasts();
    wireConfirmForms();

    function showPendingToasts() {
        document.querySelectorAll('[data-symplify-toast]').forEach(element => {
            const icon = element.dataset.toastIcon || 'success';
            const title = element.dataset.toastTitle || '';
            const text = element.dataset.toastText || '';

            if (!title && !text) return;

            if (window.Swal && typeof window.Swal.fire === 'function') {
                window.Swal.fire({
                    icon: icon,
                    title: title,
                    text: text,
                    confirmButtonText: 'Tamam',
                    allowOutsideClick: true,
                    allowEscapeKey: true
                });
                return;
            }

            if (title) window.alert(title);
        });
    }

    function wireConfirmForms() {
        document.addEventListener('submit', event => {
            const form = event.target;

            if (!(form instanceof HTMLFormElement)) return;
            if (!form.matches('form.js-confirm-delete, form.js-workflow-form')) return;
            if (form.dataset.symplifyConfirmed === 'true') return;

            event.preventDefault();

            const title = form.dataset.confirmTitle || 'İşlem onaylansın mı?';
            const text = form.dataset.confirmText || 'Bu işlem geri alınamayabilir.';
            const confirmButton = form.dataset.confirmButton || 'Evet';

            if (!window.Swal || typeof window.Swal.fire !== 'function') {
                if (window.confirm(`${title}\n\n${text}`)) {
                    submitConfirmed(form);
                }
                return;
            }

            window.Swal.fire({
                icon: 'warning',
                title: title,
                text: text,
                showCancelButton: true,
                confirmButtonText: confirmButton,
                cancelButtonText: 'Vazgeç',
                reverseButtons: true,
                focusCancel: true
            }).then(result => {
                if (!result.isConfirmed) return;
                submitConfirmed(form);
            });
        });
    }

    function submitConfirmed(form) {
        form.dataset.symplifyConfirmed = 'true';

        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
            return;
        }

        form.submit();
    }
}());
