(function () {
    'use strict';

    const modalElement = document.getElementById('finalFileUploadModal');
    const form = document.getElementById('finalFileUploadForm');
    const input = document.getElementById('finalFileUploadInput');
    const nameLabel = document.querySelector('[data-final-file-upload-name]');
    const titleLabel = document.getElementById('finalFileUploadModalLabel');
    const zoneTitle = document.querySelector('[data-final-file-upload-zone-title]');
    const description = document.querySelector('[data-final-file-upload-description]');
    const submitText = document.querySelector('[data-final-file-upload-submit]');

    if (!modalElement || !form || !input) return;

    const modal = window.bootstrap ? new window.bootstrap.Modal(modalElement) : null;
    const defaultFileText = nameLabel ? nameLabel.textContent : '';

    document.querySelectorAll('[data-final-file-upload-trigger]').forEach(trigger => {
        trigger.addEventListener('click', function () {
            if (this.disabled) return;

            form.action = this.dataset.uploadAction || '#';
            input.name = this.dataset.uploadInputName || 'file';
            input.accept = this.dataset.uploadAccept || '';
            input.value = '';

            const title = this.dataset.uploadTitle || '';
            const desc = this.dataset.uploadDescription || '';
            const submit = this.dataset.uploadSubmitText || '';

            if (titleLabel) titleLabel.textContent = title;
            if (zoneTitle) zoneTitle.textContent = title;
            if (description) description.textContent = desc;
            if (submitText) submitText.textContent = submit;
            if (nameLabel) nameLabel.textContent = defaultFileText;

            if (modal) modal.show();
        });
    });

    input.addEventListener('change', function () {
        if (!nameLabel) return;
        nameLabel.textContent = this.files && this.files.length > 0
            ? this.files[0].name
            : defaultFileText;
    });
})();
