(() => {
    const form = document.querySelector('[data-survey-form]');
    if (!form) return;

    const questionSections = [...form.querySelectorAll('[data-required="True"], [data-required="true"]')];
    const hasValue = section => [...section.querySelectorAll('input, textarea')]
        .some(input => (input.type === 'checkbox' || input.type === 'radio') ? input.checked : input.value.trim() !== '');

    const clearError = section => {
        section.classList.remove('border-danger');
        const error = section.querySelector('.question-error');
        if (error) error.textContent = '';
    };

    form.addEventListener('input', event => {
        const section = event.target.closest('[data-required]');
        if (section) clearError(section);
    });

    form.addEventListener('change', event => {
        const section = event.target.closest('[data-required]');
        if (section) clearError(section);
    });

    form.addEventListener('submit', event => {
        let firstInvalid = null;
        questionSections.forEach(section => {
            clearError(section);
            if (!hasValue(section)) {
                section.classList.add('border-danger');
                const error = section.querySelector('.question-error');
                if (error) error.textContent = 'Vui lòng trả lời câu hỏi này.';
                firstInvalid ||= section;
            }
        });
        if (firstInvalid) {
            event.preventDefault();
            firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });
})();
