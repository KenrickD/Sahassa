export function initReleasePage(config) {
    // Toggle Pallets or Items
    document.querySelectorAll(config.toggleBtnSelector).forEach(btn => {
        btn.addEventListener('click', function () {
            const receiveId = this.dataset.receiveId;
            document.querySelectorAll(config.toggleTargets.replace('{id}', receiveId)).forEach(c => {
                c.style.display = c.style.display === 'none' ? config.toggleDisplayStyle : 'none';
            });
        });
    });

    document.querySelectorAll('.receive-checkbox').forEach(cb => {
        cb.addEventListener('change', function () {
            const id = this.dataset.receiveId;
            const enabled = this.checked;

            const dropdown = document.querySelector(`.release-type[data-receive-id="${id}"]`);
            if (dropdown) dropdown.disabled = !enabled;

            document.querySelectorAll(`.pallet-checkbox[data-receive-id="${id}"]`).forEach(el => {
                if (el.dataset.released !== 'true') el.disabled = !enabled;
            });

            if (config.itemCheckboxSelector) {
                document.querySelectorAll(`.item-checkbox[data-receive-id="${id}"]`).forEach(el => {
                    if (el.dataset.released !== 'true') el.disabled = !enabled;
                });
            }
        });
    });

    document.querySelectorAll('.release-type').forEach(dd => {
        dd.addEventListener('change', function () {
            const id = this.dataset.receiveId;
            if (this.value !== 'Full') return;

            document.querySelectorAll(`.pallet-checkbox[data-receive-id="${id}"]`).forEach(cb => {
                if (!cb.disabled && cb.dataset.released !== 'true') cb.checked = true;
            });

            if (config.itemCheckboxSelector) {
                document.querySelectorAll(`.item-checkbox[data-receive-id="${id}"]`).forEach(cb => {
                    if (!cb.disabled && cb.dataset.released !== 'true') cb.checked = true;
                });
            }
        });
    });

    // Optional item-to-pallet sync
    if (config.itemCheckboxSelector) {
        document.querySelectorAll('.pallet-checkbox').forEach(palletCb => {
            palletCb.addEventListener('change', function () {
                const pid = this.dataset.palletId;
                const rid = this.dataset.receiveId;
                const state = this.checked;

                document.querySelectorAll(`.item-checkbox[data-receive-id="${rid}"][data-pallet-id="${pid}"]`).forEach(cb => {
                    if (!cb.disabled && cb.dataset.released !== 'true') cb.checked = state;
                });
            });
        });

        document.querySelectorAll('.item-checkbox').forEach(cb => {
            cb.addEventListener('change', function () {
                const pid = this.dataset.palletId;
                const rid = this.dataset.receiveId;

                const items = document.querySelectorAll(`.item-checkbox[data-receive-id="${rid}"][data-pallet-id="${pid}"]`);
                const allChecked = Array.from(items).every(c => c.checked || c.disabled);

                const palletCb = document.querySelector(`.pallet-checkbox[data-receive-id="${rid}"][data-pallet-id="${pid}"]`);
                if (palletCb && !palletCb.disabled) {
                    palletCb.checked = allChecked;
                }
            });
        });
    }

    // Submit handler
    document.getElementById(config.submitBtnId).addEventListener("click", function (e) {
        e.preventDefault();

        const errorBox = document.getElementById(config.errorContainerId);
        errorBox.style.display = "none";
        errorBox.innerHTML = "";

        const selected = [];
        const receiveIds = new Set();

        document.querySelectorAll(`${config.selectionSelector}:checked:not(:disabled)`).forEach(cb => {
            selected.push(cb.dataset[config.selectionDataKey]);
            receiveIds.add(cb.dataset.receiveId);
        });

        const payload = {
            [config.mainIdKey]: config.mainId,
            [config.selectionPayloadKey]: selected,
            receiveIds: Array.from(receiveIds)
        };

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        fetch(config.submitUrl, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": token,
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify(payload)
        }).then(async res => {
            const json = await res.json().catch(() => ({}));
            if (res.ok && json.success !== false) {
                sessionStorage.setItem("successMessage", json.message || config.successMessage);
                window.location.href = config.redirectUrl;
            } else {
                const msgs = json.errors ? Object.values(json.errors).flat() : [json.message || "Error"];
                showErrors(msgs);
            }
        }).catch(err => showErrors([err.message || "Unknown error"]));

        function showErrors(messages) {
            if (errorBox) {
                errorBox.innerHTML = messages.map(m => `<div>${m}</div>`).join('');
                errorBox.style.display = "block";
            } else {
                alert(messages.join("\n"));
            }
        }
    });
    // Ensure pallet checkbox reflects item checkbox states on page load
    document.querySelectorAll('.pallet-checkbox').forEach(palletCb => {
        const pid = palletCb.dataset.palletId;
        const rid = palletCb.dataset.receiveId;

        const items = document.querySelectorAll(`.item-checkbox[data-receive-id="${rid}"][data-pallet-id="${pid}"]`);
        const allChecked = Array.from(items).every(c => c.checked || c.disabled);

        if (!palletCb.disabled && palletCb.dataset.released !== 'true') {
            palletCb.checked = allChecked;
        }
    });

}
