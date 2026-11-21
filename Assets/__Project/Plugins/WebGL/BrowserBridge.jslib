mergeInto(LibraryManager.library, {
  BrowserBridge_Copy: function (textPtr) {
    var text = UTF8ToString(textPtr);

    var fallback = function () {
      var area = document.createElement('textarea');
      area.value = text;
      area.style.position = 'fixed';
      area.style.opacity = '0';
      document.body.appendChild(area);
      area.select();
      try { document.execCommand('copy'); } catch (e) {}
      area.remove();
    };

    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).catch(fallback);
    } else {
      fallback();
    }
  },

  BrowserBridge_UserAgent: function () {
    return stringToNewUTF8(navigator.userAgent);
  }
});
