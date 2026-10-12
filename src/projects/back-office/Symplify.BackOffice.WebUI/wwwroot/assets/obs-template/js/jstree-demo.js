(function ($) {
  'use strict';

  if (!$ || !$.fn || !$.fn.jstree) {
    console.error('jsTree yuklenemedi. assets/vendor/jstree dosyalarini kontrol edin.');
    return;
  }

  $(function () {
    function destroyTreeIfExists(selector) {
      var $tree = $(selector);
      if ($tree.data("jstree")) $tree.jstree(true).destroy();
      $tree.empty();
    }

    function debounce(fn, wait) {
      var t = null;
      return function () {
        var ctx = this, args = arguments;
        clearTimeout(t);
        t = setTimeout(function () { fn.apply(ctx, args); }, wait);
      };
    }

    function postJson(url, payload) {
      return fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      }).then(function (r) { return r.ok ? r : Promise.reject(r); });
    }

    function ensureContainer(id, parentSelector) {
      var $el = $(id);
      if ($el.length) return $el;
      var $parent = $(parentSelector);
      if (!$parent.length) return $();
      $parent.append('<div id="' + id.replace("#", "") + '"></div>');
      return $(id);
    }

    // ---------- Destroy (idempotent) ----------
    destroyTreeIfExists("#treePermissionMatrix");
    destroyTreeIfExists("#treeDocuments");
    destroyTreeIfExists("#treeCategoriesDnd");
    destroyTreeIfExists("#treeSearchable");
    destroyTreeIfExists("#treeAjaxLazy");
    destroyTreeIfExists("#treeIconless");

    // =========================================================
    // 1) Permission Matrix (checkbox only, no manual toggle)
    // =========================================================
    var selectedIds = []; // default yok
    var selectedIdSet = selectedIds.map(function (x) { return String(x); });
    var isChecked = function (id) { return selectedIdSet.includes(String(id)); };

    var permissionNodes = [
      {
        id: "group_0",
        text: "Sistem",
        state: { opened: true },
        children: [
          { id: "101", text: "Görüntüleme", state: { checked: isChecked(101) } },
          { id: "102", text: "Düzenleme", state: { checked: isChecked(102) } }
        ]
      },
      {
        id: "group_1",
        text: "Kullanıcılar",
        state: { opened: true },
        children: [
          { id: "201", text: "Listeleme", state: { checked: isChecked(201) } },
          { id: "202", text: "Oluşturma", state: { checked: isChecked(202) } },
          { id: "203", text: "Silme", state: { checked: isChecked(203) } }
        ]
      },
      {
        id: "group_2",
        text: "Raporlar",
        state: { opened: true },
        children: [
          { id: "301", text: "Görüntüleme", state: { checked: isChecked(301) } },
          { id: "302", text: "Dışa Aktarım", state: { checked: isChecked(302) } }
        ]
      }
    ];

    // Hidden input strategy:
    // - #permissionIdsPreview: UI preview
    // - #permissionIds: (mevcutsa) debug amaçlı JSON tutulabilir
    // - #permissionIdsInputs: ASP.NET Core binding için <input name="SelectedPermissionIds" value="..."> tekrarları
    var $permissionInputDebug = $("#permissionIds"); // mevcut sayfanda var
    var $permissionPreview = $("#permissionIdsPreview"); // mevcut sayfanda var
    var $permissionInputs = ensureContainer("#permissionIdsInputs", "#treePermissionMatrix").length
      ? $("#permissionIdsInputs")
      : $("<div></div>");

    $("#treePermissionMatrix")
      .addClass("jstree-checkbox-clean")
      .jstree({
      core: {
        data: permissionNodes,
        check_callback: true,
        multiple: true,
        themes: { icons: false }
      },
      checkbox: {
        three_state: true,
        cascade: "up+down+undetermined",
        tie_selection: false,        // checkbox state ayrı (tavsiye edilen)
        whole_node: true,            // satıra tıklayınca checkbox toggle
        keep_selected_style: false
      },
      plugins: ["checkbox", "wholerow"]
    });

    function syncPermissionToHiddenInputs() {
      var tree = $("#treePermissionMatrix").jstree(true);
      if (!tree) return;

      // Leaf-only gönder (backend’de permission ID listesi için en güvenlisi)
      var ids = (typeof tree.get_bottom_checked === "function") ? tree.get_bottom_checked() : tree.get_checked();

      // Debug (opsiyonel)
      if ($permissionInputDebug.length) $permissionInputDebug.val(JSON.stringify(ids));

      if ($permissionPreview.length) $permissionPreview.text(JSON.stringify(ids, null, 2));

      // ASP.NET Core List<int>/int[] binding:
      // aynı name ile tekrar eden hidden inputlar -> SelectedPermissionIds=101&SelectedPermissionIds=202...
      if ($permissionInputs.length) {
        $permissionInputs.empty();
        for (var i = 0; i < ids.length; i++) {
          $permissionInputs.append(
            '<input type="hidden" name="SelectedPermissionIds" value="' + String(ids[i]) + '">'
          );
        }
      }
    }

    $("#treePermissionMatrix")
      .off(".permission")
      .on("ready.jstree.permission", syncPermissionToHiddenInputs)
      .on("changed.jstree.permission", syncPermissionToHiddenInputs);

    // =========================================================
    // 2) Document Tree (icon-based; no types plugin)
    // =========================================================
    var documentNodes = [
      {
        id: "doc-root",
        text: "Belgeler",
        icon: "ri-folder-line text-warning",
        state: { opened: true },
        children: [
          {
            id: "doc-contracts",
            text: "Sözleşmeler",
            icon: "ri-folder-line text-warning",
            state: { opened: true },
            children: [
              { id: "doc-contract-1", text: "Sözleşme-2026.pdf", icon: "ri-file-pdf-line text-danger" },
              { id: "doc-contract-2", text: "Proforma.docx", icon: "ri-file-word-line text-primary" }
            ]
          },
          {
            id: "doc-reports",
            text: "Raporlar",
            icon: "ri-folder-line text-warning",
            children: [
              { id: "doc-report-1", text: "Satışlar.xlsx", icon: "ri-file-excel-line text-success" }
            ]
          }
        ]
      }
    ];

    $("#treeDocuments").jstree({
      core: { data: documentNodes, check_callback: false, themes: { icons: true } },
      plugins: ["wholerow"]
    });

    // =========================================================
    // 3) Category CRUD + DnD + Context Menu (single flow)
    // =========================================================
    var categoryNodes = [
      {
        id: "cat-root",
        text: "Kategoriler",
        state: { opened: true },
        children: [
          { id: "cat-electronics", text: "Elektronik" },
          { id: "cat-fashion", text: "Moda" },
          {
            id: "cat-home",
            text: "Ev & Yaşam",
            state: { opened: true },
            children: [
              { id: "cat-kitchen", text: "Mutfak" },
              { id: "cat-furniture", text: "Mobilya" }
            ]
          }
        ]
      }
    ];

    $("#treeCategoriesDnd")
      .off(".folder")
      .jstree({
        core: { data: categoryNodes, check_callback: true },
        types: {
          default: { icon: "ri-folder-3-line text-warning" }
        },
        contextmenu: {
          select_node: true,
          items: function (node) {
            return {
              createItem: {
                label: "Yeni Ekle",
                icon: "ri-add-line",
                action: function () {
                  var tree = $("#treeCategoriesDnd").jstree(true);
                  var id = "tmp-" + Date.now();
                  var newNode = tree.create_node(node, { id: id, text: "Yeni Klasör" }, "last");
                  tree.edit(newNode);
                }
              },
              renameItem: {
                label: "Yeniden Adlandır",
                icon: "ri-edit-line",
                action: function () {
                  $("#treeCategoriesDnd").jstree(true).edit(node);
                }
              },
              deleteItem: {
                label: "Sil",
                icon: "ri-delete-bin-line",
                action: function () {
                  $("#treeCategoriesDnd").jstree(true).delete_node(node);
                }
              }
            };
          }
        },
        plugins: ["dnd", "contextmenu", "state", "types", "wholerow"]
      });

    function setFolderPayload(payload) {
      $("#dndPayloadPreview").text(JSON.stringify(payload, null, 2));
    }

    $("#treeCategoriesDnd")
      .on("create_node.jstree.folder", function (e, data) {
        setFolderPayload({ action: "create", id: data.node.id, parentId: data.parent, text: data.node.text });
        // Backend: postJson("/api/Folder/Create", { parentId: data.parent, name: data.node.text })
      })
      .on("rename_node.jstree.folder", function (e, data) {
        setFolderPayload({ action: "rename", id: data.node.id, text: data.text });
        // Backend: postJson("/api/Folder/Rename", { id: data.node.id, name: data.text })
      })
      .on("delete_node.jstree.folder", function (e, data) {
        setFolderPayload({ action: "delete", id: data.node.id, text: data.node.text });
        // Backend: postJson("/api/Folder/Delete", { id: data.node.id })
      })
      .on("move_node.jstree.folder", function (e, data) {
        var payload = { action: "move", id: data.node.id, parentId: data.parent, position: data.position };
        setFolderPayload(payload);
        postJson("/api/Folder/Move", payload).catch(function () { });
      });

    // Menü butonu (sağ tık çalışmayan cihazlar için)
    $("#folderMenuBtn").off(".folderBtn").on("click.folderBtn", function () {
      var tree = $("#treeCategoriesDnd").jstree(true);
      if (!tree || typeof tree.show_contextmenu !== "function") return;

      var sel = tree.get_selected();
      var nodeId = (sel && sel.length) ? sel[0] : "cat-root";

      var $btn = $(this);
      var off = $btn.offset();
      if (!off) return;

      tree.deselect_all(true);
      tree.select_node(nodeId);
      tree.show_contextmenu(nodeId, off.left, off.top + $btn.outerHeight());
    });

    $("#folderCreateBtn").off(".folderBtn").on("click.folderBtn", function () {
      var tree = $("#treeCategoriesDnd").jstree(true);
      if (!tree) return;
      var sel = tree.get_selected();
      var parentId = (sel && sel.length) ? sel[0] : "cat-root";
      var id = "tmp-" + Date.now();
      var newNode = tree.create_node(parentId, { id: id, text: "Yeni Klasör" }, "last");
      if (!newNode) return setFolderPayload({ action: "error", message: "Yeni düğüm oluşturulamadı." });
      tree.deselect_all(true);
      tree.select_node(newNode);
      tree.edit(newNode);
    });

    $("#folderRenameBtn").off(".folderBtn").on("click.folderBtn", function () {
      var tree = $("#treeCategoriesDnd").jstree(true);
      if (!tree) return;
      var sel = tree.get_selected();
      if (!sel || !sel.length) return setFolderPayload({ action: "error", message: "Önce bir düğüm seçin." });
      tree.edit(sel[0]);
    });

    $("#folderDeleteBtn").off(".folderBtn").on("click.folderBtn", function () {
      var tree = $("#treeCategoriesDnd").jstree(true);
      if (!tree) return;
      var sel = tree.get_selected();
      if (!sel || !sel.length) return setFolderPayload({ action: "error", message: "Önce bir düğüm seçin." });
      tree.delete_node(sel[0]);
    });

    // =========================================================
    // 4) Searchable Tree (performanslı; open_all sadece mode enter)
    // =========================================================
    var searchableNodes = [
      {
        id: "s-root",
        text: "Kategoriler",
        state: { opened: true },
        children: [
          { id: "s-phone", text: "Telefon" },
          { id: "s-laptop", text: "Laptop" },
          {
            id: "s-accessories",
            text: "Aksesuarlar",
            state: { opened: true },
            children: [
              { id: "s-charger", text: "Şarj Aleti" },
              { id: "s-case", text: "Kılıf" }
            ]
          },
          { id: "s-books", text: "Kitap" }
        ]
      }
    ];

    $("#treeSearchable").jstree({
      core: { data: searchableNodes },
      search: { show_only_matches: true, show_only_matches_children: true, case_sensitive: false },
      plugins: ["search", "wholerow"]
    });

    var searchMode = false;
    $("#treeSearchInput").off(".search").on("keyup.search", debounce(function () {
      var q = String($(this).val() || "").trim();
      var tree = $("#treeSearchable").jstree(true);
      if (!tree) return;

      if (!q) {
        tree.clear_search();
        if (searchMode) {
          tree.close_all();
          tree.open_node("s-root");
        }
        searchMode = false;
        return;
      }

      if (!searchMode) {
        tree.open_all();
        searchMode = true;
      }

      tree.search(q);
    }, 250));

    // =========================================================
    // 5) Ajax / Lazy Tree (gerçek lazy: root users, child todos)
    // =========================================================
    $("#treeAjaxLazy").jstree({
      core: {
        check_callback: false,
        data: function (node, cb) {
          var root = "#";
          if (node.id === root) {
            var url = "https://jsonplaceholder.typicode.com/users";
            $("#ajaxRequestPreview").text(url);
            fetch(url)
              .then(function (r) { return r.json(); })
              .then(function (users) {
                cb((users || []).map(function (u) {
                  return { id: "user-" + u.id, text: u.name, children: true };
                }));
              })
              .catch(function () { cb([]); });
            return;
          }

          if (String(node.id).indexOf("user-") === 0) {
            var userId = String(node.id).replace("user-", "");
            var url2 = "https://jsonplaceholder.typicode.com/todos?userId=" + encodeURIComponent(userId);
            $("#ajaxRequestPreview").text(url2);
            fetch(url2)
              .then(function (r) { return r.json(); })
              .then(function (todos) {
                cb((todos || []).map(function (t) {
                  return { id: "todo-" + t.id, text: t.title, children: false };
                }));
              })
              .catch(function () { cb([]); });
            return;
          }

          cb([]);
        }
      },
      plugins: ["wholerow"]
    });

    // =========================================================
    // 6) Iconless Tree
    // =========================================================
    $("#treeIconless").jstree({
      core: { data: categoryNodes, themes: { icons: false } },
      plugins: ["wholerow"]
    });
    });
})(window.jQuery);
