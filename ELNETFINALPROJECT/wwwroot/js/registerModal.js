/**
 * registerModal.js
 * Shared registration modal logic.
 * Calls /Home/RegisterPlayer and dispatches 'playerRegistered' with the
 * server-returned player object so any listening page can update its state.
 *
 * NOTE: Players.cshtml has its own inline registerPlayer() that already does
 * this correctly. This file is for other pages (e.g. Dashboard) that embed
 * the _RegisterModal partial and need the same behaviour without duplicating code.
 * Do NOT load this script on Players.cshtml — it defines registerPlayer() inline.
 */
(function () {
    // ── Modal open / close ─────────────────────────────────────────────────────
    window.openRegisterModal = function () {
        const modal = document.getElementById('registerModal');
        if (!modal) return;
        modal.style.display = 'flex';
        setValue('regEmail', '');
        setValue('regUsername', '');
        setValue('regPassword', '');
        setValue('regBalance', '');
        hide('regError');
    };

    window.closeRegisterModal = function () {
        const modal = document.getElementById('registerModal');
        if (modal) modal.style.display = 'none';
    };

    // ── Registration (hits real backend) ──────────────────────────────────────
    window.registerPlayer = async function () {
        const email = (document.getElementById('regEmail')?.value ?? '').trim();
        const username = (document.getElementById('regUsername')?.value ?? '').trim();
        const password = document.getElementById('regPassword')?.value ?? '';
        const balance = parseFloat(document.getElementById('regBalance')?.value) || 0;
        const errEl = document.getElementById('regError');

        // Client-side validation
        if (!username) {
            showError(errEl, 'Username is required.');
            return;
        }
        if (!password || password.length < 6) {
            showError(errEl, 'Password must be at least 6 characters.');
            return;
        }
        hide('regError');

        try {
            const res = await fetch('/Home/RegisterPlayer', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, username, password, balance })
            });

            if (res.ok) {
                const newPlayer = await res.json();
                window.closeRegisterModal();
                // Let listening pages react (e.g. reload their player table)
                window.dispatchEvent(new CustomEvent('playerRegistered', { detail: newPlayer }));
            } else {
                const errData = await res.json().catch(() => ({}));
                showError(errEl, errData.message || 'Error registering player.');
            }
        } catch {
            showError(errEl, 'Network error — please try again.');
        }
    };

    // ── Helpers ────────────────────────────────────────────────────────────────
    function setValue(id, val) {
        const el = document.getElementById(id);
        if (el) el.value = val;
    }
    function hide(id) {
        const el = document.getElementById(id);
        if (el) el.style.display = 'none';
    }
    function showError(el, msg) {
        if (!el) return;
        el.textContent = msg;
        el.style.display = 'block';
    }

    // ── Wire submit button & outside-click close ───────────────────────────────
    document.addEventListener('click', function (e) {
        if (e.target?.id === 'regSubmitBtn') window.registerPlayer();

        const modal = document.getElementById('registerModal');
        if (modal && modal.style.display === 'flex' && e.target === modal) {
            window.closeRegisterModal();
        }
    });
})();
