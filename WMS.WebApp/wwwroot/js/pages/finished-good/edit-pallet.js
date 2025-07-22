$(function () {
    const form = $('#edit-pallet-form');
    const receiveId = form.find('input[name="ReceiveId"]').val();

    form.on('submit', function (e) {
        e.preventDefault();

        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: form.serialize(),
            success: function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    setTimeout(() => {
                        window.location.href = `/FinishedGood/Pallets?receiveId=${receiveId}`;
                    }, 800);
                } else {
                    const msg = res.errors && res.errors.length
                        ? res.errors.join('<br>')
                        : res.message;
                    toastr.error(msg);
                }
            },
            error: function () {
                toastr.error('An unexpected error occurred.');
            }
        });
    });
});
