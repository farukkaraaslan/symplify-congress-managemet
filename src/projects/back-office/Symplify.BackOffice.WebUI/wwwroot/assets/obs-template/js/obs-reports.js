document.addEventListener('DOMContentLoaded', function () {
  if (typeof ApexCharts === 'undefined') return;
  const css = getComputedStyle(document.documentElement);
  const primary = css.getPropertyValue('--primary-600').trim() || '#2457c5';
  const success = css.getPropertyValue('--success-main').trim() || '#45b369';
  const warning = css.getPropertyValue('--warning-main').trim() || '#ff9f29';
  const info = css.getPropertyValue('--info-main').trim() || '#2563eb';
  const danger = css.getPropertyValue('--danger-main').trim() || '#ef4a00';
  const muted = css.getPropertyValue('--text-muted').trim() || '#6f7b8e';
  const border = css.getPropertyValue('--border-soft').trim() || '#edf0f5';

  const faculty = document.querySelector('#obsFacultyStudentBar');
  if (faculty) new ApexCharts(faculty, {
    series:[{name:'Öğrenci',data:[2860,1780,1220,980,1586]}],
    chart:{type:'bar',height:285,toolbar:{show:false}}, colors:[primary],
    plotOptions:{bar:{borderRadius:5,columnWidth:'40%'}}, dataLabels:{enabled:false},
    xaxis:{categories:['Mühendislik','İİBF','Fen','Eğitim','Diğer'],labels:{style:{colors:muted}}},
    yaxis:{labels:{style:{colors:muted}}}, grid:{borderColor:border,strokeDashArray:3}
  }).render();

  const status = document.querySelector('#obsAcademicStatusDonut');
  if (status) new ApexCharts(status, {
    series:[78,9,7,6], labels:['Aktif','İzinli','Dondurmuş','Diğer'],
    chart:{type:'donut',height:285}, colors:[primary,success,warning,danger],
    legend:{position:'bottom',fontSize:'12px'}, dataLabels:{enabled:false},
    stroke:{width:3}, plotOptions:{pie:{donut:{size:'70%'}}}
  }).render();

  const trend = document.querySelector('#obsRegistrationTrend');
  if (trend) new ApexCharts(trend, {
    series:[{name:'Yeni Kayıt',data:[1320,1398,1455,1512,1608]}],
    chart:{type:'area',height:260,toolbar:{show:false}}, colors:[primary],
    stroke:{curve:'smooth',width:2.5}, dataLabels:{enabled:false},
    fill:{type:'gradient',gradient:{opacityFrom:.18,opacityTo:.015}},
    xaxis:{categories:['2022-23','2023-24','2024-25','2025-26','2026-27'],labels:{style:{colors:muted}}},
    yaxis:{labels:{style:{colors:muted}}}, grid:{borderColor:border,strokeDashArray:3}
  }).render();
});