window.Symplify = window.Symplify || {};

window.Symplify.TinyMce = (function ($) {
    'use strict';

    const defaults = {
        selector: '[data-symplify-editor]',
        scriptUrl: '/lib/tinymce/tinymce.min.js',
        baseUrl: '/lib/tinymce',
        profile: 'content',
        height: 360,
        menubar: false,
        branding: false,
        promotion: false,
        licenseKey: 'gpl',
        plugins: 'lists link table code fullscreen preview wordcount',
        toolbar: 'undo redo | blocks | bold italic underline | alignleft aligncenter alignright alignjustify | bullist numlist | link table | removeformat | code fullscreen',
        toolbarMode: 'wrap',
        blockFormats: 'Paragraf=p; Başlık 2=h2; Başlık 3=h3; Başlık 4=h4',
        contentStyle: 'body { font-family: Inter, Arial, sans-serif; font-size: 14px; line-height: 1.65; }',
        validElements: '*[*]',
        extendedValidElements: 'span[class|style],i[class],iconify-icon[class|icon],img[src|alt|title|width|height|class|style],a[href|target|rel|title|class|style]',
        liveValidation: true,
        validationDebounce: 500,
        assetUploadEnabled: false,
        assetUploadUrl: '',
        assetUploadButtonText: 'Dosya Yükle',
        assetUploadUploadingText: 'Dosya yükleniyor...',
        assetUploadSuccessText: 'Dosya yüklendi ve bağlantı hazırlandı.',
        assetUploadErrorText: 'Dosya yüklenemedi.',
        assetUploadInvalidFileText: 'Bu dosya türüne izin verilmiyor.',
        assetUploadFileTooLargeText: 'Dosya boyutu çok büyük.',
        assetUploadMaxSizeMb: 25,
        assetUploadAccept: '.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.jpg,.jpeg,.png,.webp',
        assetUploadAllowedExtensions: '.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.jpg,.jpeg,.png,.webp',
        assetUploadCongressId: ''
    };

    let loaderPromise = null;

    function initAll(container, options) {
        const $container = normalizeContainer(container);
        const settings = $.extend({}, defaults, options || {});
        const $editors = $container.find(settings.selector).addBack(settings.selector);

        if (!$editors.length) return Promise.resolve([]);

        return ensureTinyMce(settings)
            .then(function () {
                const instances = [];
                $editors.each(function () {
                    instances.push(init(this, settings));
                });
                return Promise.all(instances);
            })
            .catch(function (error) {
                if (window.console) window.console.warn('Symplify TinyMCE init failed.', error);
                return [];
            });
    }

    function init(element, options) {
        const textarea = element instanceof HTMLElement ? element : $(element).get(0);
        if (!textarea || textarea.tagName.toLowerCase() !== 'textarea') return Promise.resolve(null);

        const settings = buildSettings($(textarea), options);

        return ensureTinyMce(settings)
            .then(function () {
                const existing = getEditor(textarea);
                if (existing) existing.remove();

                ensureId(textarea);
                return tinymce.init(buildTinyMceOptions(textarea, settings))
                    .then(function (editors) {
                        return editors && editors.length ? editors[0] : null;
                    });
            });
    }

    function syncAll(container) {
        const $container = normalizeContainer(container);
        if (!window.tinymce || !tinymce.editors) return;

        $container.find(defaults.selector).addBack(defaults.selector).each(function () {
            const editor = getEditor(this);
            if (editor) editor.save();
        });
    }

    function destroy(container) {
        const $container = normalizeContainer(container);
        if (!window.tinymce || !tinymce.editors) return;

        $container.find(defaults.selector).addBack(defaults.selector).each(function () {
            const editor = getEditor(this);
            if (editor) editor.remove();
        });
    }

    function focusByName(name) {
        if (!window.tinymce || !tinymce.editors || !name) return false;

        const textarea = document.querySelector('textarea[name="' + escapeSelector(name) + '"]');
        if (!textarea) return false;

        const editor = getEditor(textarea);
        if (!editor) return false;

        editor.focus();
        return true;
    }

    function ensureTinyMce(settings) {
        if (window.tinymce) return Promise.resolve(window.tinymce);
        if (loaderPromise) return loaderPromise;

        loaderPromise = new Promise(function (resolve, reject) {
            const existingScript = document.querySelector('script[data-symplify-tinymce-loader="true"]');
            if (existingScript) {
                existingScript.addEventListener('load', function () { resolve(window.tinymce); });
                existingScript.addEventListener('error', reject);
                return;
            }

            const script = document.createElement('script');
            script.src = settings.scriptUrl;
            script.async = true;
            script.defer = true;
            script.setAttribute('data-symplify-tinymce-loader', 'true');
            script.onload = function () { resolve(window.tinymce); };
            script.onerror = function () { reject(new Error('TinyMCE script could not be loaded: ' + settings.scriptUrl)); };
            document.head.appendChild(script);
        });

        return loaderPromise;
    }

    function buildSettings($textarea, options) {
        const settings = $.extend({}, defaults, options || {});
        const data = $textarea.data() || {};

        settings.profile = data.editorProfile || settings.profile;
        settings.height = parsePositiveInt(data.editorHeight) || settings.height;
        settings.menubar = data.editorMenubar !== undefined ? parseBoolean(data.editorMenubar) : settings.menubar;
        settings.plugins = data.editorPlugins || settings.plugins;
        settings.toolbar = data.editorToolbar || settings.toolbar;
        settings.toolbarMode = data.editorToolbarMode || settings.toolbarMode;
        settings.scriptUrl = data.editorScriptUrl || settings.scriptUrl;
        settings.baseUrl = data.editorBaseUrl || settings.baseUrl;
        settings.placeholder = data.editorPlaceholder || $textarea.attr('placeholder') || '';
        settings.liveValidation = data.editorLiveValidation !== undefined
            ? parseBoolean(data.editorLiveValidation)
            : settings.liveValidation;
        settings.validationDebounce = parsePositiveInt(data.editorValidationDebounce) || settings.validationDebounce;

        settings.assetUploadEnabled = data.editorAssetUploadEnabled !== undefined
            ? parseBoolean(data.editorAssetUploadEnabled)
            : settings.assetUploadEnabled;
        settings.assetUploadUrl = data.editorAssetUploadUrl || settings.assetUploadUrl;
        settings.assetUploadButtonText = data.editorAssetUploadButtonText || settings.assetUploadButtonText;
        settings.assetUploadUploadingText = data.editorAssetUploadUploadingText || settings.assetUploadUploadingText;
        settings.assetUploadSuccessText = data.editorAssetUploadSuccessText || settings.assetUploadSuccessText;
        settings.assetUploadErrorText = data.editorAssetUploadErrorText || settings.assetUploadErrorText;
        settings.assetUploadInvalidFileText = data.editorAssetUploadInvalidFileText || settings.assetUploadInvalidFileText;
        settings.assetUploadFileTooLargeText = data.editorAssetUploadFileTooLargeText || settings.assetUploadFileTooLargeText;
        settings.assetUploadMaxSizeMb = parsePositiveInt(data.editorAssetUploadMaxSizeMb) || settings.assetUploadMaxSizeMb;
        settings.assetUploadAccept = data.editorAssetUploadAccept || settings.assetUploadAccept;
        settings.assetUploadAllowedExtensions = data.editorAssetUploadAllowedExtensions || settings.assetUploadAllowedExtensions;
        settings.assetUploadCongressId = data.editorAssetCongressId || data.editorAssetUploadCongressId || settings.assetUploadCongressId;
        settings.assetUploadAllowedExtensionList = parseAllowedExtensions(settings.assetUploadAllowedExtensions || settings.assetUploadAccept);

        if (settings.profile === 'simple') {
            settings.plugins = 'lists link code wordcount';
            settings.toolbar = 'undo redo | bold italic underline | bullist numlist | link | removeformat | code';
            settings.height = parsePositiveInt(data.editorHeight) || 260;
        }

        if (settings.assetUploadEnabled) {
            settings.toolbar = ensureToolbarButton(settings.toolbar, 'assetupload', 'link');
        }

        return settings;
    }

    function buildTinyMceOptions(textarea, settings) {
        return {
            target: textarea,
            base_url: settings.baseUrl,
            suffix: '.min',
            license_key: settings.licenseKey,
            height: settings.height,
            menubar: settings.menubar,
            branding: settings.branding,
            promotion: settings.promotion,
            plugins: settings.plugins,
            toolbar: settings.toolbar,
            toolbar_mode: settings.toolbarMode,
            block_formats: settings.blockFormats,
            content_style: settings.contentStyle,
            placeholder: settings.placeholder,
            convert_urls: false,
            relative_urls: false,
            remove_script_host: false,
            valid_elements: settings.validElements,
            extended_valid_elements: settings.extendedValidElements,
            setup: function (editor) {
                if (settings.assetUploadEnabled) {
                    registerAssetUploadButton(editor, textarea, settings);
                }

                const saveOnlyDebounced = debounce(function () {
                    editor.save();
                }, settings.validationDebounce);

                const saveAndValidateDebounced = debounce(function () {
                    editor.save();
                    if (settings.liveValidation) triggerValidation(textarea);
                }, settings.validationDebounce);

                editor.on('keyup', saveOnlyDebounced);
                editor.on('change undo redo setcontent', saveAndValidateDebounced);
                editor.on('blur', function () {
                    editor.save();
                    triggerValidation(textarea);
                });
            },
            init_instance_callback: function (editor) {
                editor.save();
            }
        };
    }

    function registerAssetUploadButton(editor, textarea, settings) {
        const buttonText = normalizeButtonText(settings.assetUploadButtonText, defaults.assetUploadButtonText);

        editor.ui.registry.addIcon('symplify-upload', '<svg width="24" height="24" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3l4.5 4.5-1.4 1.4-2.1-2.1V15h-2V6.8L8.9 8.9 7.5 7.5 12 3z"></path><path d="M5 14h2v4h10v-4h2v6H5v-6z"></path></svg>');

        editor.ui.registry.addButton('assetupload', {
            icon: 'symplify-upload',
            text: buttonText,
            tooltip: buttonText,
            onAction: function () {
                openAssetFilePicker(editor, textarea, settings);
            }
        });
    }

    function openAssetFilePicker(editor, textarea, settings) {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = settings.assetUploadAccept || defaults.assetUploadAccept;
        input.style.display = 'none';

        input.addEventListener('change', function () {
            const file = input.files && input.files.length ? input.files[0] : null;
            if (!file) {
                cleanupFileInput(input);
                return;
            }

            const validationMessage = validateAssetFile(file, settings);
            if (validationMessage) {
                notify(editor, validationMessage, 'error');
                cleanupFileInput(input);
                return;
            }

            uploadAsset(editor, textarea, settings, file)
                .always(function () {
                    cleanupFileInput(input);
                });
        });

        document.body.appendChild(input);
        input.click();
    }

    function uploadAsset(editor, textarea, settings, file) {
        const uploadUrl = settings.assetUploadUrl;

        if (!uploadUrl) {
            notify(editor, settings.assetUploadErrorText || defaults.assetUploadErrorText, 'error');
            return $.Deferred().reject().promise();
        }

        const congressId = resolveAssetCongressId(textarea, settings);
        const token = getAntiForgeryToken(textarea);
        const formData = new FormData();

        formData.append('File', file, file.name);

        if (congressId) {
            formData.append('CongressId', congressId);
        }

        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const headers = {};
        if (token) {
            headers.RequestVerificationToken = token;
        }

        const uploadingNotification = notify(
            editor,
            settings.assetUploadUploadingText || defaults.assetUploadUploadingText,
            'info',
            0);

        editor.setProgressState(true);

        return $.ajax({
            url: uploadUrl,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: headers
        })
            .done(function (response) {
                if (!response || response.success === false || !response.url) {
                    notify(editor, normalizeUploadError(response, settings), 'error');
                    return;
                }

                insertUploadedAsset(editor, response, file);
                notify(editor, response.message || settings.assetUploadSuccessText || defaults.assetUploadSuccessText, 'success');
            })
            .fail(function (xhr) {
                notify(editor, normalizeUploadError(xhr, settings), 'error');
            })
            .always(function () {
                if (uploadingNotification && typeof uploadingNotification.close === 'function') {
                    uploadingNotification.close();
                }

                editor.setProgressState(false);
            });
    }

    function insertUploadedAsset(editor, response, file) {
        const url = response.url;
        const fileName = response.fileName || file.name;
        const contentType = (response.contentType || file.type || '').toLowerCase();
        const extension = normalizeExtension(response.fileExtension || getFileExtension(fileName));

        if (isImageAsset(contentType, extension)) {
            editor.insertContent(
                '<p><img src="' + escapeHtmlAttribute(url) + '" alt="' + escapeHtmlAttribute(fileName) + '" style="max-width:100%;height:auto;" /></p>');
        } else {
            editor.insertContent(
                '<p><a href="' + escapeHtmlAttribute(url) + '" target="_blank" rel="noopener noreferrer">' + escapeHtml(fileName) + '</a></p>');
        }

        editor.save();
    }

    function validateAssetFile(file, settings) {
        const extension = normalizeExtension(getFileExtension(file.name));
        const allowedExtensions = settings.assetUploadAllowedExtensionList || [];

        if (allowedExtensions.length && allowedExtensions.indexOf(extension) < 0) {
            return settings.assetUploadInvalidFileText || defaults.assetUploadInvalidFileText;
        }

        const maxSizeMb = settings.assetUploadMaxSizeMb || defaults.assetUploadMaxSizeMb;
        const maxSizeBytes = maxSizeMb * 1024 * 1024;

        if (file.size > maxSizeBytes) {
            return settings.assetUploadFileTooLargeText || defaults.assetUploadFileTooLargeText;
        }

        return null;
    }

    function resolveAssetCongressId(textarea, settings) {
        if (settings.assetUploadCongressId) {
            return settings.assetUploadCongressId;
        }

        const $form = $(textarea).closest('form');
        const formCongressId = $form.find('input[name="Id"], input[name="CongressId"]').first().val();

        return formCongressId || '';
    }

    function getAntiForgeryToken(textarea) {
        if (window.Symplify && window.Symplify.Ajax && typeof window.Symplify.Ajax.getAntiForgeryToken === 'function') {
            const token = window.Symplify.Ajax.getAntiForgeryToken($(textarea).closest('form'));
            if (token) return token;
        }

        const $form = $(textarea).closest('form');
        return $form.find('input[name="__RequestVerificationToken"]').first().val()
            || $('input[name="__RequestVerificationToken"]').first().val()
            || '';
    }

    function normalizeUploadError(response, settings) {
        const payload = response && response.responseJSON ? response.responseJSON : response;
        const message = payload && payload.message ? payload.message : null;

        return message || settings.assetUploadErrorText || defaults.assetUploadErrorText;
    }

    function notify(editor, message, type, timeout) {
        if (editor && editor.notificationManager && typeof editor.notificationManager.open === 'function') {
            return editor.notificationManager.open({
                text: message,
                type: type || 'info',
                timeout: timeout === undefined ? 3000 : timeout
            });
        }

        if (window.Symplify && window.Symplify.Toast && typeof window.Symplify.Toast.show === 'function') {
            window.Symplify.Toast.show(message, type || 'info');
        }

        return null;
    }

    function cleanupFileInput(input) {
        window.setTimeout(function () {
            if (input && input.parentNode) {
                input.parentNode.removeChild(input);
            }
        }, 0);
    }

    function parseAllowedExtensions(value) {
        if (!value) return [];

        return String(value)
            .split(',')
            .map(function (item) { return normalizeExtension(item); })
            .filter(Boolean);
    }

    function ensureToolbarButton(toolbar, buttonName, insertAfterButtonName) {
        const value = toolbar || '';
        const tokens = value.split(/(\s+|\|)/);

        if (value.split(/\s+/).indexOf(buttonName) >= 0) {
            return value;
        }

        if (!insertAfterButtonName) {
            return value ? value + ' | ' + buttonName : buttonName;
        }

        for (let index = 0; index < tokens.length; index += 1) {
            if (tokens[index] === insertAfterButtonName) {
                tokens.splice(index + 1, 0, ' ', buttonName);
                return tokens.join('');
            }
        }

        return value ? value + ' | ' + buttonName : buttonName;
    }

    function normalizeButtonText(value, fallback) {
        const text = String(value || '').trim();

        if (!text || text.indexOf('.') >= 0) {
            return fallback || 'Dosya Yükle';
        }

        return text;
    }

    function isImageAsset(contentType, extension) {
        return contentType.indexOf('image/') === 0 || ['.jpg', '.jpeg', '.png', '.webp'].indexOf(extension) >= 0;
    }

    function getFileExtension(fileName) {
        const name = fileName || '';
        const index = name.lastIndexOf('.');

        return index >= 0 ? name.slice(index) : '';
    }

    function normalizeExtension(value) {
        const extension = String(value || '').trim().toLowerCase();
        if (!extension) return '';
        return extension.charAt(0) === '.' ? extension : '.' + extension;
    }

    function triggerValidation(textarea) {
        const $textarea = $(textarea);
        const $form = $textarea.closest('form');

        if ($form.length && $form.data('validator')) {
            $textarea.valid();
        }
    }

    function debounce(callback, delay) {
        let timer = null;

        return function () {
            const context = this;
            const args = arguments;
            window.clearTimeout(timer);
            timer = window.setTimeout(function () {
                callback.apply(context, args);
            }, delay);
        };
    }

    function getEditor(textarea) {
        if (!window.tinymce || !textarea) return null;
        ensureId(textarea);
        return tinymce.get(textarea.id);
    }

    function ensureId(element) {
        if (!element.id) element.id = 'symplify-editor-' + Math.random().toString(36).slice(2);
        return element.id;
    }

    function normalizeContainer(container) {
        if (!container) return $(document);
        return container.jquery ? container : $(container);
    }

    function parsePositiveInt(value) {
        const parsed = parseInt(value, 10);
        return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
    }

    function parseBoolean(value) {
        if (typeof value === 'boolean') return value;
        const text = String(value).toLowerCase();
        return text === 'true' || text === '1' || text === 'yes';
    }

    function escapeSelector(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|/@])/g, '\\$1');
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function escapeHtmlAttribute(value) {
        return escapeHtml(value).replace(/`/g, '&#96;');
    }

    return {
        init: init,
        initAll: initAll,
        syncAll: syncAll,
        destroy: destroy,
        focusByName: focusByName
    };
})(jQuery);

$(function () {
    if (window.Symplify.TinyMce) window.Symplify.TinyMce.initAll(document);
});
