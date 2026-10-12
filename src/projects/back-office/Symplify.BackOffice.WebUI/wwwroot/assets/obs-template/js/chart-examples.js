document.addEventListener('DOMContentLoaded', function () {
  if (typeof ApexCharts === 'undefined') return;

  const css = (name, fallback) => {
    const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return v || fallback;
  };
  const C = {
    primary: css('--primary-600', '#487FFF'),
    info: css('--info-main', '#45B9E9'),
    success: css('--success-main', '#45B369'),
    warning: css('--warning-main', '#FF9F29'),
    danger: css('--danger-main', '#EF4A00'),
    purple: '#8B5CF6',
    cyan: '#06B6D4',
    gray: '#94A3B8',
    grid: css('--border-subtle', '#E5E7EB'),
    text: css('--text-secondary', '#64748B')
  };

  function mount(id, options) {
    const el = document.getElementById(id);
    if (!el) return;
    options.chart = Object.assign({ height: 300, toolbar: { show: false }, fontFamily: 'inherit' }, options.chart || {});
    options.grid = Object.assign({ borderColor: C.grid, strokeDashArray: 3 }, options.grid || {});
    try { new ApexCharts(el, options).render(); } catch (e) { console.error('Chart render error:', id, e); }
  }

  const months = ['Eyl','Eki','Kas','Ara','Oca','Şub','Mar','Nis','May','Haz','Tem','Ağu'];
  const depts = ['Bilgisayar','Yazılım','Elektrik','İşletme','Matematik','Hukuk'];

  // LINE
  mount('lineEnrollmentTrend', { chart:{type:'line'}, series:[{name:'Kayıt',data:[620,710,760,820,845,880,910,955,1010,1045,1078,1120]}], colors:[C.primary], stroke:{curve:'smooth',width:3}, markers:{size:4}, dataLabels:{enabled:false}, xaxis:{categories:months}, yaxis:{min:500} });
  mount('lineGpaCompare', { chart:{type:'line'}, series:[{name:'2025',data:[2.72,2.78,2.81,2.86,2.89,2.94,2.98,3.01]},{name:'2026',data:[2.81,2.86,2.90,2.96,3.00,3.05,3.09,3.14]}], colors:[C.gray,C.primary], stroke:{curve:'straight',width:3}, markers:{size:3}, xaxis:{categories:['1','2','3','4','5','6','7','8']}, yaxis:{min:2.5,max:3.5} });
  mount('lineGraduation', { chart:{type:'line'}, series:[{name:'Mezun',data:[410,438,469,501,522,548,579]}], colors:[C.success], stroke:{curve:'smooth',width:4}, markers:{size:0}, dataLabels:{enabled:true}, xaxis:{categories:['2020','2021','2022','2023','2024','2025','2026']} });
  mount('lineAttendanceStep', { chart:{type:'line'}, series:[{name:'Devam %',data:[97,95,94,92,91,93,90,89,91,92]}], colors:[C.warning], stroke:{curve:'stepline',width:3}, xaxis:{categories:['1','2','3','4','5','6','7','8','9','10']}, yaxis:{min:80,max:100} });
  mount('lineExamAverage', { chart:{type:'line'}, series:[{name:'Vize',data:[68,72,64,76,70,74]},{name:'Final',data:[73,76,69,79,74,78]}], colors:[C.info,C.purple], stroke:{curve:'smooth',width:[3,3]}, markers:{size:5}, xaxis:{categories:depts} });
  mount('lineCourseDemand', { chart:{type:'line'}, series:[{name:'Talep',data:[120,145,132,168,190,185,212,238]}], colors:[C.danger], stroke:{curve:'straight',width:2}, markers:{size:6}, xaxis:{categories:['BLG101','BLG203','MAT101','FIZ101','ISL205','HUK301','YAZ302','BLG401']} });

  // COLUMN
  mount('columnFacultyStudents', { chart:{type:'bar'}, series:[{name:'Öğrenci',data:[2840,1760,1220,980,1530,860]}], colors:[C.primary], plotOptions:{bar:{borderRadius:5,columnWidth:'48%'}}, dataLabels:{enabled:false}, xaxis:{categories:['Mühendislik','İİBF','Fen','Eğitim','Hukuk','Diğer']} });
  mount('columnGroupedEnrollment', { chart:{type:'bar'}, series:[{name:'Yeni Kayıt',data:[540,410,280,195,230,160]},{name:'Kayıt Yenileme',data:[2180,1350,940,785,1300,700]}], colors:[C.primary,C.info], plotOptions:{bar:{borderRadius:4,columnWidth:'58%'}}, dataLabels:{enabled:false}, xaxis:{categories:depts} });
  mount('columnStackedStatus', { chart:{type:'bar',stacked:true}, series:[{name:'Aktif',data:[720,610,540,488,375,420]},{name:'İzinli',data:[42,35,29,31,18,22]},{name:'Dondurmuş',data:[18,22,16,14,9,11]}], colors:[C.success,C.warning,C.gray], plotOptions:{bar:{borderRadius:3,columnWidth:'55%'}}, dataLabels:{enabled:false}, xaxis:{categories:depts} });
  mount('columnGradeDistribution', { chart:{type:'bar'}, series:[{name:'Öğrenci',data:[84,132,188,240,318,355,286,169,82]}], colors:[C.purple], plotOptions:{bar:{distributed:false,borderRadius:4}}, dataLabels:{enabled:false}, xaxis:{categories:['AA','BA','BB','CB','CC','DC','DD','FD','FF']} });
  mount('columnExamSuccess', { chart:{type:'bar'}, series:[{name:'Başarı %',data:[91,88,84,86,93,82]}], colors:[C.success], plotOptions:{bar:{borderRadius:6,columnWidth:'42%'}}, dataLabels:{enabled:true}, xaxis:{categories:depts}, yaxis:{max:100} });
  mount('columnQuota', { chart:{type:'bar'}, series:[{name:'Kontenjan',data:[90,80,70,60,75,55]},{name:'Yerleşen',data:[88,79,68,58,73,54]}], colors:[C.gray,C.primary], plotOptions:{bar:{borderRadius:4}}, dataLabels:{enabled:false}, xaxis:{categories:depts} });

  // PIE / DONUT
  mount('pieStudentStatus', { chart:{type:'donut'}, series:[78,9,7,6], labels:['Aktif','İzinli','Kayıt Dondurmuş','Diğer'], colors:[C.primary,C.success,C.warning,C.danger], dataLabels:{enabled:false}, legend:{position:'bottom'}, plotOptions:{pie:{donut:{size:'68%'}}} });
  mount('pieCourseType', { chart:{type:'pie'}, series:[46,22,18,14], labels:['Zorunlu','Mesleki Seçmeli','Üniversite Seçmeli','Ortak Ders'], colors:[C.primary,C.info,C.success,C.warning], legend:{position:'bottom'} });
  mount('pieEducationType', { chart:{type:'donut'}, series:[62,25,13], labels:['Örgün','İkinci Öğretim','Uzaktan'], colors:[C.primary,C.purple,C.cyan], dataLabels:{enabled:true}, legend:{position:'bottom'} });
  mount('pieRegistration', { chart:{type:'pie'}, series:[83,11,6], labels:['Tamamlandı','Danışman Bekliyor','Eksik'], colors:[C.success,C.warning,C.danger], legend:{position:'bottom'} });
  mount('pieExamResult', { chart:{type:'donut'}, series:[71,18,11], labels:['Başarılı','Koşullu','Başarısız'], colors:[C.success,C.warning,C.danger], plotOptions:{pie:{donut:{size:'72%'}}}, legend:{position:'bottom'} });
  mount('pieCampusLoad', { chart:{type:'polarArea'}, series:[38,27,19,16], labels:['Merkez','Kuzey','Güney','Uygulama'], colors:[C.primary,C.info,C.purple,C.warning], stroke:{width:1}, fill:{opacity:.85}, legend:{position:'bottom'} });

  // AREA
  mount('areaEnrollment', { chart:{type:'area'}, series:[{name:'Toplam Öğrenci',data:[6420,6610,6840,7030,7310,7580,7820]}], colors:[C.primary], stroke:{curve:'smooth',width:3}, fill:{type:'gradient',gradient:{opacityFrom:.42,opacityTo:.06}}, dataLabels:{enabled:false}, xaxis:{categories:['2020','2021','2022','2023','2024','2025','2026']} });
  mount('areaMultiStatus', { chart:{type:'area'}, series:[{name:'Aktif',data:[6200,6410,6580,6810,7040,7280]},{name:'Mezun',data:[410,438,469,501,522,548]}], colors:[C.primary,C.success], stroke:{curve:'smooth',width:2}, fill:{opacity:.16}, dataLabels:{enabled:false}, xaxis:{categories:['2021','2022','2023','2024','2025','2026']} });
  mount('areaApplications', { chart:{type:'area'}, series:[{name:'Başvuru',data:[120,185,260,340,470,610,780,920]}], colors:[C.info], stroke:{curve:'straight',width:2}, fill:{type:'gradient',gradient:{opacityFrom:.5,opacityTo:.08}}, xaxis:{categories:['1.Hf','2.Hf','3.Hf','4.Hf','5.Hf','6.Hf','7.Hf','8.Hf']} });
  mount('areaAttendance', { chart:{type:'area'}, series:[{name:'Devam',data:[96,95,94,92,93,91,90,92,94,93]}], colors:[C.success], stroke:{curve:'smooth',width:3}, fill:{opacity:.12}, yaxis:{min:80,max:100}, xaxis:{categories:['1','2','3','4','5','6','7','8','9','10']} });
  mount('areaGrades', { chart:{type:'area',stacked:true}, series:[{name:'Başarılı',data:[68,71,74,73,76,79]},{name:'Koşullu',data:[18,17,16,17,15,14]},{name:'Başarısız',data:[14,12,10,10,9,7]}], colors:[C.success,C.warning,C.danger], stroke:{curve:'smooth',width:2}, fill:{opacity:.22}, xaxis:{categories:['2021','2022','2023','2024','2025','2026']} });
  mount('areaCourseDemand', { chart:{type:'area'}, series:[{name:'Ders Seçimi',data:[420,510,570,630,710,690,760,810,860,900,930,970]}], colors:[C.purple], stroke:{curve:'smooth',width:3}, fill:{type:'gradient',gradient:{opacityFrom:.4,opacityTo:.05}}, xaxis:{categories:months} });

  // HORIZONTAL BAR
  mount('barDepartmentSuccess', { chart:{type:'bar'}, series:[{name:'Başarı %',data:[93,91,88,86,84,82]}], colors:[C.primary], plotOptions:{bar:{horizontal:true,borderRadius:5}}, dataLabels:{enabled:true}, xaxis:{categories:depts,max:100} });
  mount('barInstructorLoad', { chart:{type:'bar'}, series:[{name:'Saat',data:[22,20,18,17,16,14]}], colors:[C.info], plotOptions:{bar:{horizontal:true,distributed:true,borderRadius:5}}, dataLabels:{enabled:false}, xaxis:{categories:['M. Arslan','Z. Çelik','K. Aksoy','D. Kılıç','A. Güneş','S. Yılmaz']} });
  mount('barStackedQuota', { chart:{type:'bar',stacked:true}, series:[{name:'Dolu',data:[88,79,68,58,73,54]},{name:'Boş',data:[2,1,2,2,2,1]}], colors:[C.success,C.gray], plotOptions:{bar:{horizontal:true,borderRadius:4}}, xaxis:{categories:depts} });
  mount('barAbsence', { chart:{type:'bar'}, series:[{name:'Devamsızlık %',data:[4.8,5.2,6.1,7.3,3.9,8.1]}], colors:[C.warning], plotOptions:{bar:{horizontal:true,borderRadius:5}}, dataLabels:{enabled:true}, xaxis:{categories:depts,max:10} });
  mount('barDocuments', { chart:{type:'bar'}, series:[{name:'Belge Talebi',data:[420,315,228,190,147,85]}], colors:[C.purple], plotOptions:{bar:{horizontal:true,borderRadius:4}}, xaxis:{categories:['Öğrenci Belgesi','Transkript','Disiplin','Hazırlık','Mezuniyet','Diğer']} });
  mount('barAdvisorLoad', { chart:{type:'bar'}, series:[{name:'Öğrenci',data:[48,44,41,39,37,35]}], colors:[C.cyan], plotOptions:{bar:{horizontal:true,borderRadius:5}}, xaxis:{categories:['A. Kaya','B. Yılmaz','C. Demir','D. Akın','E. Şen','F. Arslan']} });

  // RADIAL / RADAR / POLAR
  mount('radialOverallSuccess', { chart:{type:'radialBar'}, series:[88], colors:[C.primary], plotOptions:{radialBar:{hollow:{size:'64%'},dataLabels:{name:{show:true},value:{fontSize:'26px'}}}}, labels:['Genel Başarı'] });
  mount('radialAttendance', { chart:{type:'radialBar'}, series:[92,86,78], colors:[C.success,C.info,C.warning], plotOptions:{radialBar:{dataLabels:{name:{fontSize:'13px'},value:{fontSize:'18px'},total:{show:true,label:'Ortalama',formatter:function(){return '85%';}}}}}, labels:['1. Sınıf','2. Sınıf','3. Sınıf'] });
  mount('radialRegistration', { chart:{type:'radialBar'}, series:[83], colors:[C.success], plotOptions:{radialBar:{startAngle:-90,endAngle:90,hollow:{size:'60%'},track:{startAngle:-90,endAngle:90},dataLabels:{name:{show:true},value:{fontSize:'24px'}}}}, labels:['Kayıt Tamamlama'] });
  mount('radarOutcomes', { chart:{type:'radar'}, series:[{name:'Program Çıktısı',data:[85,78,92,74,88,81]}], colors:[C.purple], stroke:{width:2}, fill:{opacity:.22}, markers:{size:3}, xaxis:{categories:['Bilgi','Analiz','Tasarım','İletişim','Etik','Takım']} });
  mount('radarDepartments', { chart:{type:'radar'}, series:[{name:'Bilgisayar',data:[91,88,93,86,90]},{name:'Yazılım',data:[89,92,90,88,91]}], colors:[C.primary,C.success], stroke:{width:2}, fill:{opacity:.14}, xaxis:{categories:['Başarı','Devam','Memnuniyet','Proje','Mezuniyet']} });
  mount('polarWorkload', { chart:{type:'polarArea'}, series:[26,22,18,17,14], labels:['Ders','Danışmanlık','Proje','Sınav','Kurul'], colors:[C.primary,C.info,C.success,C.warning,C.purple], fill:{opacity:.85}, legend:{position:'bottom'} });

  // MIXED
  mount('mixedEnrollmentGpa', { chart:{type:'line'}, series:[{name:'Öğrenci',type:'column',data:[6420,6840,7310,7820]},{name:'GNO',type:'line',data:[2.91,2.98,3.05,3.12]}], colors:[C.primary,C.success], stroke:{width:[0,3]}, dataLabels:{enabled:false}, xaxis:{categories:['2023','2024','2025','2026']}, yaxis:[{title:{text:'Öğrenci'}},{opposite:true,min:2.5,max:3.5,title:{text:'GNO'}}] });
  mount('mixedSuccessAttendance', { chart:{type:'line'}, series:[{name:'Başarı %',type:'column',data:[88,91,84,86,93,82]},{name:'Devam %',type:'line',data:[92,94,90,91,95,89]}], colors:[C.primary,C.warning], stroke:{width:[0,3]}, xaxis:{categories:depts}, yaxis:{min:70,max:100} });
  mount('mixedApplications', { chart:{type:'line'}, series:[{name:'Başvuru',type:'area',data:[120,185,260,340,470,610]},{name:'Onay',type:'line',data:[98,160,218,295,416,552]}], colors:[C.info,C.success], stroke:{curve:'smooth',width:[2,3]}, fill:{opacity:[.18,1]}, xaxis:{categories:['1.Hf','2.Hf','3.Hf','4.Hf','5.Hf','6.Hf']} });
  mount('mixedExam', { chart:{type:'line'}, series:[{name:'Katılım',type:'column',data:[820,790,760,710,680,620]},{name:'Ortalama',type:'line',data:[74,76,71,78,80,69]}], colors:[C.purple,C.danger], stroke:{width:[0,3]}, xaxis:{categories:depts}, yaxis:[{title:{text:'Katılım'}},{opposite:true,min:50,max:100,title:{text:'Ortalama'}}] });
  mount('mixedQuota', { chart:{type:'line'}, series:[{name:'Kontenjan',type:'column',data:[90,80,70,60,75,55]},{name:'Doluluk %',type:'line',data:[98,99,97,97,97,98]}], colors:[C.gray,C.primary], stroke:{width:[0,3]}, xaxis:{categories:depts}, yaxis:[{},{opposite:true,min:90,max:100}] });
  mount('mixedGraduates', { chart:{type:'line'}, series:[{name:'Mezun',type:'bar',data:[410,438,469,501,522,548]},{name:'İstihdam %',type:'line',data:[72,74,76,78,81,83]}], colors:[C.success,C.info], stroke:{width:[0,3]}, xaxis:{categories:['2021','2022','2023','2024','2025','2026']}, yaxis:[{},{opposite:true,min:60,max:100}] });

  // ADVANCED: scatter / bubble / heatmap / treemap
  mount('scatterGpaAttendance', { chart:{type:'scatter',zoom:{enabled:true}}, series:[{name:'Öğrenciler',data:[[2.1,72],[2.4,78],[2.7,81],[2.9,86],[3.1,88],[3.3,92],[3.5,94],[3.8,97]]}], colors:[C.primary], markers:{size:6}, xaxis:{tickAmount:7,title:{text:'GNO'}}, yaxis:{title:{text:'Devam %'},min:60,max:100} });
  mount('scatterExamRelation', { chart:{type:'scatter'}, series:[{name:'BLG203',data:[[55,62],[60,68],[65,72],[70,75],[75,82],[80,86],[85,91]]},{name:'MAT101',data:[[50,58],[58,64],[66,70],[72,79],[78,84],[84,89],[90,94]]}], colors:[C.info,C.purple], markers:{size:5}, xaxis:{title:{text:'Vize'}}, yaxis:{title:{text:'Final'}} });
  mount('bubbleCourseDemand', { chart:{type:'bubble'}, series:[{name:'Dersler',data:[[10,20,20],[20,35,28],[30,28,18],[40,48,36],[50,42,25],[60,58,40]]}], colors:[C.success], dataLabels:{enabled:false}, xaxis:{title:{text:'Kontenjan Kullanımı'}}, yaxis:{title:{text:'Talep'}} });
  mount('heatmapAttendance', { chart:{type:'heatmap'}, series:['Cuma','Perşembe','Çarşamba','Salı','Pazartesi'].map(function(day,i){return {name:day,data:[{x:'08',y:82+i*2},{x:'10',y:90-i},{x:'12',y:76+i*3},{x:'14',y:88-i*2},{x:'16',y:71+i*4}]};}), colors:[C.primary], dataLabels:{enabled:false}, plotOptions:{heatmap:{shadeIntensity:.55}} });
  mount('heatmapGrades', { chart:{type:'heatmap'}, series:depts.map(function(d,i){return {name:d,data:[{x:'AA',y:10+i*2},{x:'BB',y:18+i},{x:'CC',y:24-i},{x:'DD',y:15+i},{x:'FF',y:8+i}]};}), colors:[C.warning], dataLabels:{enabled:false}, plotOptions:{heatmap:{shadeIntensity:.5}} });
  mount('treemapFaculty', { chart:{type:'treemap'}, series:[{data:[{x:'Mühendislik',y:2840},{x:'İİBF',y:1760},{x:'Fen',y:1220},{x:'Hukuk',y:1530},{x:'Eğitim',y:980},{x:'Diğer',y:860}]}], colors:[C.primary,C.info,C.success,C.warning,C.purple,C.cyan], legend:{show:false} });
});
