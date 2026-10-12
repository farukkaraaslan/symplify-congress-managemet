document.addEventListener('DOMContentLoaded', function () {
  const calendarEl = document.getElementById('calendar');
  if (!calendarEl || typeof FullCalendar === 'undefined') return;

  const root = document.documentElement;
  const syncColorScheme = () => {
    root.setAttribute('data-color-scheme', root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light');
  };

  syncColorScheme();
  new MutationObserver(syncColorScheme).observe(root, {
    attributes: true,
    attributeFilter: ['data-theme']
  });

  const calendar = new FullCalendar.Calendar(calendarEl, {
    initialView: 'dayGridMonth',
    initialDate: '2026-09-10',
    locale: 'tr',
    firstDay: 1,
    height: 'auto',
    fixedWeekCount: false,
    expandRows: false,
    nowIndicator: true,
    dayMaxEvents: true,
    eventDisplay: 'block',
    displayEventEnd: true,
    allDayText: 'Tüm gün',
    slotMinTime: '06:00:00',
    slotMaxTime: '22:00:00',
    slotDuration: '00:30:00',
    slotLabelInterval: '01:00:00',
    slotLabelFormat: {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    },
    eventTimeFormat: {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    },
    headerToolbar: {
      start: 'prev,next today',
      center: 'title',
      end: 'dayGridMonth timeGridWeek timeGridDay listWeek'
    },
    events: [
      {
        title: 'Ders Kayıt Başlangıcı',
        start: '2026-09-01',
        backgroundColor: 'var(--primary-600)',
        borderColor: 'var(--primary-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Danışman Onayları',
        start: '2026-09-07',
        end: '2026-09-11',
        backgroundColor: 'var(--info-600)',
        borderColor: 'var(--info-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Ders Kayıt Sonu',
        start: '2026-09-12',
        backgroundColor: 'var(--danger-600)',
        borderColor: 'var(--danger-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Bölüm Kurulu Toplantısı',
        start: '2026-09-15T14:00:00',
        backgroundColor: 'var(--lilac-600)',
        borderColor: 'var(--lilac-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Ekle / Bırak Başlangıcı',
        start: '2026-09-21',
        backgroundColor: 'var(--success-600)',
        borderColor: 'var(--success-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'BLG203 Ara Sınav',
        start: '2026-10-19T10:00:00',
        end: '2026-10-19T11:30:00',
        backgroundColor: 'var(--warning-600)',
        borderColor: 'var(--warning-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Vize Haftası',
        start: '2026-10-19',
        end: '2026-10-25',
        backgroundColor: 'var(--warning-600)',
        borderColor: 'var(--warning-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Mazeret Sınavları',
        start: '2026-11-02',
        end: '2026-11-06',
        backgroundColor: 'var(--info-600)',
        borderColor: 'var(--info-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Derslerin Son Günü',
        start: '2026-12-25',
        backgroundColor: 'var(--danger-600)',
        borderColor: 'var(--danger-600)',
        textColor: 'var(--white)'
      },
      {
        title: 'Final Haftası',
        start: '2027-01-04',
        end: '2027-01-16',
        backgroundColor: 'var(--danger-600)',
        borderColor: 'var(--danger-600)',
        textColor: 'var(--white)'
      }
    ]
  });

  calendar.render();
});
