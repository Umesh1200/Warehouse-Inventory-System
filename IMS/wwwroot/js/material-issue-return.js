// Material Issue & Return Scripts
document.addEventListener('DOMContentLoaded', function () {
    console.log('Material Issue & Return Loaded');

    const productDropdown = document.getElementById('NewIssueRequest_ProductID');
    const shelfDropdown = document.getElementById('NewIssueRequest_ShelfID');

    function updateShelves() {
        if (!productDropdown || !shelfDropdown) return;

        const productId = productDropdown.value;

        // Clear current options
        shelfDropdown.innerHTML = '<option value="">-- Select Location --</option>';

        if (productId) {
            fetch(`?handler=ShelvesForProduct&productId=${productId}`)
                .then(response => response.json())
                .then(data => {
                    if (data && data.length > 0) {
                        data.forEach(shelf => {
                            const option = document.createElement('option');
                            option.value = shelf.id;
                            option.textContent = shelf.name;
                            shelfDropdown.appendChild(option);
                        });
                    } else {
                        const option = document.createElement('option');
                        option.value = "";
                        option.textContent = "No available stock on any shelf";
                        option.disabled = true;
                        shelfDropdown.appendChild(option);
                    }
                })
                .catch(error => console.error('Error fetching shelves:', error));
        } else {
            if (window.initialShelfOptions) {
                shelfDropdown.innerHTML = window.initialShelfOptions;
            } else {
                const option = document.createElement('option');
                option.value = "";
                option.textContent = "Please select a material first";
                shelfDropdown.appendChild(option);
            }
        }
    }

    if (productDropdown && shelfDropdown) {
        window.initialShelfOptions = shelfDropdown.innerHTML;
        productDropdown.addEventListener('change', updateShelves);

        if (productDropdown.value) {
            productDropdown.dispatchEvent(new Event('change'));
        }
    }

    const returnModal = document.getElementById('returnModal');
    if (returnModal) {
        returnModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            document.getElementById('return-id').value = button.dataset.id;
            document.getElementById('return-disp-name').innerText = button.dataset.name;
            document.getElementById('return-disp-sku').innerText = button.dataset.sku;
            const max = button.dataset.max;
            document.getElementById('return-max').innerText = max;
            document.getElementById('return-qty').value = max;
            document.getElementById('return-qty').max = max;
            document.getElementById('return-shelf').value = button.dataset.shelf;
        });
    }
});
