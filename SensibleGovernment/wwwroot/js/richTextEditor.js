// Create new file: wwwroot/js/richTextEditor.js

export function initializeTinyMCE(editorId, dotNetRef, height, placeholder, showMenuBar) {
    // Check if TinyMCE script is loaded
    if (typeof tinymce === 'undefined') {
        // Load TinyMCE from CDN
        const script = document.createElement('script');
        script.src = 'https://cdn.tiny.cloud/1/no-api-key/tinymce/6/tinymce.min.js';
        script.referrerpolicy = 'origin';
        script.onload = () => {
            initEditor(editorId, dotNetRef, height, placeholder, showMenuBar);
        };
        document.head.appendChild(script);
    } else {
        initEditor(editorId, dotNetRef, height, placeholder, showMenuBar);
    }
}

function initEditor(editorId, dotNetRef, height, placeholder, showMenuBar) {
    tinymce.init({
        selector: `#${editorId}`,
        height: height,
        menubar: showMenuBar,
        placeholder: placeholder,
        plugins: [
            'advlist', 'autolink', 'lists', 'link', 'image', 'charmap', 'preview',
            'anchor', 'searchreplace', 'visualblocks', 'code', 'fullscreen',
            'insertdatetime', 'media', 'table', 'help', 'wordcount', 'emoticons',
            'codesample', 'quickbars'
        ],
        toolbar: 'undo redo | blocks | ' +
            'bold italic underline strikethrough | ' +
            'alignleft aligncenter alignright alignjustify | ' +
            'bullist numlist outdent indent | ' +
            'link image media | ' +
            'forecolor backcolor emoticons | ' +
            'removeformat | help',
        quickbars_selection_toolbar: 'bold italic | quicklink h2 h3 blockquote',
        quickbars_insert_toolbar: 'quickimage quicktable',
        contextmenu: 'link image table',
        content_style: 'body { font-family:Helvetica,Arial,sans-serif; font-size:14px }',

        // Image upload handler (we'll use base64 for now)
        images_upload_handler: (blobInfo, progress) => new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => resolve(reader.result);
            reader.onerror = reject;
            reader.readAsDataURL(blobInfo.blob());
        }),

        // Update Blazor component on change
        setup: function (editor) {
            editor.on('change keyup', function () {
                const content = editor.getContent();
                dotNetRef.invokeMethodAsync('UpdateContent', content);
            });
        }
    });
}

export function destroyTinyMCE(editorId) {
    const editor = tinymce.get(editorId);
    if (editor) {
        editor.destroy();
    }
}

export function getContent(editorId) {
    const editor = tinymce.get(editorId);
    return editor ? editor.getContent() : '';
}

export function setContent(editorId, content) {
    const editor = tinymce.get(editorId);
    if (editor) {
        editor.setContent(content);
    }
}