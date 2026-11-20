mergeInto(LibraryManager.library,{
    FilePicker_Open: function (accept, callback) {
        var input = document.createElement('input');
        input.type = 'file';
        input.accept = UTF8ToString(accept);
        input.style.display = 'none';
        document.body.appendChild(input);

        var done = false;
        var finish = function (bytes, name){
            if (done) return;
            done = true;
            input.remove();

            if (!bytes) {
                {{{ makeDynCall('viii', 'callback') }}}(0, 0, 0);
                return;
            }
            var dataPtr = _malloc(bytes.length);
            HEAPU8.set(bytes, dataPtr);
            var namePtr = stringToNewUTF8(name);
            {{{ makeDynCall('viii', 'callback') }}}(dataPtr, bytes.length, namePtr);
            _free(dataPtr);
            _free(namePtr);
        };

        input.addEventListener('change', function(){
            var file = input.files && input.files[0];
            if (!file) { finish(null, ''); return; }

            var reader = new FileReader();
            reader.onload = function () { finish(new Uint8Array(reader.result), file.name); };
            reader.onerror = function () { finish(null, ''); };
            reader.readAsArrayBuffer(file);
        });
        input.addEventListener('cancel', function () { finish(null, ''); });

        input.click();
    }
});