document.addEventListener('DOMContentLoaded', function () {
  if (!window.jQuery || !jQuery.fn || typeof jQuery.fn.jstree !== 'function') return;
  const $ = jQuery;
  const nodes = [
    { id:'u1', parent:'#', text:'Akademi Başkanlığı', type:'root', data:{code:'JSGA',level:1,typeName:'Üst Birim',parent:'-',manager:'Akademi Başkanı'} },
    { id:'u2', parent:'u1', text:'Eğitim Merkezi Komutanlığı', type:'folder', data:{code:'EMK',level:2,typeName:'Komutanlık',parent:'Akademi Başkanlığı',manager:'Eğitim Merkezi Komutanı'} },
    { id:'u3', parent:'u2', text:'Güvenlik Bilimleri Fakültesi', type:'folder', data:{code:'GBF',level:3,typeName:'Fakülte',parent:'Eğitim Merkezi Komutanlığı',manager:'Dekan'} },
    { id:'u4', parent:'u3', text:'Bilgisayar ve Bilgi Sistemleri Bölüm Başkanlığı', type:'folder', data:{code:'BBS',level:4,typeName:'Bölüm',parent:'Güvenlik Bilimleri Fakültesi',manager:'Bölüm Başkanı'} },
    { id:'u5', parent:'u4', text:'Yazılım Sistemleri Ana Bilim Dalı', type:'folder', data:{code:'YSA',level:5,typeName:'Ana Bilim Dalı',parent:'Bilgisayar ve Bilgi Sistemleri Bölüm Başkanlığı',manager:'Ana Bilim Dalı Başkanı'} },
    { id:'u6', parent:'u5', text:'Yazılım Geliştirme Programı', type:'folder', data:{code:'YGP',level:6,typeName:'Program',parent:'Yazılım Sistemleri Ana Bilim Dalı',manager:'Program Koordinatörü'} },
    { id:'u7', parent:'u6', text:'1. Sınıf / A Grubu', type:'file', data:{code:'YGP-1A',level:7,typeName:'Sınıf / Grup',parent:'Yazılım Geliştirme Programı',manager:'Sınıf Danışmanı'} },
    { id:'u7b', parent:'u6', text:'1. Sınıf / B Grubu', type:'file', data:{code:'YGP-1B',level:7,typeName:'Sınıf / Grup',parent:'Yazılım Geliştirme Programı',manager:'Sınıf Danışmanı'} },
    { id:'u5b', parent:'u4', text:'Siber Güvenlik Ana Bilim Dalı', type:'folder', data:{code:'SGA',level:5,typeName:'Ana Bilim Dalı',parent:'Bilgisayar ve Bilgi Sistemleri Bölüm Başkanlığı',manager:'Ana Bilim Dalı Başkanı'} },
    { id:'u3b', parent:'u2', text:'Meslek Yüksekokulu', type:'folder', data:{code:'MYO',level:3,typeName:'Yüksekokul',parent:'Eğitim Merkezi Komutanlığı',manager:'Müdür'} }
  ];

  const $tree = $('#organizationTree').jstree({
    core:{ data:nodes, check_callback:true, themes:{ dots:true, icons:true } },
    types:{
      root:{ icon:'ri-building-4-fill text-primary-600' },
      folder:{ icon:'ri-folder-3-fill text-warning-main' },
      file:{ icon:'ri-group-line text-info-main' }
    },
    plugins:['search','types','wholerow']
  });

  function showDetail(node) {
    if (!node || !node.data) return;
    document.getElementById('unitDetailName').textContent=node.text;
    document.getElementById('unitDetailType').textContent=`Seviye ${node.data.level} • ${node.data.typeName}`;
    document.getElementById('unitDetailCode').textContent=node.data.code;
    document.getElementById('unitDetailLevel').textContent=node.data.level;
    document.getElementById('unitDetailParent').textContent=node.data.parent;
    document.getElementById('unitDetailManager').textContent=node.data.manager;
  }

  $tree.on('select_node.jstree', function(_, data){ showDetail(data.node); });
  $tree.on('ready.jstree', function(){ const inst=$tree.jstree(true); inst.open_node(['u1','u2','u3','u4','u5','u6']); inst.select_node('u1'); });
  document.getElementById('unitsOpenAll')?.addEventListener('click',()=> $tree.jstree(true).open_all());
  document.getElementById('unitsCloseAll')?.addEventListener('click',()=> $tree.jstree(true).close_all());
  let timer;
  document.getElementById('unitTreeSearch')?.addEventListener('keyup',function(){ clearTimeout(timer); const q=this.value; timer=setTimeout(()=>{const inst=$tree.jstree(true); if(!q){inst.clear_search();return;} inst.open_all(); inst.search(q);},180); });
});
