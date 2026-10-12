document.addEventListener('DOMContentLoaded', function () {
  if (typeof ApexCharts === 'undefined') return;
  const css = getComputedStyle(document.documentElement);
  const primary = css.getPropertyValue('--primary-600').trim() || '#487fff';
  const success = css.getPropertyValue('--success-main').trim() || '#45b369';
  const warning = css.getPropertyValue('--warning-main').trim() || '#f4941e';
  const info = css.getPropertyValue('--info-main').trim() || '#00b8f2';

  const trend = document.querySelector('#obsStudentTrendChart');
  if (trend) {
    new ApexCharts(trend, {
      series: [{ name: 'Öğrenci', data: [7420, 7680, 7915, 8160, 8426] }],
      chart: { type: 'area', height: 310, toolbar: { show: false } },
      stroke: { curve: 'smooth', width: 3 },
      fill: { type: 'gradient', gradient: { opacityFrom: .28, opacityTo: .02 } },
      colors: [primary], dataLabels: { enabled: false },
      xaxis: { categories: ['2022','2023','2024','2025','2026'] },
      grid: { borderColor: 'rgba(148,163,184,.18)' }
    }).render();
  }

  const faculty = document.querySelector('#obsFacultyChart');
  if (faculty) {
    new ApexCharts(faculty, {
      series: [2860, 1780, 1220, 980, 1586],
      labels: ['Mühendislik','İİBF','Fen','Eğitim','Diğer'],
      chart: { type: 'donut', height: 310 },
      colors: [primary, success, warning, info, '#8252e9'],
      legend: { position: 'bottom' },
      dataLabels: { enabled: false }
    }).render();
  }
});
