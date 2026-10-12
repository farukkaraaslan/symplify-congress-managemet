document.addEventListener('DOMContentLoaded', function () {
  if (typeof ApexCharts === 'undefined') return;
  const css = getComputedStyle(document.documentElement);
  const primary = css.getPropertyValue('--primary-600').trim() || '#2457c5';
  const success = css.getPropertyValue('--success-main').trim() || '#45b369';
  const warning = css.getPropertyValue('--warning-main').trim() || '#ff9f29';
  const danger = css.getPropertyValue('--danger-main').trim() || '#ef4a00';
  const muted = css.getPropertyValue('--text-muted').trim() || '#6f7b8e';
  const border = css.getPropertyValue('--border-soft').trim() || '#edf0f5';

  const hours = document.querySelector('#workloadHoursChart');
  if (hours) new ApexCharts(hours, {
    series: [{ name: 'Haftalık Ders Saati', data: [312, 244, 198, 176, 142] }],
    chart: { type: 'bar', height: 286, toolbar: { show: false }, fontFamily: 'var(--default-font)' },
    colors: [primary],
    plotOptions: { bar: { horizontal: true, borderRadius: 5, barHeight: '48%' } },
    dataLabels: { enabled: false },
    xaxis: { categories: ['Bilgisayar Müh.', 'Elektrik-Elektronik', 'Matematik', 'İşletme', 'Hukuk'], labels: { style: { colors: muted } } },
    yaxis: { labels: { style: { colors: muted } } },
    grid: { borderColor: border, strokeDashArray: 3 },
    tooltip: { theme: 'light' }
  }).render();

  const donut = document.querySelector('#workloadStatusDonut');
  if (donut) new ApexCharts(donut, {
    series: [176, 31, 7], labels: ['Normal', 'Sınıra Yakın', 'Aşım'],
    chart: { type: 'donut', height: 238, toolbar: { show: false } },
    colors: [success, warning, danger], dataLabels: { enabled: false },
    stroke: { width: 3 }, legend: { show: false },
    plotOptions: { pie: { donut: { size: '72%', labels: { show: true, name: { show: true }, value: { show: true, fontWeight: 700 }, total: { show: true, label: 'Toplam', formatter: () => '214' } } } } }
  }).render();
});