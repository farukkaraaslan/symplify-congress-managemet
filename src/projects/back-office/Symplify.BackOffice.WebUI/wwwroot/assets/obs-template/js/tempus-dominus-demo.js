(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {
    if (!window.tempusDominus || !window.tempusDominus.TempusDominus) {
      console.error('Tempus Dominus yüklenemedi. assets/vendor/tempus-dominus dosyalarını kontrol edin.');
      return;
    }

    var TempusDominus = window.tempusDominus.TempusDominus;
    var pickers = [];

    var icons = {
      type: 'icons',
      time: 'ri-time-line',
      date: 'ri-calendar-line',
      up: 'ri-arrow-up-s-line',
      down: 'ri-arrow-down-s-line',
      previous: 'ri-arrow-left-s-line',
      next: 'ri-arrow-right-s-line',
      today: 'ri-calendar-check-line',
      clear: 'ri-delete-bin-6-line',
      close: 'ri-close-line'
    };

    var tr = {
      locale: 'tr',
      startOfTheWeek: 1,
      hourCycle: 'h23',
      today: 'Bugüne git',
      clear: 'Seçimi temizle',
      close: 'Kapat',
      selectMonth: 'Ay seç',
      previousMonth: 'Önceki ay',
      nextMonth: 'Sonraki ay',
      selectYear: 'Yıl seç',
      previousYear: 'Önceki yıl',
      nextYear: 'Sonraki yıl',
      selectDecade: 'On yıl seç',
      previousDecade: 'Önceki on yıl',
      nextDecade: 'Sonraki on yıl',
      previousCentury: 'Önceki yüzyıl',
      nextCentury: 'Sonraki yüzyıl',
      pickHour: 'Saat seç',
      incrementHour: 'Saati artır',
      decrementHour: 'Saati azalt',
      pickMinute: 'Dakika seç',
      incrementMinute: 'Dakikayı artır',
      decrementMinute: 'Dakikayı azalt',
      pickSecond: 'Saniye seç',
      incrementSecond: 'Saniyeyi artır',
      decrementSecond: 'Saniyeyi azalt',
      toggleMeridiem: 'ÖÖ/ÖS değiştir',
      selectTime: 'Saat seç',
      selectDate: 'Tarih seç'
    };

    function currentTheme() {
      return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    function common(format) {
      return {
        allowInputToggle: false,
        localization: Object.assign({}, tr, { format: format }),
        display: {
          theme: currentTheme(),
          icons: icons,
          keyboardNavigation: true
        }
      };
    }

    function create(id, options) {
      var element = document.getElementById(id);
      if (!element) return null;

      // Aynı element iki kez initialize edilmesin.
      if (element.dataset.tdInitialized === 'true') {
        return null;
      }

      var picker = new TempusDominus(element, options);
      element.dataset.tdInitialized = 'true';
      pickers.push(picker);

      /*
       * allowInputToggle kullanmıyoruz.
       * Bazı browser / template event kombinasyonlarında focus + click
       * peş peşe toggle davranışı üretip widget'ı açıp hemen kapatabiliyor.
       *
       * Input etkileşiminde toggle() yerine show() kullanıyoruz:
       * - focus -> aç
       * - click -> açık kal
       * - ikinci show() çağrısı kapatmaz
       */
      var input = element.querySelector('input[data-td-target]');
      if (input) {
        input.addEventListener('focus', function () {
          picker.show();
        });

        input.addEventListener('click', function (event) {
          event.stopPropagation();
          picker.show();
        });
      }

      return picker;
    }

    var dateOnly = common('dd.MM.yyyy');
    dateOnly.display.components = {
      calendar: true,
      date: true,
      month: true,
      year: true,
      decades: true,
      clock: false,
      hours: false,
      minutes: false,
      seconds: false
    };
    create('tdDateOnly', dateOnly);

    var dateTime = common('dd.MM.yyyy HH:mm');
    dateTime.promptTimeOnDateChange = true;
    dateTime.promptTimeOnDateChangeTransitionDelay = 150;
    create('tdDateTime', dateTime);

    var timeOnly = common('HH:mm');
    timeOnly.display.viewMode = 'clock';
    timeOnly.display.components = {
      calendar: false,
      date: false,
      month: false,
      year: false,
      decades: false,
      clock: true,
      hours: true,
      minutes: true,
      seconds: false
    };
    create('tdTimeOnly', timeOnly);

    var sideBySide = common('dd.MM.yyyy HH:mm');
    sideBySide.display.sideBySide = true;
    sideBySide.display.toolbarPlacement = 'bottom';
    sideBySide.display.buttons = { today: true, clear: true, close: true };
    create('tdSideBySide', sideBySide);

    var restricted = common('dd.MM.yyyy');
    restricted.restrictions = {
      minDate: new Date(),
      daysOfWeekDisabled: [0, 6]
    };
    restricted.display.components = {
      calendar: true,
      date: true,
      month: true,
      year: true,
      decades: true,
      clock: false,
      hours: false,
      minutes: false,
      seconds: false
    };
    create('tdRestricted', restricted);

    var monthYear = common('MM.yyyy');
    monthYear.display.viewMode = 'months';
    monthYear.display.components = {
      calendar: true,
      date: false,
      month: true,
      year: true,
      decades: true,
      clock: false,
      hours: false,
      minutes: false,
      seconds: false
    };
    create('tdMonthYear', monthYear);

    var inline = common('dd.MM.yyyy');
    inline.display.inline = true;
    inline.display.buttons = { today: true, clear: true, close: false };
    inline.display.components = {
      calendar: true,
      date: true,
      month: true,
      year: true,
      decades: true,
      clock: false,
      hours: false,
      minutes: false,
      seconds: false
    };
    create('tdInline', inline);

    // WowDash theme değiştiğinde açık/kapalı bütün picker'ları aynı temaya geçir.
    var observer = new MutationObserver(function (mutations) {
      var changed = mutations.some(function (mutation) {
        return mutation.type === 'attributes' && mutation.attributeName === 'data-theme';
      });
      if (!changed) return;

      var theme = currentTheme();
      pickers.forEach(function (picker) {
        try {
          picker.updateOptions({ display: { theme: theme } });
        } catch (error) {
          console.warn('Tempus Dominus tema güncellemesi uygulanamadı.', error);
        }
      });
    });

    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
  });
})();
