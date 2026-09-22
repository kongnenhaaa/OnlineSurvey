(() => {
    const container = document.getElementById('questionBuilder');
    const hidden = document.getElementById('QuestionsJson');
    const initial = document.getElementById('initialQuestions');
    if (!container || !hidden || !initial) return;

    const types = {
        text: 'Trả lời ngắn',
        textarea: 'Trả lời dài',
        single_choice: 'Chọn một đáp án',
        multiple_choice: 'Chọn nhiều đáp án'
    };

    let questions = [];
    try { questions = JSON.parse(initial.textContent || '[]'); } catch { questions = []; }

    const createId = () => window.crypto?.randomUUID
        ? window.crypto.randomUUID().replaceAll('-', '')
        : `${Date.now()}${Math.random().toString(16).slice(2)}`;
    const escapeHtml = value => String(value ?? '').replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[character]));

    function normalize() {
        questions.forEach((question, index) => {
            question.id ||= createId();
            question.order = index + 1;
            question.type ||= 'text';
            question.text ||= '';
            question.options ||= [];
        });
    }

    function sync() {
        normalize();
        hidden.value = JSON.stringify(questions);
        const countText = `${questions.length} câu hỏi`;
        document.getElementById('builderCount')?.replaceChildren(document.createTextNode(countText));
        document.getElementById('previewCount')?.replaceChildren(document.createTextNode(countText));
        const title = document.getElementById('Title');
        const previewTitle = document.getElementById('previewTitle');
        if (title && previewTitle) previewTitle.textContent = title.value.trim() || 'Tên khảo sát';
        renderPreview();
    }

    function renderPreview() {
        const preview = document.getElementById('previewQuestions');
        const empty = document.getElementById('previewEmpty');
        if (!preview) return;
        preview.querySelectorAll('.preview-question').forEach(element => element.remove());
        if (empty) empty.classList.toggle('d-none', questions.length > 0);
        questions.slice(0, 8).forEach((question, index) => {
            const item = document.createElement('div');
            item.className = 'preview-question';
            item.innerHTML = `<strong>${index + 1}. ${escapeHtml(question.text || 'Chưa nhập nội dung')}</strong><small>${types[question.type] || 'Câu hỏi'}</small>`;
            preview.appendChild(item);
        });
        if (questions.length > 8) {
            const more = document.createElement('div');
            more.className = 'small text-secondary mt-2';
            more.textContent = `+ ${questions.length - 8} câu hỏi khác`;
            preview.appendChild(more);
        }
    }

    function addQuestion(type = 'text') {
        const question = { id: createId(), order: questions.length + 1, type, text: '', required: false, options: [] };
        if (type === 'single_choice' || type === 'multiple_choice') {
            question.options = [{ id: createId(), text: '' }, { id: createId(), text: '' }];
        }
        questions.push(question);
        render();
        const cards = container.querySelectorAll('.question-card');
        cards[cards.length - 1]?.querySelector('.question-text')?.focus();
    }

    function render() {
        normalize();
        container.innerHTML = '';
        questions.forEach((question, index) => {
            const card = document.createElement('div');
            card.className = 'card mb-3 question-card';
            card.dataset.index = index;
            const typeOptions = Object.entries(types).map(([key, label]) => `<option value="${key}" ${question.type === key ? 'selected' : ''}>${label}</option>`).join('');
            card.innerHTML = `
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-center mb-3 gap-2">
                        <div class="d-flex align-items-center gap-2"><span class="question-number">${index + 1}</span><h3 class="h6 mb-0">Câu hỏi</h3></div>
                        <div class="builder-actions">
                            <button type="button" class="btn btn-sm btn-light move-question" data-direction="up" title="Đưa lên" ${index === 0 ? 'disabled' : ''}>↑</button>
                            <button type="button" class="btn btn-sm btn-light move-question" data-direction="down" title="Đưa xuống" ${index === questions.length - 1 ? 'disabled' : ''}>↓</button>
                            <button type="button" class="btn btn-sm btn-light duplicate-question" title="Nhân bản">⧉</button>
                            <button type="button" class="btn btn-sm btn-outline-danger remove-question" title="Xóa">Xóa</button>
                        </div>
                    </div>
                    <div class="row g-3">
                        <div class="col-md-8"><label class="form-label fw-semibold">Nội dung câu hỏi</label><input class="form-control question-text" value="${escapeHtml(question.text)}" maxlength="500" placeholder="Ví dụ: Bạn đánh giá thế nào?" /></div>
                        <div class="col-md-4"><label class="form-label fw-semibold">Loại câu hỏi</label><select class="form-select question-type">${typeOptions}</select></div>
                        <div class="col-12"><div class="form-check"><input class="form-check-input question-required" type="checkbox" ${question.required ? 'checked' : ''} /><label class="form-check-label">Bắt buộc trả lời</label></div></div>
                        <div class="col-12 options-area"></div>
                    </div>
                </div>`;
            container.appendChild(card);
            renderOptions(card, question);
        });
        sync();
    }

    function renderOptions(card, question) {
        const area = card.querySelector('.options-area');
        if (!['single_choice', 'multiple_choice'].includes(question.type)) {
            area.innerHTML = '';
            return;
        }
        question.options ||= [];
        area.innerHTML = `<label class="form-label fw-semibold">Các đáp án</label><div class="options-list"></div><button type="button" class="btn btn-sm btn-outline-secondary add-option">+ Thêm đáp án</button>`;
        const list = area.querySelector('.options-list');
        question.options.forEach((option, optionIndex) => {
            const row = document.createElement('div');
            row.className = 'input-group mb-2 option-row';
            row.innerHTML = `<span class="input-group-text">${optionIndex + 1}</span><input class="form-control option-text" value="${escapeHtml(option.text)}" maxlength="250" placeholder="Nội dung đáp án" /><button type="button" class="btn btn-outline-danger remove-option">×</button>`;
            list.appendChild(row);
        });
    }

    container.addEventListener('input', event => {
        const card = event.target.closest('.question-card');
        if (!card) return;
        const question = questions[Number(card.dataset.index)];
        if (event.target.classList.contains('question-text')) question.text = event.target.value;
        if (event.target.classList.contains('option-text')) {
            const row = event.target.closest('.option-row');
            question.options[Array.from(row.parentElement.children).indexOf(row)].text = event.target.value;
        }
        sync();
    });

    container.addEventListener('change', event => {
        const card = event.target.closest('.question-card');
        if (!card) return;
        const question = questions[Number(card.dataset.index)];
        if (event.target.classList.contains('question-required')) question.required = event.target.checked;
        if (event.target.classList.contains('question-type')) {
            question.type = event.target.value;
            if (['single_choice', 'multiple_choice'].includes(question.type) && question.options.length === 0) {
                question.options = [{ id: createId(), text: '' }, { id: createId(), text: '' }];
            }
            render();
        }
        sync();
    });

    container.addEventListener('click', event => {
        const button = event.target.closest('button');
        const card = event.target.closest('.question-card');
        if (!button || !card) return;
        const index = Number(card.dataset.index);
        if (button.classList.contains('remove-question')) questions.splice(index, 1);
        if (button.classList.contains('duplicate-question')) {
            const copy = JSON.parse(JSON.stringify(questions[index]));
            copy.id = createId();
            copy.options = (copy.options || []).map(option => ({ ...option, id: createId() }));
            questions.splice(index + 1, 0, copy);
        }
        if (button.classList.contains('move-question')) {
            const direction = button.dataset.direction === 'up' ? -1 : 1;
            const target = index + direction;
            if (target >= 0 && target < questions.length) [questions[index], questions[target]] = [questions[target], questions[index]];
        }
        if (button.classList.contains('add-option')) questions[index].options.push({ id: createId(), text: '' });
        if (button.classList.contains('remove-option')) {
            const row = button.closest('.option-row');
            const optionIndex = Array.from(row.parentElement.children).indexOf(row);
            questions[index].options.splice(optionIndex, 1);
        }
        render();
    });

    document.querySelectorAll('[data-add-type]').forEach(button => button.addEventListener('click', () => addQuestion(button.dataset.addType)));
    document.getElementById('addQuestion')?.addEventListener('click', () => addQuestion());
    document.getElementById('Title')?.addEventListener('input', sync);
    document.getElementById('surveyEditorForm')?.addEventListener('submit', event => {
        sync();
        if (questions.length === 0) {
            event.preventDefault();
            alert('Hãy thêm ít nhất một câu hỏi.');
        }
    });

    render();
})();
