document.addEventListener('DOMContentLoaded', function () {
  if (window.jQuery && jQuery.fn.select2) {
    jQuery('#facultySelect').select2({ placeholder: 'Fakülte seçiniz', width: '100%' });
    jQuery('#departmentSelect').select2({ placeholder: 'Bölüm seçiniz', width: '100%' });
  }

  const dateEl = document.getElementById('studentBirthDate');
  if (dateEl && window.tempusDominus) {
    new tempusDominus.TempusDominus(dateEl, {
      display: { components: { clock: false } },
      localization: { locale: 'tr', format: 'dd.MM.yyyy' }
    });
  }

  const phone = document.getElementById('studentPhone');
  if (phone && window.intlTelInput) {
    window.intlTelInput(phone, {
      initialCountry: 'tr',
      preferredCountries: ['tr', 'az'],
      separateDialCode: true,
      loadUtils: function () { return import('/assets/vendor/intl-tel-input/js/utils.js'); }
    });
  }

  const form = document.getElementById('studentCreateForm');
  if (!form) return;

  const wizard = form.closest('.form-wizard');
  const fieldsets = Array.from(form.querySelectorAll('.wizard-fieldset'));
  const steps = Array.from(wizard.querySelectorAll('.form-wizard-list__item'));

  const validateFieldset = (fieldset) => {
    let valid = true;

    fieldset.querySelectorAll('.wizard-required').forEach((field) => {
      const empty = !String(field.value || '').trim();
      const error = field.parentElement?.querySelector('.wizard-form-error');
      field.classList.toggle('is-invalid', empty);
      if (error) error.style.display = empty ? 'block' : 'none';
      if (empty) valid = false;
    });

    return valid;
  };

  const showStep = (index) => {
    fieldsets.forEach((fieldset, fieldsetIndex) => {
      fieldset.classList.toggle('show', fieldsetIndex === index);
    });

    steps.forEach((step, stepIndex) => {
      step.classList.toggle('active', stepIndex === index);
      step.classList.toggle('activated', stepIndex < index);
    });

    const activeFieldset = fieldsets[index];
    activeFieldset?.querySelector('.select2-hidden-accessible')?.dispatchEvent(new Event('change'));
    wizard.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

  wizard.querySelectorAll('.form-wizard-next-btn').forEach((button) => {
    button.addEventListener('click', function () {
      const currentFieldset = button.closest('.wizard-fieldset');
      const currentIndex = fieldsets.indexOf(currentFieldset);
      if (!validateFieldset(currentFieldset)) return;
      if (currentIndex < fieldsets.length - 1) showStep(currentIndex + 1);
    });
  });

  wizard.querySelectorAll('.form-wizard-previous-btn').forEach((button) => {
    button.addEventListener('click', function () {
      const currentIndex = fieldsets.indexOf(button.closest('.wizard-fieldset'));
      if (currentIndex > 0) showStep(currentIndex - 1);
    });
  });

  form.querySelectorAll('.wizard-required').forEach((field) => {
    field.addEventListener('input', function () {
      if (String(field.value || '').trim()) {
        field.classList.remove('is-invalid');
        const error = field.parentElement?.querySelector('.wizard-form-error');
        if (error) error.style.display = 'none';
      }
    });
    field.addEventListener('change', function () {
      if (String(field.value || '').trim()) {
        field.classList.remove('is-invalid');
        const error = field.parentElement?.querySelector('.wizard-form-error');
        if (error) error.style.display = 'none';
      }
    });
  });

  form.addEventListener('submit', function (e) {
    e.preventDefault();

    const invalidIndex = fieldsets.findIndex((fieldset) => !validateFieldset(fieldset));
    if (invalidIndex !== -1) {
      showStep(invalidIndex);
      return;
    }

    if (window.Swal) {
      Swal.fire({
        icon: 'success',
        title: 'Kayıt hazır',
        text: 'Demo ortamında öğrenci kaydı başarıyla doğrulandı.',
        confirmButtonText: 'Tamam'
      });
    }
  });
});
