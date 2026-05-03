(function(){
    // Ensure a global players array exists
    window.players = window.players || [];
    // Create helper functions on window so pages can call them
    window.openRegisterModal = function() {
        const modal = document.getElementById('registerModal');
        if (!modal) return;
        modal.style.display = 'flex';
        document.getElementById('regEmail').value = '';
        document.getElementById('regUsername').value = '';
        document.getElementById('regPassword').value = '';
        document.getElementById('regBalance').value = '';
        document.getElementById('regError').style.display = 'none';
    };
    window.closeRegisterModal = function() {
        const modal = document.getElementById('registerModal');
        if (!modal) return;
        modal.style.display = 'none';
    };

    window.registerPlayer = function() {
        const email = document.getElementById('regEmail').value.trim();
        const username = document.getElementById('regUsername').value.trim();
        const password = document.getElementById('regPassword').value;
        const balance = parseFloat(document.getElementById('regBalance').value) || 0;
        const errEl = document.getElementById('regError');
        if (!username) {
            errEl.textContent = 'Username is required.';
            errEl.style.display = 'block';
            return;
        }
        if (!password || password.length < 6) {
            errEl.textContent = 'Password is required (minimum 6 characters).';
            errEl.style.display = 'block';
            return;
        }
        errEl.style.display = 'none';
        const now = new Date();
        const id = (window._playerIdCounter = (window._playerIdCounter || 1001) + 1);
        const newPlayer = {
            id,
            username,
            email: email || '—',
            // NOTE: frontend demo only — do not store plaintext passwords in production
            password: password,
            balance,
            status: 'Offline',
            registered: now.toLocaleDateString('en-PH', { year:'numeric', month:'short', day:'numeric' })
        };
        window.players.push(newPlayer);
        // notify pages that subscribed
        window.dispatchEvent(new CustomEvent('playerRegistered', { detail: newPlayer }));
        // small UX feedback
        try { window.alert('Player "' + username + '" registered (frontend demo only).'); } catch (e) {}
        window.closeRegisterModal();
    };

    // wire submit button
    document.addEventListener('click', function(e){
        if (e.target && e.target.id === 'regSubmitBtn') {
            window.registerPlayer();
        }
    });

    // close modal on outside click
    document.addEventListener('click', function(e){
        const modal = document.getElementById('registerModal');
        if (modal && modal.style.display === 'flex' && e.target === modal) window.closeRegisterModal();
    });
})();
