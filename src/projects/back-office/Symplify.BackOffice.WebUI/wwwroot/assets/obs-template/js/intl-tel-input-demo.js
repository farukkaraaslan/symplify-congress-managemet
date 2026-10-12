document.addEventListener("DOMContentLoaded", function () {
      if (!window.intlTelInput) {
        console.error("intl-tel-input yuklenemedi. assets/vendor/intl-tel-input dosyalarini kontrol edin.");
        return;
      }
      var utilsUrl = "/assets/vendor/intl-tel-input/dist/js/utils.js";

      var loadUtils = function () { return import(utilsUrl); };
      var safeInit = function (input, options) {
        if (!input || !window.intlTelInput) return null;
        var baseOptions = {
          loadUtils: loadUtils,
          countryNameLocale: "tr",
          uiTranslations: { searchPlaceholder: "Ülke ara...", zeroSearchResults: "Sonuç bulunamadı" }
        };
        var finalOptions = Object.assign({}, baseOptions, options || {});
        if (options && options.uiTranslations) finalOptions.uiTranslations = Object.assign({}, baseOptions.uiTranslations, options.uiTranslations);
        return window.intlTelInput(input, finalOptions);
      };

      var errorMap = [
        "Geçersiz numara",
        "Geçersiz ülke kodu",
        "Çok kısa",
        "Çok uzun",
        "Numara değil"
      ];

      var demoPhonePreferred = document.querySelector("#demoPhonePreferred");
      safeInit(demoPhonePreferred, { initialCountry: "tr", countryOrder: ["tr", "de", "us", "gb"], separateDialCode: true });

      var demoPhoneNoInitial = document.querySelector("#demoPhoneNoInitial");
      safeInit(demoPhoneNoInitial, { initialCountry: "" });

      var demoFormHidden = document.querySelector("#demoFormHidden");
      var demoPhoneVisible = document.querySelector("#demoPhoneVisible");
      var demoPhoneHidden = document.querySelector("#demoPhoneHidden");
      var demoFormHiddenMsg = document.querySelector("#demoFormHiddenMsg");
      var demoFormHiddenFill = document.querySelector("#demoFormHiddenFill");

      var itiHidden = safeInit(demoPhoneVisible, { initialCountry: "tr" });
      if (itiHidden && demoFormHidden && demoPhoneVisible && demoPhoneHidden && demoFormHiddenMsg) {
        var setHiddenMsg = function (text, colorClass) {
          demoFormHiddenMsg.classList.remove("text-success-600", "text-error-600", "text-secondary-light");
          demoFormHiddenMsg.classList.add(colorClass);
          demoFormHiddenMsg.textContent = text;
        };

        var resetHiddenUI = function () {
          demoPhoneVisible.classList.remove("border-success-600", "border-error-600");
          setHiddenMsg("Submit ile hidden input güncellenir.", "text-secondary-light");
        };

        var validateHidden = function () {
          var value = (demoPhoneVisible.value || "").trim();
          if (!value) return resetHiddenUI();
          try {
            if (!itiHidden.isValidNumber()) {
              demoPhoneVisible.classList.remove("border-success-600");
              demoPhoneVisible.classList.add("border-error-600");
              var code = itiHidden.getValidationError();
              return setHiddenMsg(errorMap[code] || "Geçersiz numara", "text-error-600");
            }
            demoPhoneVisible.classList.remove("border-error-600");
            demoPhoneVisible.classList.add("border-success-600");
            return setHiddenMsg("Geçerli. Kaydet ile E.164 yazılacak.", "text-success-600");
          } catch (e) {
            demoPhoneVisible.classList.remove("border-success-600", "border-error-600");
            return setHiddenMsg("Doğrulama hazırlanıyor...", "text-secondary-light");
          }
        };

        demoPhoneVisible.addEventListener("input", validateHidden);
        demoPhoneVisible.addEventListener("blur", validateHidden);
        demoPhoneVisible.addEventListener("countrychange", validateHidden);
        if (itiHidden.promise && typeof itiHidden.promise.then === "function") itiHidden.promise.then(resetHiddenUI);
        resetHiddenUI();

        demoFormHidden.addEventListener("submit", function (e) {
          e.preventDefault();
          try {
            if (!itiHidden.isValidNumber()) return validateHidden();
            demoPhoneHidden.value = itiHidden.getNumber();
            setHiddenMsg("Hidden input güncellendi: " + demoPhoneHidden.value, "text-success-600");
          } catch (err) {
            setHiddenMsg("E.164 üretimi için utils yükleniyor...", "text-secondary-light");
          }
        });

        if (demoFormHiddenFill) {
          demoFormHiddenFill.addEventListener("click", function () {
            demoPhoneVisible.value = "5551234567";
            validateHidden();
          });
        }
      }

      var demoPhoneExisting = document.querySelector("#demoPhoneExisting");
      var itiExisting = safeInit(demoPhoneExisting, { numberDisplayFormat: "NATIONAL" });
      if (itiExisting && demoPhoneExisting && itiExisting.promise && typeof itiExisting.promise.then === "function") {
        itiExisting.promise.then(function () {
          itiExisting.setNumber(demoPhoneExisting.value);
        });
      }

      var demoPhoneValidate = document.querySelector("#demoPhoneValidate");
      var demoPhoneValidateError = document.querySelector("#demoPhoneValidateError");
      var itiValidate = safeInit(demoPhoneValidate, { initialCountry: "tr" });
      if (itiValidate && demoPhoneValidate && demoPhoneValidateError) {
        var resetValidate = function () {
          demoPhoneValidate.classList.remove("border-success-600", "border-error-600");
          demoPhoneValidateError.classList.remove("text-error-600", "text-success-600");
          demoPhoneValidateError.classList.add("text-secondary-light");
          demoPhoneValidateError.textContent = "Doğrulama bekleniyor.";
        };

        var applyValid = function () {
          demoPhoneValidate.classList.remove("border-error-600");
          demoPhoneValidate.classList.add("border-success-600");
          demoPhoneValidateError.classList.remove("text-secondary-light", "text-error-600");
          demoPhoneValidateError.classList.add("text-success-600");
          demoPhoneValidateError.textContent = "Geçerli numara.";
        };

        var applyError = function (message) {
          demoPhoneValidate.classList.remove("border-success-600");
          demoPhoneValidate.classList.add("border-error-600");
          demoPhoneValidateError.classList.remove("text-secondary-light", "text-success-600");
          demoPhoneValidateError.classList.add("text-error-600");
          demoPhoneValidateError.textContent = message;
        };

        var validate = function () {
          var value = (demoPhoneValidate.value || "").trim();
          if (!value) return resetValidate();
          try {
            if (itiValidate.isValidNumber()) return applyValid();
            var code = itiValidate.getValidationError();
            return applyError(errorMap[code] || "Geçersiz numara");
          } catch (e) {
            return resetValidate();
          }
        };

        demoPhoneValidate.addEventListener("blur", validate);
        demoPhoneValidate.addEventListener("input", validate);
        demoPhoneValidate.addEventListener("countrychange", validate);
        if (itiValidate.promise && typeof itiValidate.promise.then === "function") itiValidate.promise.then(resetValidate);
        resetValidate();
      }

      var demoPhoneGeoIp = document.querySelector("#demoPhoneGeoIp");
      safeInit(demoPhoneGeoIp, {
        initialCountry: "auto",
        geoIpLookup: function (callback) {
          fetch("https://ipapi.co/json")
            .then(function (res) { return res.json(); })
            .then(function (data) { return callback(((data && data.country_code) ? data.country_code : "tr").toLowerCase()); })
            .catch(function () { return callback("tr"); });
        }
      });

      var demoPhoneRestrictedList = document.querySelector("#demoPhoneRestrictedList");
      safeInit(demoPhoneRestrictedList, { initialCountry: "tr", onlyCountries: ["tr", "az"] });

      var demoPhoneMobile = document.querySelector("#demoPhoneMobile");
      var demoPhoneHome = document.querySelector("#demoPhoneHome");
      var demoPhoneWork = document.querySelector("#demoPhoneWork");
      safeInit(demoPhoneMobile, { initialCountry: "tr" });
      safeInit(demoPhoneHome, { initialCountry: "tr" });
      safeInit(demoPhoneWork, { initialCountry: "tr" });
    });
