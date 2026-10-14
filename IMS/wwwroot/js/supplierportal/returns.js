document.addEventListener('DOMContentLoaded', function () {
    const authorizeModal = document.getElementById('supplierAuthorizeModal');
    if (authorizeModal) {
        authorizeModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const returnId = button.getAttribute('data-id');
            const returnNo = button.getAttribute('data-no');

            const inputId = authorizeModal.querySelector('#authReturnID');
            const inputNo = authorizeModal.querySelector('#authReturnNo');

            if (inputId) inputId.value = returnId;
            if (inputNo) inputNo.value = returnNo;
        });
    }

    const rejectModal = document.getElementById('supplierRejectModal');
    if (rejectModal) {
        rejectModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const returnId = button.getAttribute('data-id');
            const returnNo = button.getAttribute('data-no');

            const inputId = rejectModal.querySelector('#rejectReturnID');
            const inputNo = rejectModal.querySelector('#rejectReturnNo');

            if (inputId) inputId.value = returnId;
            if (inputNo) inputNo.value = returnNo;
        });
    }
});
