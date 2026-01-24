let quillInstance = null;

window.initQuillEditor = function(initialContent) {
    try {
        // Wait a bit for DOM to be ready
        setTimeout(() => {
            const container = document.querySelector('#quill-editor');

            if (!container) {
                console.error('Quill editor container not found');
                return false;
            }

            // Destroy existing instance if any
            if (quillInstance) {
                container.innerHTML = '';
                quillInstance = null;
            }

            // Initialize Quill
            quillInstance = new Quill('#quill-editor', {
                theme: 'snow',
                modules: {
                    toolbar: [
                        [{ 'header': [1, 2, 3, false] }],
                        ['bold', 'italic', 'underline', 'strike'],
                        [{ 'list': 'ordered'}, { 'list': 'bullet' }],
                        [{ 'color': [] }, { 'background': [] }],
                        [{ 'align': [] }],
                        ['link'],
                        ['clean']
                    ]
                },
                placeholder: 'Write your journal entry here...'
            });

            // Set initial content
            if (initialContent) {
                quillInstance.root.innerHTML = initialContent;
            }

            console.log('Quill editor initialized successfully');
        }, 50);

        return true;
    } catch (error) {
        console.error('Error initializing Quill:', error);
        return false;
    }
}

window.getQuillContent = function() {
    if (!quillInstance) {
        console.warn('Quill instance not found');
        return '';
    }
    return quillInstance.root.innerHTML;
}

window.destroyQuillEditor = function() {
    if (quillInstance) {
        const container = document.querySelector('#quill-editor');
        if (container) {
            container.innerHTML = '';
        }
        quillInstance = null;
        console.log('Quill editor destroyed');
    }
}