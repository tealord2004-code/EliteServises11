// Простой и рабочий автокомплит
function initCityAutocomplete(inputSelector) {
    const input = document.querySelector(inputSelector);
    if (!input) return;

    // Создаём контейнер
    const wrapper = document.createElement('div');
    wrapper.style.cssText = 'position:relative;';
    input.parentNode.insertBefore(wrapper, input);
    wrapper.appendChild(input);

    // Создаём выпадающий список
    const dropdown = document.createElement('div');
    dropdown.style.cssText = `
        display:none;
        position:absolute;
        top:100%;
        left:0;
        right:0;
        max-height:250px;
        overflow-y:auto;
        background:white;
        border:1px solid #ccc;
        border-radius:0 0 5px 5px;
        z-index:9999;
        box-shadow:0 4px 8px rgba(0,0,0,0.2);
    `;
    wrapper.appendChild(dropdown);

    let timeout;

    input.addEventListener('input', function () {
        clearTimeout(timeout);
        const val = this.value.trim();
        if (val.length < 1) {
            dropdown.style.display = 'none';
            return;
        }
        timeout = setTimeout(() => {
            fetch('/api/directory/cities?q=' + encodeURIComponent(val))
                .then(r => r.json())
                .then(data => {
                    dropdown.innerHTML = '';
                    if (!data || data.length === 0) {
                        dropdown.innerHTML = '<div style="padding:8px;color:#999;">Ничего не найдено</div>';
                    } else {
                        data.forEach(item => {
                            const div = document.createElement('div');
                            div.style.cssText = 'padding:8px 12px;cursor:pointer;border-bottom:1px solid #eee;font-size:14px;';
                            div.innerHTML = '<strong>' + item.name + '</strong>';
                            if (item.description) {
                                div.innerHTML += ' <small style="color:#999;">' + item.description + '</small>';
                            }
                            div.addEventListener('click', function () {
                                input.value = item.name;
                                dropdown.style.display = 'none';
                            });
                            div.addEventListener('mouseenter', function () {
                                this.style.background = '#f0f0f0';
                            });
                            div.addEventListener('mouseleave', function () {
                                this.style.background = 'white';
                            });
                            dropdown.appendChild(div);
                        });
                    }
                    dropdown.style.display = 'block';
                })
                .catch(err => {
                    console.error('Error:', err);
                });
        }, 300);
    });

    // Скрыть при клике вне
    document.addEventListener('click', function (e) {
        if (!wrapper.contains(e.target)) {
            dropdown.style.display = 'none';
        }
    });
}

function initProfessionAutocomplete(inputSelector) {
    const input = document.querySelector(inputSelector);
    if (!input) return;

    const wrapper = document.createElement('div');
    wrapper.style.cssText = 'position:relative;';
    input.parentNode.insertBefore(wrapper, input);
    wrapper.appendChild(input);

    const dropdown = document.createElement('div');
    dropdown.style.cssText = `
        display:none;
        position:absolute;
        top:100%;
        left:0;
        right:0;
        max-height:250px;
        overflow-y:auto;
        background:white;
        border:1px solid #ccc;
        border-radius:0 0 5px 5px;
        z-index:9999;
        box-shadow:0 4px 8px rgba(0,0,0,0.2);
    `;
    wrapper.appendChild(dropdown);

    let timeout;

    input.addEventListener('input', function () {
        clearTimeout(timeout);
        const val = this.value.trim();
        if (val.length < 1) {
            dropdown.style.display = 'none';
            return;
        }
        timeout = setTimeout(() => {
            fetch('/api/directory/professions?q=' + encodeURIComponent(val))
                .then(r => r.json())
                .then(data => {
                    dropdown.innerHTML = '';
                    if (!data || data.length === 0) {
                        dropdown.innerHTML = '<div style="padding:8px;color:#999;">Можно ввести свою профессию</div>';
                    } else {
                        data.forEach(item => {
                            const div = document.createElement('div');
                            div.style.cssText = 'padding:8px 12px;cursor:pointer;border-bottom:1px solid #eee;font-size:14px;';
                            div.innerHTML = '<strong>' + item.name + '</strong>';
                            if (item.group) {
                                div.innerHTML += ' <small style="color:#999;">(' + item.group + ')</small>';
                            }
                            div.addEventListener('click', function () {
                                input.value = item.name;
                                dropdown.style.display = 'none';
                            });
                            div.addEventListener('mouseenter', function () {
                                this.style.background = '#f0f0f0';
                            });
                            div.addEventListener('mouseleave', function () {
                                this.style.background = 'white';
                            });
                            dropdown.appendChild(div);
                        });
                    }
                    dropdown.style.display = 'block';
                })
                .catch(err => {
                    console.error('Error:', err);
                });
        }, 300);
    });

    document.addEventListener('click', function (e) {
        if (!wrapper.contains(e.target)) {
            dropdown.style.display = 'none';
        }
    });
}