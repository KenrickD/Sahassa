// ~/js/pages/raw-material/create-raw-material.js
$(document).ready(function () {
    let receiveIndex = 0;

    // ─── HTML GENERATORS ──────────────────────────────────────────────────────

    function getReceiveHtml(idx) {
        const options = window.packageTypes
            .map(pt => `<option value="${pt.Value}">${pt.Text}</option>`)
            .join('');

        return `
        <div class="receive-block border p-3 mb-4 position-relative" data-receive-index="${idx}">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <h5>Receive #${idx + 1}</h5>
            <button
          type="button"
          class="w-8 h-8 bg-red-600 hover:bg-red-700 text-white
                 rounded-full inline-flex items-center justify-center remove-receive-btn"
          data-receive-index="${idx}"
          title="Remove Receive"
        >
          <iconify-icon icon="mdi:trash-can-outline" width="16" height="16"></iconify-icon>
        </button>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
                  <label>Transport Type</label>
                  <select
                    name="Receives[${idx}].TypeID"
                    class="form-select transport-type-select"
                    data-receive-index="${idx}"
                  >
                <option value="0">Container</option>
                <option value="1">Lorry</option>
              </select>
            </div>

            <div class="container-select-wrapper mb-3"
           data-receive-index="${idx}"
           style="display:none;"></div>

            <div>
              <label>Package Type</label>
              <select name="Receives[${idx}].PackageTypeId" class="form-select">
                <option value="">— select —</option>
                ${options}
              </select>
            </div>

            <div>
              <label>Batch No</label>
              <input type="text"
                     name="Receives[${idx}].BatchNo"
                     class="form-control" />
            </div>
            <div>
              <label>Received Date</label>
              <input type="date"
                     name="Receives[${idx}].ReceivedDate"
                     class="form-control" />
            </div>
            <div>
              <label>Received By</label>
              <input type="text"
                     name="Receives[${idx}].ReceivedBy"
                     class="form-control" />
            </div>
            <div>
              <label>Remarks</label>
              <textarea name="Receives[${idx}].Remarks"
                        class="form-control"></textarea>
            </div>
            <div>
              <label>PO</label>
              <input type="text"
                     name="Receives[${idx}].PO"
                     class="form-control" />
            </div>
          </div>
          <hr class="my-3" />
          <h6>Pallets</h6>
          <div class="pallets-container" data-pallets-for="${idx}"></div>
          <button type="button"
                  class="btn btn-outline-primary mt-3 add-pallet-btn"
                  data-receive-index="${idx}">
            + Add Pallet
          </button>
        </div>`;
    }

    function getPalletHtml(r, p) {
        return `
        <div class="pallet-block border p-3 mb-3 position-relative" data-pallet-index="${p}">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <h6>Pallet #${p + 1}</h6>
            <button
          type="button"
          class="w-8 h-8 bg-red-600 hover:bg-red-700 text-white
                 rounded-full inline-flex items-center justify-center remove-pallet-btn"
          data-receive-index="${r}"
          data-pallet-index="${p}"
          title="Remove Pallet"
        >
          <iconify-icon icon="mdi:trash-can-outline" width="16" height="16"></iconify-icon>
        </button>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label>Pallet Code</label>
              <input type="text"
                     name="Receives[${r}].Pallets[${p}].PalletCode"
                     class="form-control" />
            </div>
            <div>
              <label>Handled By</label>
              <input type="text"
                     name="Receives[${r}].Pallets[${p}].HandledBy"
                     class="form-control" />
            </div>
            <div>
              <label>Pack Size</label>
              <input type="number"
                     name="Receives[${r}].Pallets[${p}].PackSize"
                     class="form-control" />
            </div>
            <div>
              <label>Stored By</label>
              <input type="text"
                     name="Receives[${r}].Pallets[${p}].StoredBy"
                     class="form-control" />
            </div>

            <div>
              <label class="block font-medium mb-1">Group</label>
              <div class="grid grid-cols-3 gap-2">
                <div class="form-check">
                  <input type="checkbox" 
                         class="form-check-input" 
                         id="group3-${r}-${p}" 
                         name="Receives[${r}].Pallets[${p}].Group3" 
                         value="true" />
                  <label class="form-check-label" for="group3-${r}-${p}">3</label>
                </div>
                <div class="form-check">
                  <input type="checkbox" 
                         class="form-check-input" 
                         id="group6-${r}-${p}" 
                         name="Receives[${r}].Pallets[${p}].Group6" 
                         value="true" />
                  <label class="form-check-label" for="group6-${r}-${p}">6</label>
                </div>
                <div class="form-check">
                  <input type="checkbox" 
                         class="form-check-input" 
                         id="group8-${r}-${p}" 
                         name="Receives[${r}].Pallets[${p}].Group8" 
                         value="true" />
                  <label class="form-check-label" for="group8-${r}-${p}">8</label>
                </div>
                <div class="form-check">
                  <input type="checkbox" 
                         class="form-check-input" 
                         id="group9-${r}-${p}" 
                         name="Receives[${r}].Pallets[${p}].Group9" 
                         value="true" />
                  <label class="form-check-label" for="group9-${r}-${p}">9</label>
                </div>
                <div class="form-check">
                  <input type="checkbox" 
                         class="form-check-input" 
                         id="ndg-${r}-${p}" 
                         name="Receives[${r}].Pallets[${p}].NDG" 
                         value="true" />
                  <label class="form-check-label" for="ndg-${r}-${p}">NDG</label>
                </div>
                <div class="form-check">
                  <input type="checkbox" 
                         class="form-check-input" 
                         id="scentaurus-${r}-${p}" 
                         name="Receives[${r}].Pallets[${p}].Scentaurus" 
                         value="true" />
                  <label class="form-check-label" for="scentaurus-${r}-${p}">SCENTAURUS</label>
                </div>
              </div>
            </div>

          </div>
          <div class="mt-3">
            <h6>Items</h6>
            <div class="items-container" data-items-for="${r}-${p}"></div>
            <button type="button"
                    class="btn btn-outline-primary mt-3 add-item-btn"
                    data-receive-index="${r}"
                    data-pallet-index="${p}">
              + Add Item
            </button>
          </div>
          <div class="mt-3">
            <h6>Photos</h6>
            <div class="photos-container" data-photos-for="${r}-${p}"></div>
            <button type="button"
                    class="btn btn-outline-primary mt-3 add-photo-btn"
                    data-receive-index="${r}"
                    data-pallet-index="${p}">
              + Add Photo
            </button>
          </div>
        </div>`;
    }

    function getItemHtml(r, p, i) {
        return `
        <div class="item-block border p-2 mb-2 position-relative">
          <div class="d-flex justify-content-between align-items-center mb-1">
            <strong>Item #${i + 1}</strong>
            <button
          type="button"
          class="w-8 h-8 bg-red-600 hover:bg-red-700 text-white
                 rounded-full inline-flex items-center justify-center remove-item-btn"
          data-receive-index="${r}"
          data-pallet-index="${p}"
          data-item-index="${i}"
          title="Remove Item"
        >
          <iconify-icon icon="mdi:trash-can-outline" width="16" height="16"></iconify-icon>
        </button>
          </div>
          <div class="grid grid-cols-2 gap-2">
            <div>
              <label>Item Code</label>
              <input type="text"
                     name="Receives[${r}].Pallets[${p}].Items[${i}].ItemCode"
                     class="form-control" />
            </div>
            <div>
              <label>Batch No</label>
              <input type="text"
                     name="Receives[${r}].Pallets[${p}].Items[${i}].BatchNo"
                     class="form-control" />
            </div>
            <div>
              <label>Prod Date</label>
              <input type="date"
                     name="Receives[${r}].Pallets[${p}].Items[${i}].ProdDate"
                     class="form-control" />
            </div>
            <div>
              <label>DG</label>
              <input type="hidden"
                     name="Receives[${r}].Pallets[${p}].Items[${i}].DG"
                     value="false" />
              <input type="checkbox"
                     name="Receives[${r}].Pallets[${p}].Items[${i}].DG"
                     value="true"
                     class="form-check-input" />
            </div>
            <div>
              <label>Remarks</label>
              <input type="text"
                     name="Receives[${r}].Pallets[${p}].Items[${i}].Remarks"
                     class="form-control" />
            </div>
          </div>
        </div>`;
    }

    function getPhotoHtml(r, p, ph) {
        return `
        <div class="photo-block border p-2 mb-2 position-relative">
          <div class="d-flex justify-content-between align-items-center mb-1">
            <strong>Photo #${ph + 1}</strong>
            <button
          type="button"
          class="w-8 h-8 bg-red-600 hover:bg-red-700 text-white
                 rounded-full inline-flex items-center justify-center remove-photo-btn"
          data-receive-index="${r}"
          data-pallet-index="${p}"
          data-photo-index="${ph}"
          title="Remove Photo"
        >
          <iconify-icon icon="mdi:trash-can-outline" width="16" height="16"></iconify-icon>
        </button>
          </div>
          <input type="file"
                 name="photoFiles"
                 class="form-control photo-file-input" />
        </div>`;
    }

    // ─── ADD HANDLERS ──────────────────────────────────────────────────────────

    $('#add-receive-btn').on('click', () => {
        $('#receives-container').append(getReceiveHtml(receiveIndex));
        $(`.transport-type-select[data-receive-index="${receiveIndex}"]`)
            .trigger('change');
        receiveIndex++;
    });

    $(document).on('click', '.add-pallet-btn', function () {
        const r = +$(this).data('receive-index');
        const cont = $(`.pallets-container[data-pallets-for="${r}"]`);
        cont.append(getPalletHtml(r, cont.children().length));
    });

    $(document).on('click', '.add-item-btn', function () {
        const r = +$(this).data('receive-index');
        const p = +$(this).data('pallet-index');
        const cont = $(`.items-container[data-items-for="${r}-${p}"]`);
        cont.append(getItemHtml(r, p, cont.children().length));
    });

    $(document).on('click', '.add-photo-btn', function () {
        const r = +$(this).data('receive-index');
        const p = +$(this).data('pallet-index');
        const cont = $(`.photos-container[data-photos-for="${r}-${p}"]`);
        cont.append(getPhotoHtml(r, p, cont.children().length));
    });

    // ─── REINDEX & REMOVE HELPERS ─────────────────────────────────────────────

    function reindexReceives() {
        $('#receives-container .receive-block').each(function (r) {
            const $rcv = $(this);
            $rcv
                .attr('data-receive-index', r)
                .find('h5').text(`Receive #${r + 1}`).end()
                .find('.remove-receive-btn').attr('data-receive-index', r).end()
                .find('.pallets-container').attr('data-pallets-for', r).end()
                .find('.add-pallet-btn').attr('data-receive-index', r);

            // rename all names under this receive
            $rcv.find('[name]').each(function () {
                const old = $(this).attr('name');
                const neu = old.replace(/^Receives\[\d+\]/, `Receives[${r}]`);
                $(this).attr('name', neu);
            });

            reindexPallets(r);
        });
        receiveIndex = $('#receives-container .receive-block').length;
    }

    function reindexPallets(r) {
        const cont = $(`.pallets-container[data-pallets-for="${r}"]`);
        cont.children('.pallet-block').each(function (p) {
            const $pal = $(this);
            $pal
                .attr('data-pallet-index', p)
                .find('h6').first().text(`Pallet #${p + 1}`).end()
                .find('.remove-pallet-btn')
                .attr('data-receive-index', r)
                .attr('data-pallet-index', p).end()
                .find('.items-container').attr('data-items-for', `${r}-${p}`).end()
                .find('.add-item-btn')
                .attr('data-receive-index', r)
                .attr('data-pallet-index', p).end()
                .find('.photos-container').attr('data-photos-for', `${r}-${p}`).end()
                .find('.add-photo-btn')
                .attr('data-receive-index', r)
                .attr('data-pallet-index', p);

            // Update checkbox IDs to maintain unique IDs
            $pal.find('input[type="checkbox"][id^="group"]').each(function () {
                const oldId = $(this).attr('id');
                const newId = oldId.replace(/^(group\d+|ndg|scentaurus)-\d+-\d+/, `$1-${r}-${p}`);
                $(this).attr('id', newId);
                // Update the corresponding label's "for" attribute
                $pal.find(`label[for="${oldId}"]`).attr('for', newId);
            });

            // rename all names under this pallet
            $pal.find('[name]').each(function () {
                const old = $(this).attr('name');
                const neu = old.replace(
                    /Receives\[\d+\]\.Pallets\[\d+\]/,
                    `Receives[${r}].Pallets[${p}]`
                );
                $(this).attr('name', neu);
            });

            reindexItems(r, p);
            reindexPhotos(r, p);
        });
    }

    function reindexItems(r, p) {
        const cont = $(`.items-container[data-items-for="${r}-${p}"]`);
        cont.children('.item-block').each(function (i) {
            const $itm = $(this);
            $itm.find('strong').text(`Item #${i + 1}`);
            $itm.find('.remove-item-btn')
                .attr('data-receive-index', r)
                .attr('data-pallet-index', p)
                .attr('data-item-index', i);

            // rename all names under this item
            $itm.find('[name]').each(function () {
                const old = $(this).attr('name');
                const neu = old.replace(
                    /Receives\[\d+\]\.Pallets\[\d+\]\.Items\[\d+\]/,
                    `Receives[${r}].Pallets[${p}].Items[${i}]`
                );
                $(this).attr('name', neu);
            });
        });
    }

    function reindexPhotos(r, p) {
        const cont = $(`.photos-container[data-photos-for="${r}-${p}"]`);
        cont.children('.photo-block').each(function (ph) {
            const $ph = $(this);
            $ph.find('strong').text(`Photo #${ph + 1}`);
            $ph.find('.remove-photo-btn')
                .attr('data-receive-index', r)
                .attr('data-pallet-index', p)
                .attr('data-photo-index', ph);
            // name="photoFiles" stays the same
        });
    }

    $(document).on('click', '.remove-receive-btn', function () {
        const idx = +$(this).data('receive-index');
        $(`.receive-block[data-receive-index="${idx}"]`).remove();
        reindexReceives();
    });

    $(document).on('click', '.remove-pallet-btn', function () {
        const r = +$(this).data('receive-index'),
            p = +$(this).data('pallet-index');
        $(`.pallets-container[data-pallets-for="${r}"]`)
            .children('.pallet-block').eq(p).remove();
        reindexPallets(r);
    });

    $(document).on('click', '.remove-item-btn', function () {
        const r = +$(this).data('receive-index'),
            p = +$(this).data('pallet-index'),
            i = +$(this).data('item-index');
        $(`.items-container[data-items-for="${r}-${p}"]`)
            .children('.item-block').eq(i).remove();
        reindexItems(r, p);
    });

    $(document).on('click', '.remove-photo-btn', function () {
        const r = +$(this).data('receive-index'),
            p = +$(this).data('pallet-index'),
            ph = +$(this).data('photo-index');
        $(`.photos-container[data-photos-for="${r}-${p}"]`)
            .children('.photo-block').eq(ph).remove();
        reindexPhotos(r, p);
    });

    $(document).on('change', '.transport-type-select', function () {
        const r = +$(this).data('receive-index');
        const val = $(this).val();
        const $wrapper = $(`.container-select-wrapper[data-receive-index="${r}"]`);

        if (val === '0') { // Container selected
            // build the <select>
            const opts = window.containers
                .map(c => `<option value="${c.value}">${c.text}</option>`)
                .join('');
            const html = `
          <label>Container</label>
          <select name="Receives[${r}].ContainerId" class="form-select">
            <option value="">— select —</option>
            ${opts}
          </select>`;
            $wrapper.html(html).show();
        } else {
            // hide & clear
            $wrapper.empty().hide();
        }
    });

    // ─── FORM SUBMIT ───────────────────────────────────────────────────────────

    $('#create-raw-material-form').on('submit', async function (e) {
        e.preventDefault();

        const formData = new FormData();
        // antiforgery
        const token = $('input[name="__RequestVerificationToken"]').val();
        if (token) formData.append('__RequestVerificationToken', token);

        // material-level
        formData.append('MaterialNo', $('input[name="MaterialNo"]').val() || '');
        formData.append('Description', $('input[name="Description"]').val() || '');

        // each receive → pallets → items → photos
        $('.receive-block').each(function (r) {
            const $rc = $(this);
            formData.append(`Receives[${r}].TypeID`,
                $rc.find(`select[name="Receives[${r}].TypeID"]`).val() || '');
            formData.append(`Receives[${r}].BatchNo`,
                $rc.find(`input[name="Receives[${r}].BatchNo"]`).val() || '');
            formData.append(`Receives[${r}].ReceivedDate`,
                $rc.find(`input[name="Receives[${r}].ReceivedDate"]`).val() || '');
            formData.append(`Receives[${r}].ReceivedBy`,
                $rc.find(`input[name="Receives[${r}].ReceivedBy"]`).val() || '');
            formData.append(`Receives[${r}].Remarks`,
                $rc.find(`textarea[name="Receives[${r}].Remarks"]`).val() || '');
            formData.append(`Receives[${r}].PO`,
                $rc.find(`input[name="Receives[${r}].PO"]`).val() || '');
            formData.append(
                `Receives[${r}].PackageTypeId`,
                $rc.find(`select[name="Receives[${r}].PackageTypeId"]`).val() || ''
            );

            formData.append(
                `Receives[${r}].ContainerId`,
                $rc.find(`select[name="Receives[${r}].ContainerId"]`).val() || ''
            );

            $rc.find('.pallet-block').each(function (p) {
                const $pb = $(this);
                formData.append(`Receives[${r}].Pallets[${p}].PalletCode`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].PalletCode"]`).val() || '');
                formData.append(`Receives[${r}].Pallets[${p}].HandledBy`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].HandledBy"]`).val() || '');
                formData.append(`Receives[${r}].Pallets[${p}].PackSize`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].PackSize"]`).val() || '');
                formData.append(`Receives[${r}].Pallets[${p}].StoredBy`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].StoredBy"]`).val() || '');

                // Add boolean values for each group
                formData.append(
                    `Receives[${r}].Pallets[${p}].Group3`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].Group3"]`).is(':checked')
                );
                formData.append(
                    `Receives[${r}].Pallets[${p}].Group6`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].Group6"]`).is(':checked')
                );
                formData.append(
                    `Receives[${r}].Pallets[${p}].Group8`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].Group8"]`).is(':checked')
                );
                formData.append(
                    `Receives[${r}].Pallets[${p}].Group9`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].Group9"]`).is(':checked')
                );
                formData.append(
                    `Receives[${r}].Pallets[${p}].NDG`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].NDG"]`).is(':checked')
                );
                formData.append(
                    `Receives[${r}].Pallets[${p}].Scentaurus`,
                    $pb.find(`input[name="Receives[${r}].Pallets[${p}].Scentaurus"]`).is(':checked')
                );

                $pb.find('.item-block').each(function (i) {
                    const $itm = $(this);
                    formData.append(`Receives[${r}].Pallets[${p}].Items[${i}].ItemCode`,
                        $itm.find(`input[name="Receives[${r}].Pallets[${p}].Items[${i}].ItemCode"]`).val() || '');
                    formData.append(`Receives[${r}].Pallets[${p}].Items[${i}].BatchNo`,
                        $itm.find(`input[name="Receives[${r}].Pallets[${p}].Items[${i}].BatchNo"]`).val() || '');
                    formData.append(`Receives[${r}].Pallets[${p}].Items[${i}].ProdDate`,
                        $itm.find(`input[name="Receives[${r}].Pallets[${p}].Items[${i}].ProdDate"]`).val() || '');
                    const dg = $itm.find(`input[name="Receives[${r}].Pallets[${p}].Items[${i}].DG"]`).is(':checked');
                    formData.append(`Receives[${r}].Pallets[${p}].Items[${i}].DG`, dg.toString());
                    formData.append(`Receives[${r}].Pallets[${p}].Items[${i}].Remarks`,
                        $itm.find(`input[name="Receives[${r}].Pallets[${p}].Items[${i}].Remarks"]`).val() || '');
                });

                let photoIndex = 0;
                $pb.find('.photo-block').each(function () {
                    const fileInput = $(this).find('input[type="file"]')[0];
                    if (fileInput.files.length) {
                        formData.append('photoFiles', fileInput.files[0]);
                        formData.append(`photoPalletCodes[${photoIndex}]`,
                            $pb.find(`input[name="Receives[${r}].Pallets[${p}].PalletCode"]`).val());
                        photoIndex++;
                    }
                });
            });
        });

        try {
            const resp = await fetch('/RawMaterial/Create', {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });
            const ct = resp.headers.get('content-type') || '';
            const data = ct.includes('application/json') ? await resp.json() : null;
            if (data && data.success) {
                sessionStorage.setItem('successMessage', data.message);
                window.location.href = '/RawMaterial/Datatable';
            } else {
                // Show main error message
                toastr.error(data?.message || 'Failed to create raw material.');

                // If there are specific validation errors, show each one
                if (data?.errors && Array.isArray(data.errors) && data.errors.length > 0) {
                    data.errors.forEach(error => {
                        toastr.error(error);
                    });
                }
            }
        } catch (err) {
            console.error(err);
            toastr.error('Unexpected error occurred.');
        }
    });
});